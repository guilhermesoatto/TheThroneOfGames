# Release Notes - The Throne of Games

## v1.0.0 - Ambiente Local Completo (07/01/2026)

### 🎯 Destaques

Esta versão marca a conclusão da infraestrutura de desenvolvimento local completa, permitindo execução e teste de toda a plataforma com um único comando.

### ✨ Novos Recursos

#### Execução Local Completa
- **Docker Compose** configurado com todos os serviços necessários
- **Script de inicialização** automatizado (`run-local.ps1`)
- **Carga automática de dados** iniciais para testes
- **Monitoramento integrado** com Grafana + Prometheus

#### Microservices APIs
- ✅ **Usuarios API** (porta 5001) - Autenticação e gerenciamento de usuários
- ✅ **Catalogo API** (porta 5002) - Gerenciamento de jogos e catálogo
- ✅ **Vendas API** (porta 5003) - Processamento de pedidos e vendas

#### Infraestrutura
- ✅ **SQL Server 2019** (porta 1433) - Banco de dados principal
- ✅ **RabbitMQ 3.12** (portas 5672, 15672) - Message broker
- ✅ **Prometheus** (porta 9090) - Coleta de métricas
- ✅ **Grafana** (porta 3000) - Visualização de métricas

#### Monitoramento
- Dashboard Grafana pré-configurado com 8 painéis:
  - Taxa de requisições HTTP por API
  - Tempo de resposta P95
  - Uso de CPU por serviço
  - Uso de memória por serviço
  - Indicadores de status dos serviços
  - Total de requisições

### 🔧 Correções

- Removida dependência do projeto monolítico antigo nas APIs de microservices
- Corrigido `Program.cs` das 3 APIs para usar apenas suas próprias extensões
- Removido healthchecks problemáticos do Docker Compose
- Ajustado contexto de build no Docker Compose

### 📚 Documentação

- ✅ **LOCAL_EXECUTION_GUIDE.md** - Guia completo de execução local
- ✅ **MICROSERVICES_SETUP.md** - Documentação da arquitetura de microservices
- ✅ **KUBERNETES_STATUS.md** - Status da implementação Kubernetes
- ✅ **PHASE_42_COMPLETION_SUMMARY.md** - Resumo da fase 4.2

### 🚀 Como Usar

```powershell
# Iniciar ambiente completo
cd scripts
.\run-local.ps1 -LoadData

# Parar serviços
.\run-local.ps1 -Action stop

# Ver logs
.\run-local.ps1 -Action logs

# Verificar status
.\run-local.ps1 -Action status
```

### 📊 Dados Iniciais Carregados

Quando executado com `-LoadData`:
- **5 usuários**: 1 admin + 4 clientes
- **10 jogos**: Títulos populares (Last of Us II, God of War, Elden Ring, etc.)
- **10 pedidos**: Transações de teste

### 🎮 Acessos

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | - |
| RabbitMQ | http://localhost:15672 | guest / guest |
| Usuarios API | http://localhost:5001/swagger | - |
| Catalogo API | http://localhost:5002/swagger | - |
| Vendas API | http://localhost:5003/swagger | - |
| SQL Server | localhost:1433 | sa / YourSecurePassword123! |

### ⚙️ Requisitos Técnicos

- Docker Desktop
- PowerShell 5.1+
- 8GB RAM disponível
- 10GB espaço em disco

### 🔄 Próximos Passos

- [ ] Implementar testes de integração end-to-end
- [ ] Adicionar circuit breakers (Polly)
- [ ] Implementar health checks customizados
- [ ] Adicionar suporte a OpenTelemetry
- [ ] Criar pipelines CI/CD
- [ ] Implementar autoscaling no Kubernetes

### 🐛 Problemas Conhecidos

Nenhum problema crítico conhecido nesta versão.

### 📝 Notas de Upgrade

Esta é a primeira release com ambiente local completo. Não há procedimento de upgrade necessário.

---

**Data de Release**: 07 de Janeiro de 2026  
**Versão**: 1.0.0  
**Status**: ✅ Estável
