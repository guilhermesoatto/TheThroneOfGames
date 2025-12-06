using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Prometheus;

namespace TheThroneOfGames.API.Telemetry
{
    /// <summary>
    /// Extensions for configuring OpenTelemetry and Prometheus instrumentation.
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Add OpenTelemetry instrumentation (metrics, traces, logs).
        /// </summary>
        public static IServiceCollection AddOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithMetrics(meterBuilder =>
                {
                    meterBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        .AddPrometheusExporter();
                })
                .WithTracing(tracingBuilder =>
                {
                    tracingBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource("TheThroneOfGames.*");
                });

            return services;
        }

        /// <summary>
        /// Configure Prometheus metrics endpoint and custom metrics.
        /// </summary>
        public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
        {
            // Prometheus metrics endpoint
            app.UseMetricServer("/metrics");

            return app;
        }

        /// <summary>
        /// Create activity source for distributed tracing.
        /// </summary>
        public static ActivitySource CreateActivitySource(string name) =>
            new ActivitySource(name, "1.0.0");
    }

    /// <summary>
    /// Custom application metrics for business logic.
    /// </summary>
    public static class ApplicationMetrics
    {
        // HTTP Metrics (already provided by ASP.NET Core instrumentation)
        
        /// <summary>
        /// Counter: total events published to message bus
        /// </summary>
        public static readonly Counter EventsPublishedCounter = Metrics.CreateCounter(
            "thethroneofgames_events_published_total",
            "Total number of domain events published to message bus",
            labelNames: new[] { "event_type", "status" }
        );

        /// <summary>
        /// Gauge: current queue length for autoscaling metrics
        /// </summary>
        public static readonly Gauge QueueLengthGauge = Metrics.CreateGauge(
            "thethroneofgames_queue_length",
            "Current length of message queue",
            labelNames: new[] { "queue_name" }
        );

        /// <summary>
        /// Histogram: event processing latency
        /// </summary>
        public static readonly Histogram EventProcessingLatencyHistogram = Metrics.CreateHistogram(
            "thethroneofgames_event_processing_latency_seconds",
            "Time spent processing events",
            labelNames: new[] { "event_type" }
        );

        /// <summary>
        /// Counter: total users registered
        /// </summary>
        public static readonly Counter UsersRegisteredCounter = Metrics.CreateCounter(
            "thethroneofgames_users_registered_total",
            "Total number of users registered"
        );

        /// <summary>
        /// Counter: total games purchased
        /// </summary>
        public static readonly Counter GamesPurchasedCounter = Metrics.CreateCounter(
            "thethroneofgames_games_purchased_total",
            "Total number of games purchased",
            labelNames: new[] { "game_name" }
        );

        /// <summary>
        /// Gauge: active users online
        /// </summary>
        public static readonly Gauge ActiveUsersGauge = Metrics.CreateGauge(
            "thethroneofgames_active_users",
            "Number of active users online"
        );

        /// <summary>
        /// Histogram: database query latency
        /// </summary>
        public static readonly Histogram DatabaseLatencyHistogram = Metrics.CreateHistogram(
            "thethroneofgames_database_latency_seconds",
            "Time spent querying database",
            labelNames: new[] { "query_type", "status" }
        );

        /// <summary>
        /// Counter: total authentication attempts
        /// </summary>
        public static readonly Counter AuthenticationAttemptsCounter = Metrics.CreateCounter(
            "thethroneofgames_authentication_attempts_total",
            "Total authentication attempts",
            labelNames: new[] { "result" }
        );

        /// <summary>
        /// Counter: validation errors
        /// </summary>
        public static readonly Counter ValidationErrorsCounter = Metrics.CreateCounter(
            "thethroneofgames_validation_errors_total",
            "Total validation errors",
            labelNames: new[] { "field" }
        );

        /// <summary>
        /// Record event publication metric
        /// </summary>
        public static void RecordEventPublished(string eventType, string status = "success")
        {
            EventsPublishedCounter.WithLabels(eventType, status).Inc();
        }

        /// <summary>
        /// Update queue length metric
        /// </summary>
        public static void UpdateQueueLength(string queueName, double length)
        {
            QueueLengthGauge.WithLabels(queueName).Set(length);
        }

        /// <summary>
        /// Record event processing time
        /// </summary>
        public static void RecordEventProcessingLatency(string eventType, double latencySeconds)
        {
            EventProcessingLatencyHistogram.WithLabels(eventType).Observe(latencySeconds);
        }
    }
}
