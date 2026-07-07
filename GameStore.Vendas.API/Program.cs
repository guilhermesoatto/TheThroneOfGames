using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameStore.Vendas.Infrastructure.Extensions;
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
    .Enrich.WithProperty("ServiceName", "vendas-api")
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

builder.Services.AddVendasInfrastructure(builder.Configuration);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Prometheus Metrics
builder.Services.AddSingleton<IMetricServer>(new KestrelMetricServer(port: 9093));

// OpenTelemetry — Distributed Tracing (fase3-T08)
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
var sampleRatio = double.TryParse(builder.Configuration["OTEL_TRACES_SAMPLER_ARG"], out var configuredRatio)
    ? configuredRatio
    : 1.0;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("vendas-api"))
    .WithTracing(t => t
        .SetSampler(new TraceIdRatioBasedSampler(sampleRatio))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        // Spans de publish/consume do RabbitMqAdapter/BaseEventConsumer (GameStore.Common) —
        // sem isso, o trace_id propagado via TraceContextPropagator não aparece no backend.
        .AddSource("GameStore.Common.Messaging")
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VendasAPI";

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
_ = metricsServer; // Ensures server is started

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
