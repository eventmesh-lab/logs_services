using Xunit;
using Moq;
using logs_services.application.Commands.Hanlder;
using logs_services.application;
using logs_services.domain.Interfaces;
using logs_services.domain.Entities;
using logs_services.application.DTOs;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace logs_services.application.Tests.Handlers
{
    public class GetAllLogsHandlerTests
    {
        private readonly Mock<IAuditRepository> _repositoryMock;
        private readonly GetAllLogsHandler _handler;

        public GetAllLogsHandlerTests()
        {
            _repositoryMock = new Mock<IAuditRepository>();
            _handler = new GetAllLogsHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnListOfDtos_WhenLogsExist()
        {
            var query = new GetAllLogsQuery();
            var logsEntities = new List<AuditLog>
            {
                new AuditLog
                {
                    Id = "1",
                    EventoId = Guid.NewGuid(),
                    ServicioOrigen = "ServiceA",
                    UsuarioId = "User1",
                    TipoAccion = "Login",
                    Datos = "RawData",
                    FechaOcurrencia = DateTime.UtcNow,
                    Nivel = "Info"
                },
                new AuditLog
                {
                    Id = "2",
                    EventoId = Guid.NewGuid(),
                    ServicioOrigen = "ServiceB",
                    UsuarioId = "User2",
                    TipoAccion = "Logout",
                    Datos = new BsonString("BsonData"),
                    FechaOcurrencia = DateTime.UtcNow,
                    Nivel = "Warning"
                }
            };

            _repositoryMock
                .Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(logsEntities);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            Assert.Equal("RawData", result[0].Datos);

            Assert.Equal("BsonData", result[1].Datos);

            Assert.Equal(logsEntities[0].Id, result[0].Id);
            Assert.Equal(logsEntities[0].EventoId, result[0].EventoId);

            _repositoryMock.Verify(r => r.ObtenerTodosAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoLogsExist()
        {
            var query = new GetAllLogsQuery();
            var logsEntities = new List<AuditLog>();

            _repositoryMock
                .Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(logsEntities);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);

            _repositoryMock.Verify(r => r.ObtenerTodosAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMapBsonDocumentCorrectly()
        {
            var query = new GetAllLogsQuery();
            var bsonDoc = new BsonDocument { { "key", "value" } };

            var logsEntities = new List<AuditLog>
            {
                new AuditLog
                {
                    Id = "1",
                    Datos = bsonDoc
                }
            };

            _repositoryMock
                .Setup(r => r.ObtenerTodosAsync())
                .ReturnsAsync(logsEntities);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Single(result);

            var mappedData = result[0].Datos as Dictionary<string, object>;
            Assert.NotNull(mappedData);
            Assert.Equal("value", mappedData["key"]);
        }
    }
}