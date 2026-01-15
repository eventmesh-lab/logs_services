using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Events.Shared
{
    public interface IAuditLogCreated
    {
        Guid Id { get; }
        string ServicioOrigen { get; } 
        string UsuarioId { get; }     
        string TipoAccion { get; }    
        string Datos { get; }         
        DateTime FechaOcurrencia { get; }
        string Nivel { get; }          
    }
}
