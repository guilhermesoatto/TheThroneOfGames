using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using GameStore.Common.Events;
using GameStore.Partidas.Infrastructure.Extensions;
using GameStore.Partidas.Infrastructure.Persistence;
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
    .Enrich.WithProperty("ServiceName", "partidas-api")
    .WriteTo.Console(new CompactJsonFormatter()));

// Configure Kestrel to listen on port 80
builder.WebHost.UseUrls("http://*:80");

builder.Services.AddPartidasInfrastructure(builder.Configuration);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// AddSecurityDefinition/Requirement expõem o botão "Authorize" (JWT Bearer) no Swagger UI —
// sem isso não há como testar os endpoints [Authorize] de /api/partidas via Try-it-out.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Prometheus Metrics — porta dedicada 9094 (9091/9092/9093 já usadas por usuarios/catalogo/vendas).
// Ver docs/ai/skills/dotnet-clean-arch.md §8: resolver o singleton do DI NÃO inicia o listener —
// metricsServer.Start() abaixo é obrigatório.
builder.Services.AddSingleton<IMetricServer>(new KestrelMetricServer(port: 9094));

// OpenTelemetry — Distributed Tracing
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
var sampleRatio = double.TryParse(builder.Configuration["OTEL_TRACES_SAMPLER_ARG"], out var configuredRatio)
    ? configuredRatio
    : 1.0;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("partidas-api"))
    .WithTracing(t => t
        .SetSampler(new TraceIdRatioBasedSampler(sampleRatio))
        .AddAspNetCoreInstrumentation()
        // Cobre tanto a chamada síncrona a usuarios-api (IUsuariosGateway) quanto a propagação
        // automática do header traceparent (W3C Trace Context) nessa chamada.
        .AddHttpClientInstrumentation()
        // Spans de publish/consume do RabbitMqAdapter/BaseEventConsumer (GameStore.Common) —
        // sem isso, o trace_id propagado via TraceContextPropagator não aparece no backend.
        .AddSource("GameStore.Common.Messaging")
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PartidasAPI";

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isso, o ASP.NET Core remapeia claims JWT curtas para URIs longas
        // (ex: "sub" -> ClaimTypes.NameIdentifier), quebrando User.FindFirst("sub")
        // usado em PartidaController (mesmo ajuste já feito nos outros 3 serviços).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Accept tokens from other issuers (inter-service communication)
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Start Prometheus Metrics Server
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
metricsServer.Start();

// Força a construção do IEventBus (RabbitMqAdapter) na inicialização em vez de na primeira
// requisição — mesmo racional dos outros 3 serviços (declara exchange/filas cedo).
var eventBus = app.Services.GetRequiredService<IEventBus>();
_ = eventBus;

// MongoDB não tem "migration" — só garante os índices TTL (purge automático de 60 dias, ver
// PartidasMongoContext) na inicialização, equivalente ao MigrateAsync() dos outros 3 serviços.
var mongoContext = app.Services.GetRequiredService<PartidasMongoContext>();
await mongoContext.EnsureIndexesAsync();

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
