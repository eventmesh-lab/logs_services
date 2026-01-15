using logs_services.domain.Interfaces;
using logs_services.infrastructure.Configurations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logs_services.domain.Entities;
using logs_services.domain.Interfaces;

namespace logs_services.infrastructure.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly IMongoCollection<AuditLog> _collection;

        public AuditRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings)
        {
            _collection = database.GetCollection<AuditLog>(settings.Value.CollectionName);
        }

        public async Task RegistrarLogAsync(AuditLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            await _collection.InsertOneAsync(log);
        }

        public async Task<List<AuditLog>> ObtenerLogsPorUsuarioAsync(string userId)
        {
            return await _collection
                .Find(log => log.UsuarioId == userId)
                .SortByDescending(log => log.FechaOcurrencia)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> ObtenerTodosAsync()
        {
            return await _collection
                .Find(_ => true)
                .SortByDescending(l => l.FechaOcurrencia)
                .ToListAsync();
        }
    }
}
