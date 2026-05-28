using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameStore.Usuarios.Infrastructure.Extensions;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "usuarios-api")
    .WriteTo.Console(new CompactJsonFormatter()));

// Configure Kestrel to listen on port 80
builder.WebHost.UseUrls("http://*:80");

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

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

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("usuarios-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

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
var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? "http://localhost:3000,http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Start Prometheus Metrics Server
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
_ = metricsServer; // Ensures server is started

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
