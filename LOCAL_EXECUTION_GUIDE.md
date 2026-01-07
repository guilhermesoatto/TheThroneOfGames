# 🚀 Guia de Execução Local - The Throne of Games

## Visão Geral

Este guia mostra como executar toda a plataforma localmente usando Docker Compose, incluindo monitoramento com Grafana e carga inicial de dados.

---

## 📋 Pré-requisitos

- **Docker Desktop** instalado e rodando
- **PowerShell** (Windows)
- Mínimo **8GB RAM** disponível
- Mínimo **10GB** de espaço em disco

---

## 🚀 Início Rápido (3 Comandos)

### 1. Iniciar Ambiente Completo
```powershell
cd scripts
.\run-local.ps1 -LoadData
```

Este comando:
- Constrói todas as imagens Docker
- Inicia todos os serviços (APIs, banco de dados, RabbitMQ, Prometheus, Grafana)
- Carrega dados iniciais automaticamente

### 2. Acessar Grafana
Abra o navegador: **http://localhost:3000**
- Usuário: `admin`
- Senha: `admin`

### 3. Visualizar Dashboard
O dashboard "The Throne of Games - Overview" será carregado automaticamente com métricas em tempo real.

---

## 📊 Serviços Disponíveis

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **Grafana** (Monitoramento) | http://localhost:3000 | admin / admin |
| **Prometheus** (Métricas) | http://localhost:9090 | - |
| **RabbitMQ** (Mensageria) | http://localhost:15672 | guest / guest |
| **Usuarios API** | http://localhost:5001/swagger | - |
| **Catalogo API** | http://localhost:5002/swagger | - |
| **Vendas API** | http://localhost:5003/swagger | - |
| **SQL Server** | localhost:1433 | sa / YourSecurePassword123! |

---

## 🎮 Comandos Disponíveis

### Gerenciamento de Serviços

```powershell
# Iniciar serviços
.\scripts\run-local.ps1

# Iniciar com carga de dados
.\scripts\run-local.ps1 -LoadData

# Parar serviços
.\scripts\run-local.ps1 -Action stop

# Reiniciar serviços
.\scripts\run-local.ps1 -Action restart

# Ver status
.\scripts\run-local.ps1 -Action status

# Ver logs
.\scripts\run-local.ps1 -Action logs
```

### Carga de Dados

```powershell
# Carregar dados manualmente (após iniciar serviços)
.\scripts\load-initial-data.ps1
```

---

## 📈 Monitoramento no Grafana

### Acesso ao Dashboard

1. Acesse: http://localhost:3000
2. Login: `admin` / `admin`
3. Dashboard "The Throne of Games - Overview" já estará configurado

### Métricas Disponíveis

O dashboard mostra:

#### 📊 Performance
- **HTTP Requests Rate**: Requisições por segundo em cada API
- **API Response Time (P95)**: Tempo de resposta percentil 95

#### 💻 Recursos
- **CPU Usage**: Uso de CPU por API
- **Memory Usage**: Uso de memória por API

#### ✅ Status
- **API Status**: Indicadores de saúde (verde = online, vermelho = offline)
- **Total Requests**: Total de requisições no último minuto

### Visualização em Tempo Real

- Atualização automática a cada 5 segundos
- Período padrão: últimos 15 minutos
- Ajustável no canto superior direito

---

## 🗄️ Dados Iniciais Carregados

Quando executado com `-LoadData`, o sistema carrega:

### Usuários (5)
- 1 Administrador
- 4 Clientes

### Jogos (10)
- The Last of Us Part II
- God of War Ragnarök
- Elden Ring
- Cyberpunk 2077
- Red Dead Redemption 2
- Horizon Forbidden West
- Spider-Man Miles Morales
- Hogwarts Legacy
- FIFA 24
- Call of Duty Modern Warfare III

### Transações (10)
- Pedidos aleatórios dos clientes
- 1 a 3 itens por pedido

---

## 🔍 Testando o Sistema

### 1. Verificar APIs

```powershell
# Usuarios API
Invoke-RestMethod http://localhost:5001/swagger

# Catalogo API
Invoke-RestMethod http://localhost:5002/swagger

# Vendas API
Invoke-RestMethod http://localhost:5003/swagger
```

### 2. Fazer Login

```powershell
$loginData = @{
    email = "admin@thethroneofgames.com"
    senha = "Admin@123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5001/api/usuario/login" -Method POST -Body $loginData -ContentType "application/json"

$token = $response.token
```

### 3. Listar Jogos

```powershell
$headers = @{
    "Authorization" = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5002/api/game" -Headers $headers
```

### 4. Criar Pedido

```powershell
$pedido = @{
    itens = @(
        @{ jogoId = 1; quantidade = 1 },
        @{ jogoId = 3; quantidade = 2 }
    )
} | ConvertTo-Json -Depth 10

Invoke-RestMethod -Uri "http://localhost:5003/api/pedido" -Method POST -Body $pedido -Headers $headers -ContentType "application/json"
```

---

## 📊 Visualizando Métricas

### No Grafana

1. Acesse http://localhost:3000
2. Vá para "Dashboards" → "The Throne of Games - Overview"
3. Observe as métricas sendo atualizadas em tempo real

### No Prometheus

1. Acesse http://localhost:9090
2. Execute queries:
   ```
   # Taxa de requisições
   rate(http_requests_received_total[5m])
   
   # Uso de CPU
   rate(process_cpu_seconds_total[5m])
   
   # Uso de memória
   process_working_set_bytes
   ```

---

## 🔧 Troubleshooting

### Problema: Docker não inicia

**Solução**: Certifique-se de que o Docker Desktop está rodando.

```powershell
# Verificar se Docker está ativo
docker ps
```

### Problema: Porta já em uso

**Solução**: Pare serviços que estejam usando as portas:
- 1433 (SQL Server)
- 5672, 15672 (RabbitMQ)
- 5001, 5002, 5003 (APIs)
- 3000 (Grafana)
- 9090 (Prometheus)

```powershell
# Windows: Ver processos usando porta
netstat -ano | findstr :3000
```

### Problema: Container não inicia

**Solução**: Ver logs do container específico

```powershell
docker-compose -f docker-compose.local.yml logs <service-name>

# Exemplos:
docker-compose -f docker-compose.local.yml logs usuarios-api
docker-compose -f docker-compose.local.yml logs sqlserver
```

### Problema: APIs não respondem

**Solução**: Aguardar inicialização completa (pode levar 1-2 minutos)

```powershell
# Verificar status dos containers
docker-compose -f docker-compose.local.yml ps

# Ver logs em tempo real
docker-compose -f docker-compose.local.yml logs -f
```

### Problema: Grafana não mostra métricas

**Solução**:
1. Verifique se Prometheus está rodando: http://localhost:9090
2. Verifique targets no Prometheus: http://localhost:9090/targets
3. Force refresh do dashboard no Grafana

---

## 🧹 Limpeza

### Parar todos os serviços

```powershell
.\scripts\run-local.ps1 -Action stop
```

### Remover todos os volumes (dados persistentes)

```powershell
docker-compose -f docker-compose.local.yml down -v
```

### Remover imagens

```powershell
docker rmi $(docker images -q thethroneofgames-*)
```

---

## 📝 Estrutura de Arquivos

```
TheThroneOfGames/
├── docker-compose.local.yml           ← Configuração Docker Compose
├── monitoring/
│   ├── prometheus/
│   │   └── prometheus.yml             ← Config Prometheus
│   └── grafana/
│       ├── provisioning/
│       │   ├── datasources/           ← Prometheus datasource
│       │   └── dashboards/            ← Dashboard config
│       └── dashboards/
│           └── overview-dashboard.json ← Dashboard Grafana
└── scripts/
    ├── run-local.ps1                  ← Script principal
    └── load-initial-data.ps1          ← Carga de dados
```

---

## 🎯 Próximos Passos

Após executar localmente:

1. **Explorar Grafana**: Visualize métricas e crie alertas
2. **Testar APIs**: Use Swagger para testar endpoints
3. **Simular Carga**: Execute requisições para ver métricas mudarem
4. **Personalizar Dashboard**: Adicione novos painéis no Grafana
5. **Monitorar RabbitMQ**: Veja filas de mensagens em tempo real

---

## 📚 Recursos Adicionais

- **Grafana Docs**: https://grafana.com/docs/
- **Prometheus Docs**: https://prometheus.io/docs/
- **Docker Compose Docs**: https://docs.docker.com/compose/

---

## 🆘 Suporte

Se encontrar problemas:

1. Verifique os logs: `.\scripts\run-local.ps1 -Action logs`
2. Veja o status: `.\scripts\run-local.ps1 -Action status`
3. Reinicie: `.\scripts\run-local.ps1 -Action restart`

---

**Status**: ✅ Pronto para Uso  
**Última Atualização**: Janeiro 2026  
**Versão**: 1.0
