# Guía de Configuración y Setup

## 📋 Prerrequisitos

- **.NET 8.0 SDK** - [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker** (opcional) - [Descargar](https://www.docker.com/products/docker-desktop)
- **Docker Compose** (opcional) - Incluido con Docker Desktop
- **MongoDB** (si no usas Docker)
- **RabbitMQ** (si no usas Docker)

## 🔧 Variables de Entorno

### Tabla de Configuración

| Variable | Descripción | Valor por Defecto | Requerida | Ejemplo |
|----------|-------------|-------------------|-----------|---------|
| `MongoDbSettings__ConnectionString` | Cadena de conexión a MongoDB | `mongodb://localhost:27017` | ✅ | `mongodb://user:pass@mongo:27017` |
| `MongoDbSettings__DatabaseName` | Nombre de la base de datos | `AuditDb` | ✅ | `AuditDb` |
| `MongoDbSettings__CollectionName` | Nombre de la colección | `AuditLogs` | ✅ | `AuditLogs` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` | ❌ | `Development`, `Staging`, `Production` |
| `ASPNETCORE_URLS` | URLs donde escucha el servidor | `http://*:5000` | ❌ | `http://*:7188` |
| `Logging__LogLevel__Default` | Nivel de logging | `Information` | ❌ | `Debug`, `Information`, `Warning`, `Error` |

### ⚠️ Configuración de RabbitMQ (Actualmente Hardcodeada)

**Nota:** La configuración de RabbitMQ está actualmente hardcodeada en `Program.cs` y **no se puede cambiar vía variables de entorno**. Ver sección de [Deuda Técnica](./architecture.md#⚠️-deuda-técnica-detectada).

**Valores actuales:**
- **Host:** `localhost`
- **Puerto:** `5672`
- **VirtualHost:** `/`
- **Usuario:** `guest`
- **Contraseña:** `guest`
- **Cola:** `audit-service-queue`

## 🐳 Configuración con Docker

### 1. Docker Compose (Recomendado para Desarrollo)

El proyecto incluye un `docker-compose.yml` que levanta MongoDB y RabbitMQ automáticamente.

#### Levantar servicios de infraestructura

```bash
# Iniciar MongoDB y RabbitMQ
docker-compose up -d

# Verificar que estén corriendo
docker-compose ps
```

**Servicios disponibles:**
- **RabbitMQ:** `localhost:5676` (AMQP), `localhost:15676` (Management UI)
- **MongoDB:** `localhost:27021`

#### Ejecutar la aplicación localmente

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar el servicio
dotnet run --project logs_services.api/logs_services.api.csproj
```

La aplicación estará disponible en `http://localhost:5000` (o el puerto en `launchSettings.json`).

#### Detener servicios

```bash
docker-compose down

# Para eliminar también los datos
docker-compose down -v
```

### 2. Dockerfile (Contenedor completo)

#### Construir la imagen

```bash
# Desde la raíz del proyecto
docker build -t logs-services:latest .
```

**Argumentos de build:**
- `APP_PORT`: Puerto de la aplicación (default: `7188`)

Ejemplo con puerto personalizado:
```bash
docker build --build-arg APP_PORT=8080 -t logs-services:latest .
```

#### Ejecutar el contenedor

**Opción 1: Red host (desarrollo local)**

```bash
docker run -d \
  --name logs-services \
  --network host \
  logs-services:latest
```

**Opción 2: Con mapeo de puertos y variables de entorno**

```bash
docker run -d \
  --name logs-services \
  -p 7188:7188 \
  -e MongoDbSettings__ConnectionString="mongodb://host.docker.internal:27021" \
  -e MongoDbSettings__DatabaseName="AuditDb" \
  -e MongoDbSettings__CollectionName="AuditLogs" \
  -e ASPNETCORE_ENVIRONMENT="Development" \
  logs-services:latest
```

**Nota:** Usa `host.docker.internal` en lugar de `localhost` para conectar desde el contenedor a servicios en el host.

#### Ver logs del contenedor

```bash
docker logs -f logs-services
```

#### Detener y eliminar

```bash
docker stop logs-services
docker rm logs-services
```

## ⚙️ Configuración de Archivos

### appsettings.json

Ubicación: `logs_services.api/appsettings.json`

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "AuditDb",
    "CollectionName": "AuditLogs"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

Ubicación: `logs_services.api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Puedes sobrescribir aquí configuraciones específicas para desarrollo (ej. logging más detallado, ConnectionString diferente).

### launchSettings.json

Ubicación: `logs_services.api/Properties/launchSettings.json`

Configura perfiles de ejecución para desarrollo con Visual Studio / Rider / VS Code.

## 🛠️ Comandos y Scripts

### Comandos de .NET

#### Restaurar dependencias

```bash
dotnet restore
```

#### Compilar el proyecto

```bash
# Compilar en modo Debug
dotnet build

# Compilar en modo Release
dotnet build -c Release
```

#### Ejecutar la aplicación

```bash
# Ejecutar el proyecto API
dotnet run --project logs_services.api/logs_services.api.csproj

# Ejecutar en modo Development
dotnet run --project logs_services.api/logs_services.api.csproj --environment Development

# Ejecutar en modo Release (optimizado)
dotnet run --project logs_services.api/logs_services.api.csproj -c Release
```

#### Ejecutar tests

```bash
# Ejecutar todos los tests del directorio tests/
dotnet test

# Ejecutar con cobertura (si está configurado)
dotnet test --collect:"XPlat Code Coverage"
```

#### Limpiar builds

```bash
dotnet clean
```

#### Publicar para producción

```bash
# Crear un build optimizado en /app/publish
dotnet publish logs_services.api/logs_services.api.csproj \
  -c Release \
  -o /app/publish
```

### Comandos útiles de Docker Compose

```bash
# Ver logs de todos los servicios
docker-compose logs -f

# Ver logs solo de MongoDB
docker-compose logs -f mongodb

# Ver logs solo de RabbitMQ
docker-compose logs -f rabbitmq

# Reiniciar un servicio específico
docker-compose restart mongodb

# Ver estado de los servicios
docker-compose ps

# Eliminar todo (incluyendo volúmenes)
docker-compose down -v
```

### Comandos útiles de MongoDB

```bash
# Conectar a MongoDB desde CLI
docker exec -it <mongodb-container-id> mongosh

# Una vez dentro de mongosh:
use AuditDb                    # Seleccionar base de datos
db.AuditLogs.find().pretty()   # Ver todos los logs
db.AuditLogs.countDocuments()  # Contar documentos
db.AuditLogs.find().sort({FechaOcurrencia: -1}).limit(10) # Últimos 10 logs
```

### Comandos útiles de RabbitMQ

```bash
# Acceder al Management UI
# http://localhost:15676
# Usuario: guest / Contraseña: guest

# Listar colas desde CLI
docker exec -it <rabbitmq-container-id> rabbitmqctl list_queues

# Ver mensajes en cola
docker exec -it <rabbitmq-container-id> rabbitmqctl list_queues name messages
```

## 🔐 Configuración de Seguridad

### Recomendaciones para Producción

#### 1. MongoDB

**No usar credenciales por defecto:**

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://admin:SecureP@ssw0rd@mongodb-prod:27017",
    "DatabaseName": "AuditDb",
    "CollectionName": "AuditLogs"
  }
}
```

**Configurar autenticación en MongoDB:**

```yaml
# docker-compose.yml
mongodb:
  image: mongo
  environment:
    MONGO_INITDB_ROOT_USERNAME: admin
    MONGO_INITDB_ROOT_PASSWORD: SecureP@ssw0rd
  ports:
    - "27017:27017"
```

#### 2. RabbitMQ

**Cambiar credenciales por defecto:**

⚠️ Actualmente requiere modificar el código en `Program.cs`. Recomendado refactorizar a configuración externa.

#### 3. HTTPS

**Habilitar HTTPS en producción:**

```bash
# Generar certificado de desarrollo
dotnet dev-certs https --trust

# Configurar en appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/path/to/cert.pfx",
          "Password": "cert-password"
        }
      }
    }
  }
}
```

#### 4. Variables de Entorno Sensibles

**Usar secretos en lugar de appsettings:**

```bash
# .NET User Secrets (desarrollo)
dotnet user-secrets init --project logs_services.api
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb://..." --project logs_services.api

# Docker Secrets (producción)
docker secret create mongo_connection_string mongo_conn.txt
```

## 🚀 Despliegue

### Azure App Service

```bash
# Publicar en Azure
az webapp up --name logs-services-app \
  --resource-group my-resource-group \
  --runtime "DOTNET|8.0"
```

### Kubernetes

```yaml
# Ejemplo de deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: logs-services
spec:
  replicas: 3
  selector:
    matchLabels:
      app: logs-services
  template:
    metadata:
      labels:
        app: logs-services
    spec:
      containers:
      - name: logs-services
        image: logs-services:latest
        ports:
        - containerPort: 7188
        env:
        - name: MongoDbSettings__ConnectionString
          valueFrom:
            secretKeyRef:
              name: mongo-secret
              key: connection-string
```

## 🧪 Verificación de Instalación

### Verificar que el servicio está corriendo

```bash
curl http://localhost:5000/api/logs
```

**Respuesta esperada:** `[]` (lista vacía si no hay logs) o `200 OK` con datos.

### Verificar Swagger (Development)

```bash
curl http://localhost:5000/swagger/index.html
```

### Verificar MongoDB

```bash
# Desde línea de comandos
mongosh "mongodb://localhost:27021/AuditDb" --eval "db.AuditLogs.countDocuments()"
```

### Verificar RabbitMQ

```bash
# Acceder al Management UI
open http://localhost:15676
# Usuario: guest / Contraseña: guest
```

Buscar la cola `audit-service-queue` en la pestaña "Queues".

## ❗ Troubleshooting

### Error: "Unable to connect to MongoDB"

**Causa:** MongoDB no está corriendo o la ConnectionString es incorrecta.

**Solución:**
```bash
# Verificar que MongoDB esté corriendo
docker ps | grep mongo

# Si no está, iniciarlo
docker-compose up -d mongodb
```

### Error: "RabbitMQ connection failed"

**Causa:** RabbitMQ no está corriendo o el puerto es incorrecto.

**Solución:**
```bash
# Verificar que RabbitMQ esté corriendo
docker ps | grep rabbitmq

# Si no está, iniciarlo
docker-compose up -d rabbitmq

# Verificar que esté escuchando en el puerto correcto
docker exec <rabbitmq-container-id> rabbitmqctl status
```

### Puerto ya en uso

**Error:** `System.IO.IOException: Failed to bind to address http://127.0.0.1:5000`

**Solución:** Cambiar el puerto en `launchSettings.json` o vía variable de entorno:

```bash
export ASPNETCORE_URLS="http://*:5001"
dotnet run --project logs_services.api/logs_services.api.csproj
```

### No se pueden ver logs en Swagger

**Causa:** Swagger solo está habilitado en modo Development.

**Solución:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project logs_services.api/logs_services.api.csproj
```

---

## 📚 Recursos Adicionales

- [Documentación oficial de .NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [MongoDB .NET Driver](https://www.mongodb.com/docs/drivers/csharp/)
- [MassTransit Documentation](https://masstransit.io/)
- [Docker Compose Reference](https://docs.docker.com/compose/)

---

**Última actualización:** 2024-01-20
