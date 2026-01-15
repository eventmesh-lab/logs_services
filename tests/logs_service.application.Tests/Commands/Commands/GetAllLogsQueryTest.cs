using Xunit;
using logs_services.application;
using logs_services.application.DTOs;
using MediatR;
using System.Collections.Generic;

namespace logs_services.application.Tests.Queries
{
    public class GetAllLogsQueryTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            var query = new GetAllLogsQuery();

            Assert.NotNull(query);
            Assert.IsAssignableFrom<IRequest<List<AuditLogDto>>>(query);
        }
    }
}