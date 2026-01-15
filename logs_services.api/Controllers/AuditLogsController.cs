using logs_services.application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace logs_services.api.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLogs()
        {
            var query = new GetAllLogsQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
