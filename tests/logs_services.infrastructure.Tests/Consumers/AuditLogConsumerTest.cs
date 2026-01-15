using Xunit;
using Moq;
using logs_services.infrastructure.Consumers;
using logs_services.domain.Interfaces;
using logs_services.domain.Entities;
using Events.Shared;
using MassTransit;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace logs_services.infrastructure.Tests.Consumers
{
    public class AuditLogConsumerTests
    {
        private readonly Mock<IAuditRepository> _repositoryMock;
        private readonly AuditLogConsumer _consumer;
        private readonly Mock<ConsumeContext<IAuditLogCreated>> _contextMock;

        public AuditLogConsumerTests()
        {
            _repositoryMock = new Mock<IAuditRepository>();
            _contextMock = new Mock<ConsumeContext<IAuditLogCreated>>();
            _consumer = new AuditLogConsumer(_repositoryMock.Object);
        }

        [Fact]
        public async Task Consume_ShouldSaveLogWithBsonDocument_WhenDatosIsValidJson()
        {
            var validJson = "{ \"action\": \"login\", \"ip\": \"127.0.0.1\" }";
            var messageMock = new Mock<IAuditLogCreated>();
            messageMock.Setup(m => m.Id).Returns(Guid.NewGuid());
            messageMock.Setup(m => m.ServicioOrigen).Returns("AuthService");
            messageMock.Setup(m => m.UsuarioId).Returns("user123");
            messageMock.Setup(m => m.TipoAccion).Returns("Login");
            messageMock.Setup(m => m.Datos).Returns(validJson);
            messageMock.Setup(m => m.FechaOcurrencia).Returns(DateTime.UtcNow);
            messageMock.Setup(m => m.Nivel).Returns("Info");

            _contextMock.Setup(c => c.Message).Returns(messageMock.Object);

            await _consumer.Consume(_contextMock.Object);

            _repositoryMock.Verify(r => r.RegistrarLogAsync(It.Is<AuditLog>(log =>
                log.Datos is BsonDocument &&
                log.ServicioOrigen == "AuthService" &&
                log.UsuarioId == "user123"
            )), Times.Once);
        }

        [Fact]
        public async Task Consume_ShouldSaveLogWithString_WhenDatosIsInvalidJson()
        {
            var invalidJson = "Simple string message, not json";
            var messageMock = new Mock<IAuditLogCreated>();
            messageMock.Setup(m => m.Id).Returns(Guid.NewGuid());
            messageMock.Setup(m => m.ServicioOrigen).Returns("PaymentService");
            messageMock.Setup(m => m.UsuarioId).Returns("user456");
            messageMock.Setup(m => m.TipoAccion).Returns("Error");
            messageMock.Setup(m => m.Datos).Returns(invalidJson);
            messageMock.Setup(m => m.FechaOcurrencia).Returns(DateTime.UtcNow);
            messageMock.Setup(m => m.Nivel).Returns("Error");

            _contextMock.Setup(c => c.Message).Returns(messageMock.Object);

            await _consumer.Consume(_contextMock.Object);

            _repositoryMock.Verify(r => r.RegistrarLogAsync(It.Is<AuditLog>(log =>
                log.Datos is string &&
                (string)log.Datos == invalidJson &&
                log.ServicioOrigen == "PaymentService"
            )), Times.Once);
        }
    }
}