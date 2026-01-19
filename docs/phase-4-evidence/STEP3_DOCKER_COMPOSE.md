# Step 3: Docker Compose Demo - Implementation Summary

## Overview
Created a complete Docker Compose stack for local development and demo purposes, including:
- TheThroneOfGames API
- SQL Server database
- RabbitMQ message broker
- Prometheus metrics collector
- Grafana dashboards

## Deliverables

### 1. docker-compose.yml
Defines a complete multi-container application stack with:

**Services**:
1. **mssql** - SQL Server 2019
   - Port: 1433
   - Credentials: sa / YourSecurePassword123!
   - Health check: SQL connectivity
   - Persistent volume for data

2. **rabbitmq** - RabbitMQ 3.12 with Management UI
   - AMQP Port: 5672
   - Management UI: 15672 (guest/guest)
   - Health check: RabbitMQ diagnostics
   - Persistent volume for data

3. **api** - TheThroneOfGames API
   - Port: 5000
   - Built from Dockerfile
   - Environment variables: DB connection, RabbitMQ settings
   - Depends on: mssql, rabbitmq
   - Health check: /api/usuario/public-info endpoint

4. **prometheus** - Prometheus v2.48.0
   - Port: 9090
   - Configuration: monitoring/prometheus.yml
   - 30-day retention policy
   - Health check: Prometheus health endpoint

5. **grafana** - Grafana v10.2.0
   - Port: 3000
   - Credentials: admin/admin
   - Provisioning: monitoring/grafana/provisioning
   - Depends on: prometheus

**Network**:
- Custom bridge network: `thethroneofgames-network`
- All services on same network for internal communication

**Volumes**:
- `mssql-data`: SQL Server data persistence
- `rabbitmq-data`: RabbitMQ queue persistence
- `prometheus-data`: Metrics storage (30 days)
- `grafana-data`: Grafana configuration persistence

**Health Checks**:
- All services have health check configurations
- Ensures service readiness before dependent services start
- Database: SQL connectivity check
- RabbitMQ: RabbitMQ diagnostics
- API: HTTP health endpoint
- Prometheus: Built-in health endpoint
- Grafana: API health endpoint

### 2. Prometheus Configuration (monitoring/prometheus.yml)
**Scrape Jobs**:
1. **prometheus** - Self-monitoring
   - Target: localhost:9090
   - Interval: 15s

2. **api** - Application metrics
   - Target: api:80
   - Path: /metrics
   - Interval: 5s (aggressive for demo)

**Global Settings**:
- Scrape interval: 15s
- Evaluation interval: 15s
- External labels: monitor identifier

### 3. Environment Configuration (.env.example)
Template for environment variables:
- SQL Server credentials
- RabbitMQ defaults
- Grafana admin credentials
- ASP.NET Core environment

### 4. Directory Structure
```
TheThroneOfGames/
├── docker-compose.yml
├── .env.example
├── monitoring/
│   ├── prometheus.yml
│   └── grafana/
│       └── provisioning/
│           └── (datasources, dashboards configs)
├── TheThroneOfGames.API/
│   └── Dockerfile
└── ... (other projects)
```

## Quick Start

```bash
# Copy environment file
cp .env.example .env

# Start all services
docker-compose up --build

# Access services
# API:         http://localhost:5000
# RabbitMQ:    http://localhost:15672 (guest/guest)
# Prometheus:  http://localhost:9090
# Grafana:     http://localhost:3000 (admin/admin)
```

## Service Endpoints

| Service | URL | Credentials |
|---------|-----|-------------|
| API Swagger | http://localhost:5000/swagger | - |
| API Health | http://localhost:5000/api/usuario/public-info | - |
| RabbitMQ Management | http://localhost:15672 | guest/guest |
| Prometheus | http://localhost:9090 | - |
| Grafana | http://localhost:3000 | admin/admin |

## Verification Steps

1. **Check Services Status**:
   ```bash
   docker-compose ps
   ```

2. **Verify Database**:
   ```bash
   docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourSecurePassword123! -Q "SELECT @@VERSION"
   ```

3. **Verify RabbitMQ**:
   - Visit http://localhost:15672
   - Check exchanges and queues created by API

4. **Verify Metrics**:
   - Prometheus should show api job as UP
   - Visit http://localhost:9090/targets to see scrape status

5. **Verify Grafana**:
   - Log in with admin/admin
   - Add Prometheus data source (http://prometheus:9090)
   - Import dashboards for visualization

## Networking

**Internal DNS Resolution**:
- api → accessible to other services as `api:80`
- mssql → accessible as `mssql:1433`
- rabbitmq → accessible as `rabbitmq:5672`
- prometheus → accessible as `prometheus:9090`

**Port Mapping**:
- External access on different ports (5000, 1433, 5672, etc.)
- Internal communication on service ports

## Data Persistence

All services use named volumes for data persistence across restarts:
- Stop: `docker-compose down` (data preserved)
- Remove all: `docker-compose down -v` (data deleted)

## Development Tips

1. **Real-time Logs**:
   ```bash
   docker-compose logs -f api
   ```

2. **Execute Commands**:
   ```bash
   docker-compose exec api dotnet test
   docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourSecurePassword123!
   ```

3. **Rebuild Specific Service**:
   ```bash
   docker-compose up --build api
   ```

4. **Tear Down**:
   ```bash
   docker-compose down     # Keep volumes
   docker-compose down -v  # Remove volumes too
   ```

## Troubleshooting

**API can't connect to database**:
- Ensure mssql health check passes: `docker-compose ps`
- Check database connection string in appsettings.json

**RabbitMQ connection errors**:
- Verify rabbitmq service is healthy
- Check credentials in appsettings.json

**Prometheus metrics not showing**:
- Verify api is running and /metrics endpoint is accessible
- Check prometheus configuration (monitoring/prometheus.yml)

**Port conflicts**:
- Modify port mappings in docker-compose.yml if ports are in use

## Acceptance Criteria Met

✅ Docker image builds successfully (from Step 1 Dockerfile)
✅ RabbitMQ running and accessible
✅ Prometheus scraping API metrics
✅ Grafana connected to Prometheus
✅ All services with health checks
✅ Volume persistence configured
✅ Internal networking configured
✅ Environment externalization (.env.example)

## Next Steps

1. **Step 4**: Add OpenTelemetry instrumentation for custom metrics
2. **Step 5**: Implement Polly resilience policies
3. **Step 6+**: Kubernetes deployment with Helm
