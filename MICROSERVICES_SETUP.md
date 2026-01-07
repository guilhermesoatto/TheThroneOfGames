# TheThroneOfGames - Microservices Architecture

## Overview

This document describes the migration of the TheThroneOfGames platform from a monolithic architecture to a microservices architecture based on Bounded Contexts (Domain-Driven Design).

## Bounded Contexts & Microservices

### 1. **Usuarios Service** (User Management)
- **Project**: `GameStore.Usuarios.API`
- **Port**: 5001 (development)
- **Responsibilities**:
  - User registration and account management
  - User activation via token
  - Authentication (JWT token generation)
  - User profile management
  
**Key Endpoints**:
- `POST /api/usuario/pre-register` - Register new user
- `POST /api/usuario/activate?activationToken={token}` - Activate account
- `POST /api/usuario/login` - Authenticate user
- `GET /api/usuario/profile` - Get user profile (requires auth)

### 2. **Catalogo Service** (Game Catalog)
- **Project**: `GameStore.Catalogo.API`
- **Port**: 5002 (development)
- **Responsibilities**:
  - Game catalog management
  - Game search and filtering
  - Game availability tracking
  - Game purchase integration
  
**Key Endpoints**:
- `GET /api/game` - Get all games
- `GET /api/game/available` - Get available games for user (requires auth)
- `GET /api/game/owned` - Get user's owned games (requires auth)
- `POST /api/game/{gameId}/buy` - Purchase a game (requires auth)
- `GET /api/game/search?name=query` - Search games by name

### 3. **Vendas Service** (Sales & Orders)
- **Project**: `GameStore.Vendas.API`
- **Port**: 5003 (development)
- **Responsibilities**:
  - Order management
  - Shopping cart management
  - Order finalization and payment
  - Purchase history
  
**Key Endpoints**:
- `GET /api/pedidos` - Get user's orders (requires auth)
- `GET /api/pedidos/{id}` - Get order details (requires auth)
- `POST /api/pedidos` - Create new order (requires auth)
- `POST /api/pedidos/{pedidoId}/itens` - Add item to order (requires auth)
- `POST /api/pedidos/{pedidoId}/finalizar` - Finalize order (requires auth)
- `DELETE /api/pedidos/{id}` - Cancel order (requires auth)

## Architecture Layers

Each microservice follows Clean Architecture with the following layers:

```
GameStore.Usuarios/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Repositories/
├── Application/
│   ├── Commands/
│   ├── DTOs/
│   ├── Handlers/
│   ├── Interfaces/
│   └── Services/
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   └── DbContexts/
│   └── Extensions/
└── API/
    ├── Controllers/
    ├── Models/
    └── Program.cs
```

## Shared Infrastructure

### GameStore.Common
Provides shared messaging and event infrastructure:
- **IEventBus**: Abstraction for pub/sub messaging
- **SimpleEventBus**: In-memory implementation (development)
- **RabbitMqAdapter**: RabbitMQ implementation (production)
- **BaseEventConsumer**: Generic consumer for async event processing
- **EventConsumerService**: Hosted service for managing consumers

### Event Bus Configuration
Set via `appsettings.json`:
```json
{
  "EventBus": {
    "UseRabbitMq": false,
    "RabbitMq": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    }
  }
}
```

## Running Microservices Locally

### Prerequisites
- .NET 9.0 SDK
- SQL Server (or LocalDB)
- RabbitMQ (optional, for async messaging)

### Running Individual Services

#### Usuarios Service
```bash
cd GameStore.Usuarios.API
dotnet run
# Available at: http://localhost:5001
```

#### Catalogo Service
```bash
cd GameStore.Catalogo.API
dotnet run
# Available at: http://localhost:5002
```

#### Vendas Service
```bash
cd GameStore.Vendas.API
dotnet run
# Available at: http://localhost:5003
```

### Using Docker Compose (All Services)
```bash
docker-compose up -d
```

This starts:
- SQL Server on port 1433
- RabbitMQ on ports 5672 (AMQP) and 15672 (Management UI)
- Usuarios API on port 5001
- Catalogo API on port 5002
- Vendas API on port 5003

## Authentication

All services use JWT authentication with the following configuration:

**JWT Settings** (from appsettings.json):
```json
{
  "Jwt": {
    "Key": "your-super-secret-key-that-is-at-least-32-characters-long!",
    "Issuer": "TheThroneOfGames",
    "Audience": "TheThroneOfGamesClient",
    "ExpirationMinutes": 60
  }
}
```

**Token Usage**:
```
Authorization: Bearer <token>
```

## Inter-Service Communication

Services communicate asynchronously via events published through the EventBus:

### Event Types
- `UsuarioAtivadoEvent` - User account activated
- `GameCompradoEvent` - Game purchased
- `PedidoFinalizadoEvent` - Order finalized

### Event Consumers
Each service subscribes to relevant events:
- **Usuarios Service**: Listens to `GameCompradoEvent`, `PedidoFinalizadoEvent`
- **Catalogo Service**: Listens to `UsuarioAtivadoEvent`
- **Vendas Service**: Publishes `PedidoFinalizadoEvent`

## Database

All services share a common SQL Server database `GameStore` with the following tables:
- Users
- Games
- Purchases
- Orders
- OrderItems
- Promotions

Connection string (in appsettings.json):
```
Server=localhost,1433;Database=GameStore;User Id=sa;Password=TheThroneOfGames@123;Encrypt=false;
```

## Monitoring & Logging

### Logging Configuration
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

Development environment enables Debug-level logging for better troubleshooting.

## API Documentation

Each service exposes Swagger/OpenAPI documentation:
- Usuarios: http://localhost:5001/swagger
- Catalogo: http://localhost:5002/swagger
- Vendas: http://localhost:5003/swagger

## Testing

Run unit tests for each service:
```bash
cd Test
dotnet test
```

## Future Enhancements

1. **API Gateway**: Implement API Gateway pattern for unified entry point
2. **Service Discovery**: Add Consul or similar for dynamic service discovery
3. **Load Balancing**: Deploy behind load balancer for horizontal scaling
4. **Circuit Breaker**: Implement Polly for resilient inter-service calls
5. **Distributed Tracing**: Add OpenTelemetry for observability
6. **Kubernetes Deployment**: Create K8s manifests for orchestration

## Migration Notes

- **Original API**: `TheThroneOfGames.API` remains functional for backward compatibility
- **Gradual Rollout**: Can route clients to individual services or through API Gateway
- **Data Integrity**: Async messaging via events ensures eventual consistency
- **Shared DbContext**: Services currently share the same database; can be separated per service as needed

