using Events.Shared;
using logs_services.domain.Entities;
using logs_services.domain.Interfaces;
using MassTransit;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace logs_services.infrastructure.Consumers
{
    public class AuditLogConsumer : IConsumer<IAuditLogCreated>
    {
        private readonly IAuditRepository _repository;

        public AuditLogConsumer(IAuditRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<IAuditLogCreated> context)
        {
            var msg = context.Message;
            object datosParaGuardar;

            try
            {
                datosParaGuardar = BsonDocument.Parse(msg.Datos);
            }
            catch
            {
                datosParaGuardar = msg.Datos;
            }

            var logEntry = new AuditLog
            {
                EventoId = msg.Id,
                ServicioOrigen = msg.ServicioOrigen,
                UsuarioId = msg.UsuarioId,
                TipoAccion = msg.TipoAccion,
                Datos = datosParaGuardar, 
                FechaOcurrencia = msg.FechaOcurrencia,
                Nivel = msg.Nivel
            };

            await _repository.RegistrarLogAsync(logEntry);
        }
    }
}
