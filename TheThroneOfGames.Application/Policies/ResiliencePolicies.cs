using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace TheThroneOfGames.Application.Policies
{
    /// <summary>
    /// Centraliza as políticas de resiliência (Polly) para a aplicação.
    /// </summary>
    public static class ResiliencePolicies
    {
        /// <summary>
        /// Política de retry com jitter exponencial para operações transientes.
        /// Tenta até 3 vezes com backoff exponencial + jitter.
        /// </summary>
        public static IAsyncPolicy<T> CreateRetryPolicy<T>() where T : class
        {
            return Policy<T>
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .Or<OperationCanceledException>()
                .OrResult(r => r == null)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt =>
                    {
                        var exponentialBackoff = Math.Pow(2, retryAttempt);
                        var jitterMs = new Random().Next(0, 1000);
                        return TimeSpan.FromMilliseconds(exponentialBackoff * 100 + jitterMs);
                    }
                );
        }

        /// <summary>
        /// Política de circuit breaker para proteção contra cascata de falhas.
        /// Abre o circuito após 5 falhas em 30 segundos.
        /// </summary>
        public static IAsyncPolicy<T> CreateCircuitBreakerPolicy<T>() where T : class
        {
            return Policy<T>
                .Handle<HttpRequestException>()
                .OrResult(r => r == null)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );
        }

        /// <summary>
        /// Política de timeout para evitar esperas indefinidas.
        /// Timeout de 5 segundos com cancel do token.
        /// </summary>
        public static IAsyncPolicy<T> CreateTimeoutPolicy<T>() where T : class
        {
            return Policy.TimeoutAsync<T>(
                TimeSpan.FromSeconds(5),
                TimeoutStrategy.Optimistic
            );
        }

        /// <summary>
        /// Combina retry + circuit breaker + timeout em uma única política.
        /// Ordem: Timeout wrapper → Circuit Breaker → Retry
        /// </summary>
        public static IAsyncPolicy<T> CreateCombinedPolicy<T>() where T : class
        {
            return Policy.WrapAsync(
                CreateTimeoutPolicy<T>(),
                CreateCircuitBreakerPolicy<T>(),
                CreateRetryPolicy<T>()
            );
        }

        /// <summary>
        /// Política específica para chamadas a banco de dados.
        /// Retry com timeout curto e circuit breaker agressivo.
        /// </summary>
        public static IAsyncPolicy<T> CreateDatabasePolicy<T>() where T : class
        {
            return Policy<T>
                .Handle<InvalidOperationException>()
                .Or<TimeoutException>()
                .OrResult(r => r == null)
                .WaitAndRetryAsync(
                    2,
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 50)
                )
                .WrapAsync(Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(3)));

        }

        /// <summary>
        /// Política específica para processamento de mensagens.
        /// Retry agressivo com backoff exponencial.
        /// </summary>
        public static IAsyncPolicy<T> CreateMessageProcessingPolicy<T>() where T : class
        {
            return Policy<T>
                .Handle<Exception>()
                .OrResult(r => r == null)
                .WaitAndRetryAsync(
                    5,
                    retryAttempt =>
                    {
                        var exponentialBackoff = Math.Pow(2, retryAttempt);
                        var jitterMs = new Random().Next(0, 2000);
                        return TimeSpan.FromMilliseconds(exponentialBackoff * 100 + jitterMs);
                    }
                );
        }

        /// <summary>
        /// Política para operações de IO externo (APIs, serviços).
        /// Balanço entre retry e falha rápida.
        /// </summary>
        public static IAsyncPolicy<T> CreateExternalServicePolicy<T>() where T : class
        {
            return Policy.WrapAsync(
                Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(10)),
                Policy<T>
                    .Handle<HttpRequestException>()
                    .Or<TimeoutException>()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: 3,
                        durationOfBreak: TimeSpan.FromSeconds(60)
                    ),
                Policy<T>
                    .Handle<HttpRequestException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt =>
                            TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 200)
                    )
            );
        }
    }
}
