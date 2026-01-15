using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logs_services.application.DTOs;
using MediatR;

namespace logs_services.application
{
    public class GetAllLogsQuery : IRequest<List<AuditLogDto>>
    {
    }
}
