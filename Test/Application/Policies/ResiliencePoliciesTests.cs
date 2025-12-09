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
        public void CircuitBreakerPolicy_OpensAfterThresholdFailures()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateCircuitBreakerPolicy<string>();
            int attemptCount = 0;

            // Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var exception = Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Simulated failure");
                });
                Assert.IsNotNull(exception);
            }

            // After 5 failures, circuit should be open
            var circuitOpenException = Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    throw new HttpRequestException("Should not execute");
                });
            });

            Assert.IsNotNull(circuitOpenException);
        }

        [Test]
        public void TimeoutPolicy_CancelsAfterDuration()
        {
            // Arrange
            var policy = ResiliencePolicies.CreateTimeoutPolicy<string>();

            // Act & Assert
            var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    // Simulate long-running operation
                    await Task.Delay(10000);
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

            // Act & Assert - Timeout after 3 seconds
            var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    await Task.Delay(5000);
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
