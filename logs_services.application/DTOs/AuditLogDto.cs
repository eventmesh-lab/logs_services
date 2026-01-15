using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logs_services.application.DTOs
{
    public class AuditLogDto
    {
        public string Id { get; set; }
        public Guid EventoId { get; set; }
        public string ServicioOrigen { get; set; }
        public string UsuarioId { get; set; }
        public string TipoAccion { get; set; }
        public object Datos { get; set; } // Puede ser JSON crudo o un objeto
        public DateTime FechaOcurrencia { get; set; }
        public string Nivel { get; set; }
    }
}
