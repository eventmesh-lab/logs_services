# Documentación de API

## 📡 Endpoints REST

### Base URL

- **Development:** `http://localhost:5000` (o puerto configurado en `launchSettings.json`)
- **Docker:** `http://localhost:7188`
- **Swagger UI:** `http://localhost:5000/swagger` (solo en modo Development)

---

## Endpoints Disponibles

### 1. GET /api/logs

Obtiene todos los logs de auditoría almacenados en el sistema.

#### Request

```http
GET /api/logs HTTP/1.1
Host: localhost:5000
Accept: application/json
```

**Query Parameters:** Ninguno (sin paginación actualmente)

**Headers:**
- `Accept: application/json`

**Autenticación:** No requerida (⚠️ endpoint público)

#### Response

**Success (200 OK)**

```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "eventoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "servicioOrigen": "users-service",
    "usuarioId": "user-12345",
    "tipoAccion": "CREATE",
    "datos": {
      "entity": "User",
      "email": "john.doe@example.com",
      "role": "admin"
    },
    "fechaOcurrencia": "2024-01-15T14:30:00Z",
    "nivel": "INFO"
  },
  {
    "id": "507f1f77bcf86cd799439012",
    "eventoId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "servicioOrigen": "orders-service",
    "usuarioId": "user-67890",
    "tipoAccion": "UPDATE",
    "datos": {
      "orderId": "ORD-2024-001",
      "status": "SHIPPED",
      "previousStatus": "PENDING"
    },
    "fechaOcurrencia": "2024-01-15T15:45:00Z",
    "nivel": "INFO"
  },
  {
    "id": "507f1f77bcf86cd799439013",
    "eventoId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
    "servicioOrigen": "payments-service",
    "usuarioId": "user-11111",
    "tipoAccion": "DELETE",
    "datos": {
      "paymentMethodId": "pm_1234567890",
      "reason": "User request"
    },
    "fechaOcurrencia": "2024-01-15T16:00:00Z",
    "nivel": "WARNING"
  }
]
```

**Empty Result (200 OK)**

```json
[]
```

**Error Response (500 Internal Server Error)**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-abc123-def456-00"
}
```

#### Modelo de Respuesta

| Campo | Tipo | Descripción | Ejemplo |
|-------|------|-------------|---------|
| `id` | string | ID único del log en MongoDB (ObjectId) | `"507f1f77bcf86cd799439011"` |
| `eventoId` | string (GUID) | ID del evento original | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| `servicioOrigen` | string | Nombre del microservicio que generó el evento | `"users-service"` |
| `usuarioId` | string | ID del usuario que ejecutó la acción | `"user-12345"` |
| `tipoAccion` | string | Tipo de operación | `"CREATE"`, `"UPDATE"`, `"DELETE"`, `"LOGIN"`, etc. |
| `datos` | object/string | Datos adicionales del evento (estructura flexible) | `{ "entity": "User", ... }` |
| `fechaOcurrencia` | string (ISO 8601) | Timestamp de cuando ocurrió la acción | `"2024-01-15T14:30:00Z"` |
| `nivel` | string | Nivel de severidad del log | `"INFO"`, `"WARNING"`, `"ERROR"`, `"CRITICAL"` |

#### Notas

- ⚠️ **Sin paginación:** Este endpoint devuelve TODOS los logs. Puede ser lento con grandes volúmenes.
- ⚠️ **Sin filtros:** No se puede filtrar por fecha, usuario, servicio, etc.
- ⚠️ **Ordenamiento:** Los logs se devuelven ordenados por `fechaOcurrencia` descendente (más reciente primero).
- El campo `datos` puede contener estructuras JSON complejas o strings, dependiendo de cómo el servicio origen envió el evento.

---

## 🔄 Contrato de Eventos (RabbitMQ)

### Event: IAuditLogCreated

Este es el evento que los microservicios externos deben publicar en RabbitMQ para registrar logs de auditoría.

#### Configuración de RabbitMQ

- **Queue:** `audit-service-queue`
- **Exchange:** Default exchange (configurado por MassTransit)
- **Host:** `localhost:5672` (configurable)
- **Virtual Host:** `/`

#### Estructura del Evento

```csharp
public interface IAuditLogCreated
{
    Guid Id { get; }                  // ID único del evento
    string ServicioOrigen { get; }     // Nombre del servicio origen
    string UsuarioId { get; }          // ID del usuario
    string TipoAccion { get; }         // Tipo de acción
    string Datos { get; }              // JSON serializado
    DateTime FechaOcurrencia { get; }  // Timestamp
    string Nivel { get; }              // Nivel de severidad
}
```

#### Ejemplo de Mensaje (JSON)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "servicioOrigen": "users-service",
  "usuarioId": "user-12345",
  "tipoAccion": "CREATE",
  "datos": "{\"entity\":\"User\",\"email\":\"john.doe@example.com\",\"role\":\"admin\"}",
  "fechaOcurrencia": "2024-01-15T14:30:00Z",
  "nivel": "INFO"
}
```

#### Ejemplo de Publicación (C# con MassTransit)

```csharp
// En el microservicio productor
public class UserService
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task CreateUserAsync(User user)
    {
        // ... lógica de creación de usuario ...
        
        // Publicar evento de auditoría
        await _publishEndpoint.Publish<IAuditLogCreated>(new
        {
            Id = Guid.NewGuid(),
            ServicioOrigen = "users-service",
            UsuarioId = user.Id,
            TipoAccion = "CREATE",
            Datos = JsonSerializer.Serialize(new 
            { 
                entity = "User",
                email = user.Email,
                role = user.Role 
            }),
            FechaOcurrencia = DateTime.UtcNow,
            Nivel = "INFO"
        });
    }
}
```

#### Validaciones del Consumer

El `AuditLogConsumer` realiza las siguientes transformaciones al recibir el evento:

1. **Parseo de JSON:** Intenta parsear el campo `Datos` como `BsonDocument` para almacenarlo estructurado en MongoDB.
2. **Fallback:** Si el parseo falla, guarda `Datos` como string plano.
3. **Persistencia:** Inserta el log en MongoDB de forma asíncrona.

#### Recomendaciones para Productores

- **Serializar correctamente:** Asegúrate de que el campo `Datos` contenga JSON válido.
- **Usar UTC:** Siempre envía `FechaOcurrencia` en UTC para evitar problemas de zona horaria.
- **Niveles consistentes:** Usa niveles estándar: `INFO`, `WARNING`, `ERROR`, `CRITICAL`.
- **Incluir contexto:** En `Datos`, incluye suficiente información para entender la acción sin consultar otros servicios.

---

## 🧪 Ejemplos de Uso

### Ejemplo 1: Obtener todos los logs (cURL)

```bash
curl -X GET "http://localhost:5000/api/logs" \
     -H "accept: application/json"
```

### Ejemplo 2: Obtener todos los logs (PowerShell)

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/logs" `
                  -Method Get `
                  -ContentType "application/json"
```

### Ejemplo 3: Obtener todos los logs (JavaScript/Fetch)

```javascript
fetch('http://localhost:5000/api/logs', {
  method: 'GET',
  headers: {
    'Accept': 'application/json'
  }
})
  .then(response => response.json())
  .then(logs => console.log(logs))
  .catch(error => console.error('Error:', error));
```

### Ejemplo 4: Publicar evento desde Python

```python
import pika
import json
import uuid
from datetime import datetime

# Conexión a RabbitMQ
connection = pika.BlockingConnection(
    pika.ConnectionParameters('localhost', 5672)
)
channel = connection.channel()

# Crear evento
event = {
    "id": str(uuid.uuid4()),
    "servicioOrigen": "python-service",
    "usuarioId": "user-999",
    "tipoAccion": "UPDATE",
    "datos": json.dumps({"key": "value"}),
    "fechaOcurrencia": datetime.utcnow().isoformat() + "Z",
    "nivel": "INFO"
}

# Publicar
channel.basic_publish(
    exchange='',
    routing_key='audit-service-queue',
    body=json.dumps(event)
)

connection.close()
print("Evento de auditoría publicado")
```

---

## 📝 Notas Adicionales

### Swagger/OpenAPI

En modo Development, puedes acceder a la documentación interactiva en:

```
http://localhost:5000/swagger
```

Desde allí puedes probar el endpoint directamente desde el navegador.

### Limitaciones Actuales

1. **Sin autenticación:** Los endpoints son públicos.
2. **Sin paginación:** El endpoint GET devuelve todos los registros.
3. **Sin filtros:** No se puede filtrar por fecha, usuario, servicio o nivel.
4. **Solo lectura:** No hay endpoints para crear/actualizar/eliminar logs manualmente (solo vía eventos).

### Futuras Mejoras Sugeridas

- [ ] Agregar paginación: `GET /api/logs?page=1&pageSize=50`
- [ ] Agregar filtros: `GET /api/logs?servicioOrigen=users-service&nivel=ERROR`
- [ ] Agregar búsqueda por rango de fechas: `GET /api/logs?desde=2024-01-01&hasta=2024-01-31`
- [ ] Agregar endpoint por usuario: `GET /api/logs/user/{userId}`
- [ ] Implementar autenticación JWT
- [ ] Agregar endpoint de estadísticas: `GET /api/logs/stats`
