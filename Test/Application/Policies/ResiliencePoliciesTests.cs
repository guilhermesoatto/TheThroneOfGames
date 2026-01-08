using NUnit.Framework;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using TheThroneOfGames.Application.Policies;

namespace TheThroneOfGames.Application.Tests.Policies
{
    [TestFixture]
    public class ResiliencePoliciesTests
    {
        [Test]
        public void RetryPolicy_RetriesOnTransientFailure()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateRetryPolicy<string>();
            int attemptCount = 0;

            // Act
            var result = policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < 2)
                    throw new TimeoutException("Simulated transient failure");
                return "Success";
            }).Result;

            // Assert
            Assert.AreEqual("Success", result);
            Assert.AreEqual(2, attemptCount);
        }

        [Test]
        public async Task CircuitBreakerPolicy_OpensAfterThresholdFailures()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateCircuitBreakerPolicy<string>();
            int attemptCount = 0;

            // Act - Execute 5 failures to open circuit
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        attemptCount++;
                        throw new HttpRequestException("Simulated failure");
                    });
                }
                catch (HttpRequestException)
                {
                    // Expected
                }
            }

            // Assert - After 5 failures, circuit should be open
            var exception = Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    await Task.CompletedTask;
                    return "Should not execute";
                });
            });

            Assert.IsNotNull(exception);
        }

        [Test]
        public void TimeoutPolicy_CancelsAfterDuration()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateTimeoutPolicy<string>();

            // Act & Assert - Use TimeoutRejectedException which is what Polly throws
            var exception = Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () =>
            {
                await policy.ExecuteAsync(async (ct) =>
                {
                    // Simulate long-running operation
                    await Task.Delay(15000, ct); // Longer than timeout, observes cancellation
                    return "Should not complete";
                });
            });

            Assert.IsNotNull(exception);
        }

        [Test]
        public async Task CombinedPolicy_ApplicationRethenCircuitBreakerThenTimeout()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateCombinedPolicy<string>();
            int attemptCount = 0;

            // Act - Retry succeeds on second attempt
            var result = await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                    throw new TimeoutException("Transient failure");
                return "Success";
            });

            // Assert
            Assert.AreEqual("Success", result);
            Assert.AreEqual(2, attemptCount);
        }

        [Test]
        public void DatabasePolicy_HasShortTimeoutAndLimitedRetry()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateDatabasePolicy<string>();

            // Act & Assert - Use TimeoutRejectedException which is what Polly throws
            var exception = Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () =>
            {
                await policy.ExecuteAsync(async (ct) =>
                {
                    await Task.Delay(10000, ct); // Longer than database timeout, observes cancellation
                    return "Should timeout";
                });
            });

            Assert.IsNotNull(exception);
        }

        [Test]
        public async Task MessageProcessingPolicy_RetriesAggresively()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateMessageProcessingPolicy<string>();
            int attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                    throw new InvalidOperationException("Transient");
                return "Success";
            });

            // Assert
            Assert.AreEqual("Success", result);
            Assert.AreEqual(3, attemptCount);
        }

        [Test]
        public async Task ExternalServicePolicy_CombinesAllStrategies()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateExternalServicePolicy<string>();
            int attemptCount = 0;

            // Act
            var result = await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount == 1)
                    throw new HttpRequestException("Service unavailable");
                return "Success";
            });

            // Assert
            Assert.AreEqual("Success", result);
            Assert.GreaterOrEqual(attemptCount, 1);
        }

        [Test]
        public void Policy_DoesNotRetryNonTransientErrors()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateRetryPolicy<string>();
            int attemptCount = 0;

            // Act & Assert - Should not retry for non-transient errors
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    throw new ArgumentException("Non-transient error");
                });
            });

            Assert.IsNotNull(exception);
            Assert.AreEqual(1, attemptCount); // Only one attempt
        }
    }
}
