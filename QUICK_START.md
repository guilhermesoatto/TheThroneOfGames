# Quick Start Guide - TheThroneOfGames Microservices

## Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose (optional, for containerized setup)
- SQL Server or Docker (for database)
- RabbitMQ (optional, for async messaging)

## Option 1: Run Locally (Development)

### Step 1: Database Setup
Ensure SQL Server is running on `localhost,1433` with:
- Database: `GameStore`
- Username: `sa`
- Password: `TheThroneOfGames@123`

### Step 2: Start Services (in separate terminals)

#### Terminal 1 - Usuarios Service
```bash
cd GameStore.Usuarios.API
dotnet run
# Available at: http://localhost:5001
# Swagger: http://localhost:5001/swagger
```

#### Terminal 2 - Catalogo Service
```bash
cd GameStore.Catalogo.API
dotnet run
# Available at: http://localhost:5002
# Swagger: http://localhost:5002/swagger
```

#### Terminal 3 - Vendas Service
```bash
cd GameStore.Vendas.API
dotnet run
# Available at: http://localhost:5003
# Swagger: http://localhost:5003/swagger
```

## Option 2: Docker Compose (Recommended for Full Stack)

### Start All Services
```bash
docker-compose up -d
```

This starts:
- **SQL Server** - localhost:1433
- **RabbitMQ** - localhost:5672 (Management UI: 15672)
- **Usuarios API** - http://localhost:5001
- **Catalogo API** - http://localhost:5002
- **Vendas API** - http://localhost:5003
- **Prometheus** - http://localhost:9090
- **Grafana** - http://localhost:3000

### Stop All Services
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f usuarios-api    # Usuarios service
docker-compose logs -f catalogo-api    # Catalogo service
docker-compose logs -f vendas-api      # Vendas service
docker-compose logs -f mssql          # Database
docker-compose logs -f rabbitmq       # Message broker
```

## Testing the APIs

### 1. Register a New User

```bash
curl -X POST http://localhost:5001/api/usuario/pre-register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "name": "John Doe",
    "password": "SecurePassword123!",
    "role": "User"
  }'
```

Response: `{"message": "Usuário pré-registrado com sucesso! E-mail de ativação enviado.", "activationToken": "..."}`

### 2. Activate User Account

```bash
curl -X POST "http://localhost:5001/api/usuario/activate?activationToken=YOUR_TOKEN_HERE"
```

### 3. Login and Get JWT Token

```bash
curl -X POST http://localhost:5001/api/usuario/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

Response: `{"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."}`

### 4. Get All Games

```bash
curl http://localhost:5002/api/game
```

### 5. Get User's Profile (with Authentication)

```bash
curl -X GET http://localhost:5001/api/usuario/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 6. Get Available Games for User

```bash
curl -X GET http://localhost:5002/api/game/available \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 7. Purchase a Game

```bash
curl -X POST http://localhost:5002/api/game/{gameId}/buy \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 8. Create an Order

```bash
curl -X POST http://localhost:5003/api/pedidos \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "reference": "ORDER123",
    "orderDate": "2024-01-15T10:30:00Z"
  }'
```

## Access Points

### Services

| Service | URL | Swagger |
|---------|-----|---------|
| Usuarios | http://localhost:5001 | /swagger |
| Catalogo | http://localhost:5002 | /swagger |
| Vendas | http://localhost:5003 | /swagger |
| Monolithic API | http://localhost:5000 | /swagger |

### Infrastructure

| Service | URL | Credentials |
|---------|-----|-------------|
| SQL Server | localhost:1433 | sa / TheThroneOfGames@123 |
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| Prometheus | http://localhost:9090 | - |
| Grafana | http://localhost:3000 | admin / admin |

## Configuration

### Change Ports
Modify docker-compose.yml:
```yaml
usuarios-api:
  ports:
    - "5001:80"  # Change first number to desired port
```

### Change Database Password
Update in docker-compose.yml (mssql service):
```yaml
SA_PASSWORD: YourNewPassword123!
```

Also update in each service's appsettings.json:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=GameStore;User Id=sa;Password=YourNewPassword123!;Encrypt=false;"
}
```

### Enable RabbitMQ
Update appsettings.json in each service:
```json
"EventBus": {
  "UseRabbitMq": true,
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

## Building Docker Images

### Build All Images
```bash
docker-compose build
```

### Build Specific Service
```bash
docker build -f GameStore.Usuarios.API/Dockerfile -t usuarios-api:latest .
docker build -f GameStore.Catalogo.API/Dockerfile -t catalogo-api:latest .
docker build -f GameStore.Vendas.API/Dockerfile -t vendas-api:latest .
```

## Troubleshooting

### Port Already in Use
```bash
# Find process using port
lsof -i :5001  # macOS/Linux
netstat -ano | findstr :5001  # Windows

# Kill process
kill -9 PID  # macOS/Linux
taskkill /PID PID /F  # Windows
```

### Database Connection Issues
1. Verify SQL Server is running
2. Check connection string in appsettings.json
3. Ensure database `GameStore` exists
4. Verify sa user password is correct

### RabbitMQ Connection Issues
1. Verify RabbitMQ is running
2. Check hostname and port in appsettings.json
3. Verify credentials (guest/guest by default)
4. Check if UseRabbitMq is set to true

### Service Won't Start
1. Check logs: `docker-compose logs <service>`
2. Verify all dependencies are running
3. Check appsettings.json for valid configuration
4. Ensure ports are available

## Additional Resources

- [MICROSERVICES_SETUP.md](./MICROSERVICES_SETUP.md) - Detailed architecture guide
- [PHASE_4_COMPLETION_REPORT.md](./PHASE_4_COMPLETION_REPORT.md) - Complete implementation report
- Docker Compose Reference: https://docs.docker.com/compose/
- .NET 9 Docs: https://learn.microsoft.com/en-us/dotnet/
- JWT Authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
- RabbitMQ Documentation: https://www.rabbitmq.com/documentation.html

## Support

For issues or questions:
1. Check service logs: `docker-compose logs <service>`
2. Review appsettings.json configuration
3. Verify database and message broker connectivity
4. Check Swagger documentation for each service

---

**Last Updated**: 2024  
**Version**: 1.0  
**Status**: Ready for Development & Testing
