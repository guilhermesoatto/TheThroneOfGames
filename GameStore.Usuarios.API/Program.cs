using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameStore.Common.Messaging;
using GameStore.Usuarios.Application.EventConsumers;
using GameStore.Usuarios.Infrastructure.Extensions;
using GameStore.Usuarios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Enrich.WithProperty("ServiceName", "usuarios-api")
    .WriteTo.Console(new CompactJsonFormatter()));

// Configure Kestrel to listen on port 80
builder.WebHost.UseUrls("http://*:80");

// appsettings.json / appsettings.{Environment}.json / variáveis de ambiente já são
// carregados por WebApplication.CreateBuilder() na ordem correta de precedência
// (env vars por último = maior prioridade). Re-adicionar os arquivos JSON aqui
// os colocava DEPOIS das env vars na cadeia de configuração, fazendo o valor
// hardcoded de appsettings.Development.json (localhost) sobrescrever a
// ConnectionStrings__DefaultConnection do docker-compose.yml.

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddUsuariosContext(connectionString);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Prometheus Metrics
builder.Services.AddSingleton<IMetricServer>(new KestrelMetricServer(port: 9091));

// OpenTelemetry — Distributed Tracing (fase3-T08)
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
var sampleRatio = double.TryParse(builder.Configuration["OTEL_TRACES_SAMPLER_ARG"], out var configuredRatio)
    ? configuredRatio
    : 1.0;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("usuarios-api"))
    .WithTracing(t => t
        .SetSampler(new TraceIdRatioBasedSampler(sampleRatio))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("GameStore.Common.Messaging")
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "UsuariosAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "UsuariosAPIUsers";

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Sem isso, o ASP.NET Core remapeia claims JWT curtas para URIs longas
    // (ex: "sub" -> ClaimTypes.NameIdentifier), quebrando User.FindFirst("sub")
    // usado em UsuarioController.GetProfile.
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false, // Disabled for inter-service communication
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Event Consumer — reage a PedidoFinalizadoEvent publicado por GameStore.Vendas (fase3-T08).
// Condicional a EventBus:UseRabbitMq para que testes com WebApplicationFactory (que não sobem um
// broker real) não tentem conectar — ver UsuariosWebApplicationFactory, que força esse valor a false.
if (builder.Configuration.GetValue<bool>("EventBus:UseRabbitMq"))
{
    var rabbitMq = builder.Configuration.GetSection("EventBus:RabbitMq");
    var rabbitHost = rabbitMq.GetValue<string>("HostName") ?? "localhost";
    var rabbitPort = rabbitMq.GetValue<int>("Port", 5672);
    var rabbitUser = rabbitMq.GetValue<string>("UserName") ?? "guest";
    var rabbitPassword = rabbitMq.GetValue<string>("Password") ?? "guest";

    builder.Services.AddSingleton<IEventConsumer>(provider =>
        new PedidoFinalizadoEventConsumer(
            rabbitHost, rabbitPort, rabbitUser, rabbitPassword,
            provider.GetRequiredService<ILogger<PedidoFinalizadoEventConsumer>>()));
    builder.Services.AddHostedService<EventConsumerService>();
}

var app = builder.Build();

// Start Prometheus Metrics Server
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
_ = metricsServer; // Ensures server is started

// Aplica as migrations do EF Core na inicialização — cada microsserviço gerencia seu próprio
// schema. Sem isso, um banco recém-criado (docker-compose de um ambiente novo) nunca teria as
// tabelas, e toda escrita falharia.
using (var migrationScope = app.Services.CreateScope())
{
    var usuariosDbContext = migrationScope.ServiceProvider.GetRequiredService<UsuariosDbContext>();
    await usuariosDbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Add Prometheus middleware for request/response metrics
app.UseHttpMetrics();

app.MapControllers();
app.MapMetrics();

// Health check endpoint for Kubernetes probes
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
