using NUnit.Framework;
using Testcontainers.RabbitMq;

namespace GameStore.Common.Tests;

/// <summary>
/// Sobe um RabbitMQ real via Testcontainers antes de qualquer teste da suíte rodar, e derruba
/// ao final. Publicado na porta 5672 do host para bater com os endereços hardcoded
/// ("localhost:5672") já usados em RabbitMqAdapterTests/RabbitMqConsumerTests — substitui a
/// dependência de um broker externo já rodando manualmente na máquina.
/// </summary>
[SetUpFixture]
public class GlobalTestSetup
{
    private static RabbitMqContainer? _container;

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _container = new RabbitMqBuilder("rabbitmq:3.13-management-alpine")
            .WithPortBinding(5672, 5672)
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
