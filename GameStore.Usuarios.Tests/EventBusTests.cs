using NUnit.Framework;
using GameStore.Common.Events;
using GameStore.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameStore.Usuarios.Tests
{
    public class EventBusTests
    {
        private IEventBus _eventBus = null!;

        [SetUp]
        public void Setup()
        {
            _eventBus = new SimpleEventBus();
        }

        [Test]
        public async Task EventBus_PublishAsync_Calls_RegisteredHandlers()
        {
            // Arrange
            bool handlerCalled = false;
            var handler = new TestEventHandler(() => handlerCalled = true);
            
            var usuarioEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(),
                Email: "test@example.com",
                Nome: "Test User"
            );

            _eventBus.Subscribe(handler);

            // Act
            await _eventBus.PublishAsync(usuarioEvent);

            // Assert
            Assert.IsTrue(handlerCalled);
        }

        [Test]
        public async Task EventBus_PublishAsync_Calls_Multiple_Handlers()
        {
            // Arrange
            var handlerCalls = new List<string>();
            var handler1 = new TestEventHandler(() => handlerCalls.Add("Handler1"));
            var handler2 = new TestEventHandler(() => handlerCalls.Add("Handler2"));

            var usuarioEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(),
                Email: "test@example.com",
                Nome: "Test User"
            );

            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);

            // Act
            await _eventBus.PublishAsync(usuarioEvent);

            // Assert
            Assert.AreEqual(2, handlerCalls.Count);
            Assert.Contains("Handler1", handlerCalls);
            Assert.Contains("Handler2", handlerCalls);
        }

        [Test]
        public void EventBus_GetHandlerCount_Returns_Correct_Count()
        {
            // Arrange
            var handler1 = new TestEventHandler(() => { });
            var handler2 = new TestEventHandler(() => { });

            // Act
            _eventBus.Subscribe(handler1);
            _eventBus.Subscribe(handler2);
            var count = _eventBus.GetHandlerCount<UsuarioAtivadoEvent>();

            // Assert
            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task EventBus_Unsubscribe_Removes_Handler()
        {
            // Arrange
            bool handlerCalled = false;
            var handler = new TestEventHandler(() => handlerCalled = true);

            var usuarioEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(),
                Email: "test@example.com",
                Nome: "Test User"
            );

            _eventBus.Subscribe(handler);
            _eventBus.Unsubscribe(handler);

            // Act
            await _eventBus.PublishAsync(usuarioEvent);

            // Assert
            Assert.IsFalse(handlerCalled);
            Assert.AreEqual(0, _eventBus.GetHandlerCount<UsuarioAtivadoEvent>());
        }

        [Test]
        public async Task EventBus_PublishAsync_With_No_Handlers_Succeeds()
        {
            // Arrange
            var usuarioEvent = new UsuarioAtivadoEvent(
                UsuarioId: Guid.NewGuid(),
                Email: "test@example.com",
                Nome: "Test User"
            );

            // Act & Assert - Should not throw
            await _eventBus.PublishAsync(usuarioEvent);
        }

        [Test]
        public void EventBus_Subscribe_Throws_With_Null_Handler()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _eventBus.Subscribe<UsuarioAtivadoEvent>(null!));
        }

        [Test]
        public void EventBus_PublishAsync_Throws_With_Null_Event()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _eventBus.PublishAsync<UsuarioAtivadoEvent>(null!));
        }

        // Test helper
        private class TestEventHandler : IEventHandler<UsuarioAtivadoEvent>
        {
            private readonly Action _action;

            public TestEventHandler(Action action)
            {
                _action = action;
            }

            public Task HandleAsync(UsuarioAtivadoEvent domainEvent)
            {
                _action();
                return Task.CompletedTask;
            }
        }
    }
}
