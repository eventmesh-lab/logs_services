using Xunit;
using Moq;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using logs_services.infrastructure.Repositories;
using logs_services.domain.Entities;
using logs_services.infrastructure.Configurations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace logs_services.infrastructure.Tests.Repositories
{
    public class AuditRepositoryTests
    {
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<AuditLog>> _mockCollection;
        private readonly Mock<IOptions<MongoDbSettings>> _mockOptions;
        private readonly AuditRepository _repository;

        public AuditRepositoryTests()
        {
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockCollection = new Mock<IMongoCollection<AuditLog>>();
            _mockOptions = new Mock<IOptions<MongoDbSettings>>();

            _mockOptions.Setup(o => o.Value).Returns(new MongoDbSettings 
            { 
                CollectionName = "AuditLogs" 
            });

            _mockDatabase
                .Setup(d => d.GetCollection<AuditLog>("AuditLogs", null))
                .Returns(_mockCollection.Object);

            _repository = new AuditRepository(_mockDatabase.Object, _mockOptions.Object);
        }

        [Fact]
        public async Task RegistrarLogAsync_ShouldThrowArgumentNullException_WhenLogIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.RegistrarLogAsync(null));
        }

        [Fact]
        public async Task RegistrarLogAsync_ShouldCallInsertOneAsync_WhenLogIsValid()
        {
            var log = new AuditLog 
            { 
                Id = "1", 
                UsuarioId = "user1", 
                FechaOcurrencia = DateTime.UtcNow 
            };

            await _repository.RegistrarLogAsync(log);

            _mockCollection.Verify(c => c.InsertOneAsync(
                log, 
                It.IsAny<InsertOneOptions>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ObtenerTodosAsync_ShouldReturnListOfLogs()
        {
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = "1", UsuarioId = "A" },
                new AuditLog { Id = "2", UsuarioId = "B" }
            };

            var mockCursor = new MockAsyncCursor<AuditLog>(logs);
        }
    }

    public class MockAsyncCursor<T> : IAsyncCursor<T>
    {
        private readonly IEnumerable<T> _items;
        private readonly IEnumerator<T> _enumerator;
        private bool _moved;

        public MockAsyncCursor(IEnumerable<T> items)
        {
            _items = items ?? new List<T>();
            _enumerator = _items.GetEnumerator();
            _moved = false;
        }

        public IEnumerable<T> Current => _items; 

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext(CancellationToken cancellationToken = default)
        {
            return !_moved && (_moved = true);
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MoveNext(cancellationToken));
        }
    }
}