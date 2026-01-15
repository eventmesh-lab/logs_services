using logs_services.domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logs_services.domain.Interfaces
{
    public interface IAuditRepository
    {
        Task RegistrarLogAsync(AuditLog log);
        Task<List<AuditLog>> ObtenerLogsPorUsuarioAsync(string userId);
        Task<List<AuditLog>> ObtenerTodosAsync();
    }
}
