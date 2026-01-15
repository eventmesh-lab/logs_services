using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logs_services.application.DTOs;
using logs_services.domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace logs_services.application.Commands.Hanlder
{
    public class GetAllLogsHandler : IRequestHandler<GetAllLogsQuery, List<AuditLogDto>>
    {
        private readonly IAuditRepository _repository;

        public GetAllLogsHandler(IAuditRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AuditLogDto>> Handle(GetAllLogsQuery request, CancellationToken cancellationToken)
        {
            var logsEntities = await _repository.ObtenerTodosAsync();

            var logsDtos = logsEntities.Select(log => new AuditLogDto
            {
                Id = log.Id,
                EventoId = log.EventoId,
                ServicioOrigen = log.ServicioOrigen,
                UsuarioId = log.UsuarioId,
                TipoAccion = log.TipoAccion,

                Datos = log.Datos is BsonValue bsonValue
                    ? BsonTypeMapper.MapToDotNetValue(bsonValue)
                    : log.Datos,

                FechaOcurrencia = log.FechaOcurrencia,
                Nivel = log.Nivel
            }).ToList();

            return logsDtos;
        }
    }
}
