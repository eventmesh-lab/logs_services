# Logs Services - Microservicio de Auditoría

## 📋 Descripción

**Logs Services** es un microservicio especializado en la **gestión centralizada de logs de auditoría** para arquitecturas distribuidas basadas en eventos. Su propósito principal es:

- **Capturar eventos de auditoría** de otros microservicios a través de mensajería asíncrona (RabbitMQ)
- **Almacenar logs de forma persistente** en MongoDB para consulta histórica
- **Exponer APIs REST** para consultar y analizar logs de auditoría

Este servicio resuelve el problema de negocio de **trazabilidad y compliance**: permite rastrear quién hizo qué, cuándo y en qué contexto dentro de un ecosistema de microservicios, facilitando auditorías, troubleshooting y cumplimiento normativo.

## 📚 Tabla de Contenidos

- [Arquitectura del Sistema](./docs/architecture.md) - Flujo de datos, componentes y dependencias externas
- [Documentación de API](./docs/api.md) - Endpoints REST y contratos de eventos
- [Guía de Configuración](./docs/setup.md) - Variables de entorno, Docker y comandos de desarrollo

## 🛠️ Stack Tecnológico

| Tecnología | Versión | Propósito |
|-----------|---------|-----------|
| **.NET** | 8.0 | Framework principal |
| **ASP.NET Core** | 8.0 | Web API |
| **MongoDB** | 3.5.2 (Driver) | Base de datos NoSQL para logs |
| **RabbitMQ** | 8.5.0 (MassTransit) | Message broker para eventos |
| **MediatR** | 14.0.0 | Patrón CQRS/Mediator |
| **Swagger/OpenAPI** | 6.6.2 | Documentación interactiva de API |

**Patrón arquitectónico:** Clean Architecture (API → Application → Domain → Infrastructure)

## 🚀 Quick Start

### Opción 1: Con Docker Compose (Recomendado)

```bash
# Levantar MongoDB y RabbitMQ
docker-compose up -d

# Ejecutar el servicio
dotnet run --project logs_services.api/logs_services.api.csproj
```

El servicio estará disponible en:
- **API REST:** `http://localhost:5000` (o puerto configurado)
- **Swagger UI:** `http://localhost:5000/swagger` (en modo Development)
- **RabbitMQ Management:** `http://localhost:15676` (user: guest, pass: guest)

### Opción 2: Con Docker (Contenedor completo)

```bash
# Construir la imagen
docker build -t logs-services .

# Ejecutar el contenedor
docker run -p 7188:7188 \
  -e MongoDbSettings__ConnectionString="mongodb://host.docker.internal:27021" \
  logs-services
```

### Verificar funcionamiento

```bash
# Consultar todos los logs
curl http://localhost:5000/api/logs
```

Para más detalles de configuración, consulta la [Guía de Setup](./docs/setup.md).

---

**Mantenido por:** EventMesh Lab  
**Licencia:** [Especificar licencia]
