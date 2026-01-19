using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace GameStore.Common.Messaging;

/// <summary>
/// Configuração de Dead Letter Queue para RabbitMQ
/// </summary>
public static class RabbitMqDlqConfiguration
{
    public static void ConfigureDeadLetterQueue(IModel channel, string queueName, ILogger? logger = null)
    {
        var dlxName = $"{queueName}.dlx";
        var dlqName = $"{queueName}.dlq";

        try
        {
            // Declarar Dead Letter Exchange
            channel.ExchangeDeclare(
                exchange: dlxName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declarar Dead Letter Queue
            channel.QueueDeclare(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind DLQ ao DLX
            channel.QueueBind(
                queue: dlqName,
                exchange: dlxName,
                routingKey: "failed");

            // Declarar fila principal com DLX configurado
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = dlxName,
                ["x-dead-letter-routing-key"] = "failed",
                ["x-message-ttl"] = 86400000 // 24 horas
            };

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            logger?.LogInformation($"[RabbitMQ] DLQ configured for queue: {queueName}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"[RabbitMQ] Failed to configure DLQ for queue: {queueName}");
            throw;
        }
    }

    public static void ConfigureWithRetry(IModel channel, string queueName, int maxRetries = 3, ILogger? logger = null)
    {
        var retryExchange = $"{queueName}.retry";
        var retryQueue = $"{queueName}.retry";

        try
        {
            // Exchange para retry
            channel.ExchangeDeclare(
                exchange: retryExchange,
                type: ExchangeType.Direct,
                durable: true);

            // Fila de retry com TTL
            var retryArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = "", // Volta pra fila original
                ["x-dead-letter-routing-key"] = queueName,
                ["x-message-ttl"] = 5000 // 5 segundos
            };

            channel.QueueDeclare(
                queue: retryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: retryArgs);

            channel.QueueBind(
                queue: retryQueue,
                exchange: retryExchange,
                routingKey: "retry");

            logger?.LogInformation($"[RabbitMQ] Retry queue configured for: {queueName}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"[RabbitMQ] Failed to configure retry for queue: {queueName}");
            throw;
        }
    }
}
