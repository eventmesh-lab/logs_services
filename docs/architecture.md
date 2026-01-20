# Arquitectura del Sistema

## 🏗️ Visión General

Logs Services implementa una **arquitectura hexagonal (Clean Architecture)** con separación clara de responsabilidades en 4 capas:

```
┌─────────────────────────────────────────────────────────────┐
│  logs_services.api (Presentation Layer)                     │
│  - Controllers REST                                          │
│  - Configuración de Swagger                                 │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│  logs_services.application (Application Layer)              │
│  - Commands/Queries (CQRS)                                  │
│  - Handlers (MediatR)                                       │
│  - DTOs                                                     │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│  logs_services.domain (Domain Layer)                        │
│  - Entities (AuditLog)                                      │
│  - Interfaces (IAuditRepository)                            │
│  - Events (IAuditLogCreated)                               │
└──────────────────┬──────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────────────┐
│  logs_services.infrastructure (Infrastructure Layer)        │
│  - Repositories (MongoDB)                                   │
│  - Consumers (RabbitMQ)                                     │
│  - Configuraciones                                          │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Flujo de Datos

### Flujo 1: Consumo de eventos (Escritura Asíncrona)

```
[Otro Microservicio] 
    │
    │ Publica evento IAuditLogCreated
    ▼
[RabbitMQ Queue: audit-service-queue]
    │
    │ MassTransit Consumer
    ▼
[AuditLogConsumer] (Infrastructure)
    │
    │ Parsea JSON y convierte a BsonDocument
    ▼
[AuditRepository.RegistrarLogAsync]
    │
    │ InsertOneAsync
    ▼
[MongoDB Collection: AuditLogs]
```

**Descripción narrativa:**

1. **Origen del evento:** Un microservicio externo (ej. servicio de usuarios, órdenes, etc.) publica un evento `IAuditLogCreated` en RabbitMQ cuando ocurre una acción auditable.

2. **Recepción:** El `AuditLogConsumer` escucha la cola `audit-service-queue` mediante MassTransit y recibe el mensaje.

3. **Transformación:** El consumer intenta parsear el campo `Datos` como JSON (`BsonDocument`). Si falla, lo guarda como string plano.

4. **Persistencia:** Se crea una entidad `AuditLog` y se persiste en MongoDB usando el repositorio.

### Flujo 2: Consulta de logs (Lectura Síncrona)

```
[Cliente HTTP]
    │
    │ GET /api/logs
    ▼
[AuditLogsController] (API)
    │
    │ Envía GetAllLogsQuery
    ▼
[GetAllLogsHandler] (Application)
    │
    │ Usa IAuditRepository
    ▼
[AuditRepository.ObtenerTodosAsync]
    │
    │ Find(_ => true) + Sort
    ▼
[MongoDB]
    │
    │ Devuelve List<AuditLog>
    ▼
[Handler]
    │
    │ Mapea a List<AuditLogDto>
    │ Convierte BsonValue a .NET types
    ▼
[Controller]
    │
    │ Devuelve HTTP 200 OK
    ▼
[Cliente recibe JSON]
```

**Descripción narrativa:**

1. **Request HTTP:** Un cliente (frontend, otro servicio) hace un GET a `/api/logs`.

2. **Controller:** `AuditLogsController` delega en MediatR, enviando un `GetAllLogsQuery`.

3. **Handler:** `GetAllLogsHandler` consulta el repositorio para obtener todos los logs ordenados por fecha descendente.

4. **Repositorio:** `AuditRepository` ejecuta una query MongoDB (`Find(_ => true)`) y ordena por `FechaOcurrencia`.

5. **Mapeo:** El handler convierte las entidades `AuditLog` a DTOs (`AuditLogDto`), transformando valores `BsonValue` a tipos .NET nativos.

6. **Response:** El controller devuelve la lista en formato JSON con HTTP 200.

## 🔗 Dependencias Externas

### 1. MongoDB (Base de Datos)

- **Propósito:** Almacenamiento persistente de logs de auditoría
- **Conexión:** Configurada vía `MongoDbSettings` en `appsettings.json`
- **Default:** `mongodb://localhost:27017`
- **Base de datos:** `AuditDb`
- **Colección:** `AuditLogs`
- **Driver:** MongoDB.Driver v3.5.2

**Schema implícito:**
```json
{
  "_id": "ObjectId",
  "EventoId": "GUID",
  "ServicioOrigen": "string",
  "UsuarioId": "string",
  "TipoAccion": "string",
  "Datos": "BsonDocument | string",
  "FechaOcurrencia": "DateTime",
  "Nivel": "string"
}
```

### 2. RabbitMQ (Message Broker)

- **Propósito:** Recepción de eventos de auditoría desde otros microservicios
- **Protocolo:** AMQP vía MassTransit
- **Default:** `localhost:5672` (management UI en `15672`)
- **Credenciales:** `guest`/`guest`
- **Cola consumida:** `audit-service-queue`
- **Evento:** `IAuditLogCreated` (contrato compartido en namespace `Events.Shared`)

**⚠️ Nota crítica:** La configuración de RabbitMQ está **hardcodeada** en `Program.cs` (ver sección de Deuda Técnica).

### 3. Microservicios Externos (Productores de eventos)

- **Comunicación:** Asíncrona vía RabbitMQ
- **Contrato:** Deben publicar eventos que implementen `IAuditLogCreated`
- **Campos requeridos:**
  - `Id` (Guid): Identificador único del evento
  - `ServicioOrigen` (string): Nombre del servicio que originó el evento
  - `UsuarioId` (string): ID del usuario que ejecutó la acción
  - `TipoAccion` (string): Tipo de acción (ej. "CREATE", "UPDATE", "DELETE")
  - `Datos` (string): JSON serializado con detalles de la acción
  - `FechaOcurrencia` (DateTime): Timestamp de la acción
  - `Nivel` (string): Nivel de severidad (ej. "INFO", "WARNING", "ERROR")

## 📊 Modelo de Datos

### Entidad Principal: `AuditLog`

```csharp
public class AuditLog
{
    [BsonId]
    public string Id { get; set; }              // ObjectId de MongoDB
    
    public Guid EventoId { get; set; }          // ID del evento original
    public string ServicioOrigen { get; set; }  // Microservicio origen
    public string UsuarioId { get; set; }       // ID del usuario
    public string TipoAccion { get; set; }      // Tipo de operación
    public object Datos { get; set; }           // JSON/objeto con detalles
    public DateTime FechaOcurrencia { get; set; } // Timestamp
    public string Nivel { get; set; }           // Severidad del log
}
```

**Características:**
- **ID autogenerado:** MongoDB genera el `_id` automáticamente
- **Flexibilidad en Datos:** El campo `Datos` es de tipo `object`, permitiendo almacenar estructuras JSON complejas como `BsonDocument` o strings
- **Sin TTL:** Los logs se almacenan indefinidamente (no hay expiración automática configurada)

### DTO de Transferencia: `AuditLogDto`

Idéntico a la entidad, usado en la capa de aplicación para desacoplar el dominio de la API.

## 🧩 Patrones de Diseño Aplicados

1. **Clean Architecture:** Separación en capas con inversión de dependencias
2. **CQRS (Command Query Responsibility Segregation):** Separación entre escritura (Consumer) y lectura (Query)
3. **Mediator Pattern:** MediatR para desacoplar controllers de handlers
4. **Repository Pattern:** Abstracción de acceso a datos (`IAuditRepository`)
5. **Dependency Injection:** Inyección de dependencias en toda la aplicación
6. **Event-Driven Architecture:** Comunicación asíncrona vía eventos

## ⚠️ Deuda Técnica Detectada

### 1. 🔴 Configuración Hardcodeada de RabbitMQ (CRÍTICO)

**Ubicación:** `Program.cs` líneas 47-51

```csharp
cfg.Host("localhost", "/", h =>
{
    h.Username("guest");
    h.Password("guest");
});
```

**Problema:** Host, usuario y contraseña de RabbitMQ están hardcodeados. Imposibilita despliegue en diferentes entornos (staging, producción) sin recompilar.

**Impacto:** Seguridad, configurabilidad, escalabilidad.

**Recomendación:** Mover a `appsettings.json`:
```json
"RabbitMqSettings": {
  "Host": "localhost",
  "VirtualHost": "/",
  "Username": "guest",
  "Password": "guest"
}
```

### 2. 🟡 Manejo de Excepciones Silencioso

**Ubicación:** `AuditLogConsumer.cs` líneas 31-36

```csharp
try
{
    datosParaGuardar = BsonDocument.Parse(msg.Datos);
}
catch
{
    datosParaGuardar = msg.Datos;
}
```

**Problema:** El `catch` sin tipo ni logging puede ocultar errores de parsing importantes.

**Impacto:** Dificultad para diagnosticar problemas de formato de datos.

**Recomendación:** Agregar logging:
```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "No se pudo parsear Datos como JSON. Guardando como string.");
    datosParaGuardar = msg.Datos;
}
```

### 3. 🟡 Falta de Paginación en Endpoint GET

**Ubicación:** `GetAllLogsHandler.cs` línea 24

```csharp
var logsEntities = await _repository.ObtenerTodosAsync();
```

**Problema:** El endpoint `/api/logs` devuelve **todos** los logs sin paginación. Con miles de registros, puede causar:
- Timeouts
- Consumo excesivo de memoria
- Respuestas HTTP gigantes

**Impacto:** Performance, escalabilidad.

**Recomendación:** Implementar paginación con `skip` y `limit`:
```csharp
Task<List<AuditLog>> ObtenerTodosAsync(int pageNumber, int pageSize);
```

### 4. 🟢 Método de Repositorio No Utilizado

**Ubicación:** `IAuditRepository.cs` línea 13

```csharp
Task<List<AuditLog>> ObtenerLogsPorUsuarioAsync(string userId);
```

**Problema:** El método está definido e implementado pero **nunca se usa** en la aplicación (código muerto).

**Impacto:** Mantenibilidad, confusión.

**Recomendación:** Eliminarlo o exponerlo como endpoint si es necesario:
```csharp
[HttpGet("user/{userId}")]
public async Task<IActionResult> GetLogsByUser(string userId) { ... }
```

### 5. 🟢 GUID Serialization Explícita

**Ubicación:** `Program.cs` línea 15

```csharp
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
```

**Problema:** No es técnicamente un problema, pero está configurado globalmente sin documentación clara del por qué.

**Impacto:** Posibles conflictos si se usan otras librerías que esperan diferente representación de GUIDs en MongoDB.

**Recomendación:** Documentar en comentarios por qué se usa `GuidRepresentation.Standard`.

### 6. 🟡 Sin Autenticación/Autorización

**Observación:** El endpoint `/api/logs` es público. No hay validación de permisos para ver logs de auditoría, que pueden contener información sensible.

**Impacto:** Seguridad, compliance.

**Recomendación:** Implementar autenticación (JWT) y autorización basada en roles.

---

**Resumen:** La arquitectura es sólida y sigue buenas prácticas de Clean Architecture. Los principales puntos a mejorar son la configurabilidad, paginación y seguridad.
