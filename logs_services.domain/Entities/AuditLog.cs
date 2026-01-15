using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace logs_services.domain.Entities
{
    public class AuditLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] 
        public string Id { get; set; }

        public Guid EventoId { get; set; }
        public string ServicioOrigen { get; set; }
        public string UsuarioId { get; set; }
        public string TipoAccion { get; set; }
        public object Datos { get; set; }
        public DateTime FechaOcurrencia { get; set; }
        public string Nivel { get; set; }
    }
}
