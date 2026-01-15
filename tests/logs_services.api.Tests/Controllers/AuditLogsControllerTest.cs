using Xunit;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using logs_services.api.Controllers;
using logs_services.application;
using logs_services.application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace logs_services.api.Tests.Controllers
{
    public class AuditLogsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly AuditLogsController _controller;

        public AuditLogsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new AuditLogsController(_mediatorMock.Object);
        }

        [Fact]
        public async Task GetAllLogs_ShouldReturnOk_WhenCalled()
        {
            var expectedLogs = new List<AuditLogDto>
            {
                new AuditLogDto
                {
                    Id = "65a12b3c",
                    EventoId = Guid.NewGuid(),
                    ServicioOrigen = "AuthService",
                    UsuarioId = "user1@test.com",
                    TipoAccion = "Login",
                    FechaOcurrencia = DateTime.UtcNow,
                    Nivel = "Info",
                    Datos = new { IP = "127.0.0.1" }
                },
                new AuditLogDto
                {
                    Id = "65a12b3d",
                    EventoId = Guid.NewGuid(),
                    ServicioOrigen = "PaymentService",
                    UsuarioId = "user2@test.com",
                    TipoAccion = "Payment",
                    FechaOcurrencia = DateTime.UtcNow,
                    Nivel = "Warning",
                    Datos = "{ \"monto\": 100 }"
                }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLogs);

            var result = await _controller.GetAllLogs();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expectedLogs, okResult.Value);

            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllLogsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}