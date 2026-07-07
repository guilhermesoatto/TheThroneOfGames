using System.Diagnostics;
using GameStore.Common.Events;
using GameStore.Common.Messaging;
using GameStore.Common.Tracing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

namespace GameStore.Common.Tests
{
    /// <summary>
    /// Prova, contra um RabbitMQ real (via GlobalTestSetup/Testcontainers), que o contexto de trace
    /// distribuído (fase3-T08) sobrevive à publicação e ao consumo de um evento de domínio: o
    /// traceparent injetado por RabbitMqAdapter.PublishAsync é extraído e continuado por
    /// BaseEventConsumer, de forma que o TraceId do publisher e do consumer sejam idênticos.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class TraceContextPropagationTests
    {
        private ActivityListener _listener = null!;
        private RabbitMqAdapter _adapter = null!;
        private TestUsuarioAtivadoConsumer _consumer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // ActivitySource.StartActivity() retorna null sem nenhum listener inscrito — precisamos
            // de um listener "ouvindo tudo" para que as Activities de publish/consume sejam
            // realmente criadas durante o teste (do contrário o teste passaria sem provar nada).
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(_listener);

            try
            {
                var factory = new ConnectionFactory { HostName = "localhost", Port = 5672, UserName = "guest", Password = "guest" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                try { channel.QueueDelete("catalogo.usuario-ativado"); } catch { }
                try { channel.QueueDelete("catalogo.usuario-ativado.dlq"); } catch { }
            }
            catch { /* fila pode não existir ainda */ }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _listener?.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            _consumer?.Dispose();
            _adapter?.Dispose();
        }

        [Test]
        public async Task PublishAsync_ThenConsume_PropagatesSameTraceIdAcrossServices()
        {
            // Arrange — o adapter declara a fila "catalogo.usuario-ativado" ao ser construído.
            _adapter = new RabbitMqAdapter(
                host: "localhost", port: 5672, username: "guest", password: "guest",
                logger: new Mock<ILogger<RabbitMqAdapter>>().Object);

            _consumer = new TestUsuarioAtivadoConsumer(
                "localhost", 5672, "guest", "guest",
                new Mock<ILogger>().Object);
            await _consumer.StartConsumingAsync();

            var testEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(), Email: "trace-test@example.com", Nome: "Trace Test");

            // Act — publica dentro de uma Activity real, simulando uma requisição HTTP instrumentada.
            using var publishActivity = new ActivitySource("GameStore.Common.Tests")
                .StartActivity("HTTP POST /api/usuario/activate")!;
            Assert.That(publishActivity, Is.Not.Null, "listener deveria ter permitido criar a Activity de publish");

            await _adapter.PublishAsync(testEvent);

            var received = await _consumer.WaitForMessageAsync(TimeSpan.FromSeconds(10));

            // Assert
            Assert.That(received, Is.Not.Null, "consumer não recebeu a mensagem a tempo");
            Assert.That(received!.CapturedTraceId, Is.EqualTo(publishActivity.TraceId.ToString()),
                "TraceId capturado no consumer deveria ser o mesmo do publisher (mesmo trace distribuído)");
            Assert.That(received.CapturedTraceId, Is.Not.EqualTo(default(ActivityTraceId).ToString()));
        }

        private class TestUsuarioAtivadoConsumer : BaseEventConsumer<UsuarioAtivadoEvent>
        {
            private readonly TaskCompletionSource<CapturedMessage> _tcs = new();

            public TestUsuarioAtivadoConsumer(string host, int port, string username, string password, ILogger logger)
                : base(host, port, username, password, logger, "catalogo.usuario-ativado")
            {
            }

            public override Task ProcessEventAsync(UsuarioAtivadoEvent domainEvent)
            {
                _tcs.TrySetResult(new CapturedMessage(Activity.Current?.TraceId.ToString() ?? string.Empty));
                return Task.CompletedTask;
            }

            public async Task<CapturedMessage?> WaitForMessageAsync(TimeSpan timeout)
            {
                var completed = await Task.WhenAny(_tcs.Task, Task.Delay(timeout));
                return completed == _tcs.Task ? _tcs.Task.Result : null;
            }
        }

        private record CapturedMessage(string CapturedTraceId);
    }
}
