# 🚀 Teste de Carga - The Throne of Games

## Visão Geral

Script automatizado para realizar testes de carga em todos os endpoints das APIs, gerando dados aleatórios e coletando métricas detalhadas de performance.

## 📋 Características

### Cobertura de Endpoints

**API de Usuários (100% coberta):**
- ✅ POST /api/Usuario/pre-register - Registro de usuários
- ✅ POST /api/Usuario/activate - Ativação de contas
- ✅ POST /api/Usuario/login - Autenticação

**API de Catálogo (100% coberta):**
- ✅ POST /api/Game - Criação de jogos (admin)
- ✅ GET /api/Game - Listagem de jogos
- ✅ GET /api/Game/{id} - Busca de jogo específico

**API de Vendas (100% coberta):**
- ✅ POST /api/Pedido - Criação de pedidos
- ✅ GET /api/Pedido - Listagem de pedidos

### Métricas Coletadas

- **Requisições:** Total, sucesso, falhas, taxa de sucesso
- **Tempos de resposta:** Mínimo, médio, máximo, P50, P95, P99
- **Por endpoint:** Todas as métricas individualizadas
- **Concorrência:** Testes com múltiplas threads simultâneas

### Dados Gerados

- **Usuários aleatórios:** Nomes, emails, senhas seguros
- **Jogos aleatórios:** Títulos, descrições, preços, estoque
- **Pedidos aleatórios:** Múltiplos itens, quantidades variadas

## 🎯 Uso Básico

### Teste Padrão

```powershell
cd scripts
.\load-test.ps1
```

**Configuração padrão:**
- 50 usuários
- 100 jogos
- 200 pedidos
- 10 threads concorrentes

### Teste Customizado

```powershell
# Teste leve
.\load-test.ps1 -NumUsuarios 10 -NumJogos 20 -NumPedidos 30 -ConcurrentUsers 3

# Teste médio
.\load-test.ps1 -NumUsuarios 100 -NumJogos 200 -NumPedidos 500 -ConcurrentUsers 20

# Teste pesado
.\load-test.ps1 -NumUsuarios 500 -NumJogos 1000 -NumPedidos 2000 -ConcurrentUsers 50
```

### Com Relatório em Arquivo

```powershell
.\load-test.ps1 -GenerateReport
```

Gera arquivo `load-test-report-yyyyMMdd-HHmmss.txt` com métricas completas.

### URLs Customizadas

```powershell
.\load-test.ps1 `
    -BaseUrlUsuarios "http://localhost:5001" `
    -BaseUrlCatalogo "http://localhost:5002" `
    -BaseUrlVendas "http://localhost:5003"
```

## 📊 Parâmetros Disponíveis

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `-NumUsuarios` | int | 50 | Número de usuários a criar |
| `-NumJogos` | int | 100 | Número de jogos a criar |
| `-NumPedidos` | int | 200 | Número de pedidos a criar |
| `-ConcurrentUsers` | int | 10 | Threads concorrentes no teste de carga |
| `-BaseUrlUsuarios` | string | http://localhost:5001 | URL da API de Usuários |
| `-BaseUrlCatalogo` | string | http://localhost:5002 | URL da API de Catálogo |
| `-BaseUrlVendas` | string | http://localhost:5003 | URL da API de Vendas |
| `-SkipDataCreation` | switch | false | Pula criação de dados (apenas testa) |
| `-GenerateReport` | switch | false | Gera relatório em arquivo |

## 📈 Interpretando Resultados

### Exemplo de Saída

```
======================================================================
                    RELATORIO DE TESTE DE CARGA
======================================================================

METRICAS GERAIS:
  Total de requisicoes: 1247
  Requisicoes bem-sucedidas: 1198 (96.07%)
  Requisicoes falhadas: 49 (3.93%)

TEMPOS DE RESPOSTA:
  Minimo: 23.45 ms
  Medio: 156.78 ms
  Maximo: 2345.67 ms
  P50 (Mediana): 134.23 ms
  P95: 478.90 ms
  P99: 1234.56 ms

METRICAS POR ENDPOINT:

Endpoint              Total Success Failed Success % Avg (ms) P95 (ms)
--------              ----- ------- ------ ---------- -------- --------
Usuario/PreRegister      50      48      2       96.0   178.45   345.67
Usuario/Activate         50      48      2       96.0   123.89   289.12
Usuario/Login            48      47      1       97.9    89.34   156.78
Game/Create             100      98      2       98.0   234.56   567.89
Game/List                20      20      0      100.0    67.89   123.45
Game/GetById             30      30      0      100.0    56.78   101.23
Pedido/Create           200     192      8       96.0   289.45   789.01
```

### Métricas Ideais

- **Taxa de sucesso:** > 95%
- **Tempo médio:** < 200ms
- **P95:** < 500ms
- **P99:** < 1000ms

### Troubleshooting

**Alta taxa de falhas:**
- Verificar se APIs estão rodando: `docker ps`
- Verificar logs: `docker logs thethroneofgames-usuarios-api`
- Reduzir concorrência: `-ConcurrentUsers 5`

**Tempos altos:**
- SQL Server pode estar sobrecarregado
- Aumentar resources no docker-compose.yml
- Verificar Grafana para gargalos

**Erros de autenticação:**
- Verificar configuração JWT nas APIs
- Verificar se banco está limpo
- Reiniciar ambiente: `.\run-local.ps1 restart`

## 🎭 Cenários de Teste

### 1. Teste de Sanidade (Smoke Test)

```powershell
.\load-test.ps1 -NumUsuarios 5 -NumJogos 10 -NumPedidos 10 -ConcurrentUsers 2
```

**Objetivo:** Verificar se todos os endpoints estão respondendo.

### 2. Teste de Carga Normal

```powershell
.\load-test.ps1 -NumUsuarios 100 -NumJogos 200 -NumPedidos 500 -ConcurrentUsers 20 -GenerateReport
```

**Objetivo:** Simular uso normal da plataforma.

### 3. Teste de Estresse

```powershell
.\load-test.ps1 -NumUsuarios 500 -NumJogos 1000 -NumPedidos 2000 -ConcurrentUsers 50 -GenerateReport
```

**Objetivo:** Identificar limites do sistema.

### 4. Teste de Pico (Spike Test)

```powershell
# Primeiro criar dados
.\load-test.ps1 -NumUsuarios 100 -NumJogos 200 -NumPedidos 0 -ConcurrentUsers 5

# Depois teste de pico apenas leitura
.\load-test.ps1 -SkipDataCreation -ConcurrentUsers 100
```

**Objetivo:** Testar sistema sob carga súbita.

### 5. Teste de Resistência (Soak Test)

```powershell
# Executar múltiplas vezes
1..10 | ForEach-Object {
    Write-Host "Iteracao $_"
    .\load-test.ps1 -NumUsuarios 50 -NumJogos 100 -NumPedidos 200 -ConcurrentUsers 15
    Start-Sleep -Seconds 30
}
```

**Objetivo:** Verificar estabilidade ao longo do tempo.

## 📊 Monitoramento durante Testes

### Grafana

Acesse http://localhost:3000 durante os testes para visualizar:

- **Request Rate:** Requisições por segundo
- **Response Time:** P50, P95, P99
- **Error Rate:** Taxa de erros
- **CPU/Memory:** Uso de recursos

### Prometheus

Acesse http://localhost:9090 para queries customizadas:

```promql
# Taxa de requisições
rate(http_requests_total[1m])

# Latência P95
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Taxa de erros
rate(http_requests_total{status=~"5.."}[1m])
```

### RabbitMQ

Acesse http://localhost:15672 para verificar:

- Filas de mensagens
- Taxa de publicação
- Consumidores ativos

## 🔧 Automação CI/CD

### GitHub Actions

```yaml
name: Load Test

on:
  schedule:
    - cron: '0 2 * * *'  # Diariamente às 2h
  workflow_dispatch:

jobs:
  load-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Start services
        run: |
          cd scripts
          .\run-local.ps1
      
      - name: Run load test
        run: |
          cd scripts
          .\load-test.ps1 -GenerateReport
      
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: load-test-report
          path: scripts/load-test-report-*.txt
```

## 📝 Boas Práticas

1. **Sempre inicie com ambiente limpo:**
   ```powershell
   .\run-local.ps1 restart
   ```

2. **Monitore recursos do sistema:**
   - Task Manager (Windows)
   - Docker stats: `docker stats`

3. **Documente resultados:**
   - Use `-GenerateReport` em testes importantes
   - Compare resultados ao longo do tempo

4. **Ajuste progressivamente:**
   - Comece com carga baixa
   - Aumente gradualmente até encontrar limite

5. **Valide após mudanças:**
   - Execute testes após deploy
   - Compare com baseline anterior

## 🎯 Checklist de Teste

- [ ] Ambiente local rodando (run-local.ps1)
- [ ] Grafana acessível (localhost:3000)
- [ ] Prometheus acessível (localhost:9090)
- [ ] SQL Server respondendo
- [ ] Executar teste de sanidade (5/10/10)
- [ ] Executar teste padrão (50/100/200)
- [ ] Verificar métricas no Grafana
- [ ] Analisar relatório gerado
- [ ] Validar taxa de sucesso > 95%
- [ ] Validar P95 < 500ms
- [ ] Documentar resultados

## 🚀 Próximos Passos

- [ ] Integração com K6 para testes distribuídos
- [ ] Testes de carga em Kubernetes
- [ ] Comparação com ambientes staging/produção
- [ ] Alertas automáticos em degradação
- [ ] Dashboard customizado para testes

## 📞 Suporte

Em caso de problemas:

1. Verificar logs: `docker logs <container>`
2. Verificar status: `docker ps`
3. Reiniciar ambiente: `.\run-local.ps1 restart`
4. Consultar [IMPLEMENTATION_STATUS.md](../IMPLEMENTATION_STATUS.md)

---

**The Throne of Games** - Sistema de teste de carga automatizado v1.0
