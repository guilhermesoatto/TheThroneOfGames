# Performance Testing - The Throne of Games

## 📊 Visão Geral

Este diretório contém scripts e configurações para testes de performance dos microservices The Throne of Games.

## 🎯 Objetivos dos Testes

1. **Medir Capacidade**: Determinar quantas requisições cada container pode processar
2. **Validar HPA**: Fornecer baseline para configuração do Horizontal Pod Autoscaler
3. **Garantir Qualidade**: Assegurar que os microservices atendem aos requisitos de latência e throughput
4. **CI/CD Integration**: Validar performance automaticamente em cada build

## 📁 Arquivos

### Scripts de Teste

| Arquivo | Descrição | Uso |
|---------|-----------|-----|
| `performance-test.ps1` | Teste completo de performance (60s, 10 usuários) | Desenvolvimento/Staging |
| `quick-performance-test.ps1` | Teste rápido (30s, 5 usuários) | CI/CD Pipeline |
| `performance-config.yml` | Configuração de thresholds e baselines | Referência |

### GitHub Actions

| Arquivo | Descrição |
|---------|-----------|
| `.github/workflows/ci-cd-pipeline.yml` | Pipeline completo com testes de performance |

## 🚀 Como Usar

### 1. Teste Local Completo

```powershell
# Teste padrão (60 segundos, 10 usuários)
.\scripts\performance-test.ps1

# Teste customizado
.\scripts\performance-test.ps1 `
    -BaseUrl "http://localhost" `
    -Duration 120 `
    -ConcurrentUsers 20 `
    -RampUpTime 15 `
    -OutputFile "my-results.json"
```

### 2. Teste Rápido (CI/CD)

```powershell
# Teste rápido (30 segundos, 5 usuários)
.\scripts\quick-performance-test.ps1

# Com parâmetros
.\scripts\quick-performance-test.ps1 `
    -BaseUrl "http://localhost" `
    -Duration 30 `
    -ConcurrentUsers 5
```

### 3. Teste de Stress

```powershell
# Teste prolongado com muitos usuários
.\scripts\performance-test.ps1 `
    -Duration 300 `
    -ConcurrentUsers 50 `
    -RampUpTime 30
```

## 📊 Métricas Coletadas

### Por Microservice

- **Total de Requisições**: Número total de requisições executadas
- **Taxa de Sucesso**: Percentual de requisições bem-sucedidas
- **Throughput**: Requisições por segundo (req/s)
- **Latência**:
  - Média
  - Mínima
  - Máxima
  - P50 (mediana)
  - P95 (95º percentil)
  - P99 (99º percentil)

### Agregadas

- **Throughput Médio**: Média entre todos os microservices
- **Latência Média**: Média entre todos os microservices
- **Taxa de Sucesso Média**: Média entre todos os microservices

## ✅ Critérios de Aprovação

Um teste é considerado **APROVADO** quando:

1. ✅ **Taxa de Sucesso** ≥ 95%
2. ✅ **Latência Média** < 2000ms
3. ✅ **P95** < 5000ms

Configurações em: `performance-config.yml`

## 📈 Baseline para HPA

Os resultados dos testes fornecem o baseline para configurar o HPA:

```yaml
# Exemplo de configuração baseada em testes
performance:
  baselines:
    usuarios-api:
      targetThroughput: 100 req/s  # Capacidade medida
      cpuThreshold: 70%            # Escala aos 70%
      
    catalogo-api:
      targetThroughput: 120 req/s
      cpuThreshold: 70%
      
    vendas-api:
      targetThroughput: 80 req/s
      cpuThreshold: 70%
```

### Cálculo da Margem de Segurança

O HPA deve ser configurado para escalar antes de atingir a capacidade máxima:

- **Capacidade Máxima Medida**: X req/s
- **Threshold HPA**: 70% de X req/s
- **Margem de Segurança**: 30%

**Exemplo:**
- Teste mediu: 100 req/s por container
- HPA configurado: Escala ao atingir 70 req/s
- Margem: 30 req/s para absorver picos

## 🔄 CI/CD Integration

### GitHub Actions Workflow

O pipeline executa automaticamente:

1. **Build & Unit Tests**: Compila e executa testes unitários
2. **Docker Build**: Cria imagens Docker dos microservices
3. **Performance Tests**: Executa `quick-performance-test.ps1`
4. **Security Scan**: Scanner de vulnerabilidades (Trivy)
5. **Summary Report**: Gera relatório consolidado

### Triggers

- ✅ Push em `master`, `main`, `develop`
- ✅ Pull Requests para `master`, `main`
- ✅ Manual (workflow_dispatch)

### Artifacts Gerados

- `test-results`: Resultados dos testes unitários
- `docker-image-*`: Imagens Docker buildadas
- `performance-results`: JSON com resultados de performance
- `trivy-results-*`: Relatórios de segurança

## 📊 Interpretando Resultados

### Exemplo de Output

```
═══════════════════════════════════════════════════════════
                    RESUMO FINAL
═══════════════════════════════════════════════════════════

📊 ESTATÍSTICAS GERAIS:
   Throughput Médio: 105.5 req/s
   Latência Média: 245.8 ms
   Taxa de Sucesso Média: 98.2%

🎯 RESULTADO FINAL:
   Aprovados: 3
   Reprovados: 0

📋 DETALHES POR MICROSERVICE:
   Usuarios API: ✅ PASSOU
      Throughput: 110 req/s | Latência: 230ms | Sucesso: 99%
   Catalogo API: ✅ PASSOU
      Throughput: 125 req/s | Latência: 210ms | Sucesso: 98.5%
   Vendas API: ✅ PASSOU
      Throughput: 82 req/s | Latência: 297ms | Sucesso: 97.1%

📊 BASELINE PARA HPA:
   Com base nos resultados, cada container pode processar:
   • Usuarios API: ~77 req/s
      (70% de 110 req/s para manter margem de segurança)
   • Catalogo API: ~87 req/s
      (70% de 125 req/s para manter margem de segurança)
   • Vendas API: ~57 req/s
      (70% de 82 req/s para manter margem de segurança)
```

### Análise dos Resultados

#### ✅ Resultados Saudáveis

- Taxa de sucesso > 98%
- Latência P95 < 1000ms
- Throughput consistente
- Sem erros

#### ⚠️ Atenção Necessária

- Taxa de sucesso entre 95-98%
- Latência P95 entre 1000-3000ms
- Throughput variável
- < 5% de erros

#### ❌ Problemas Críticos

- Taxa de sucesso < 95%
- Latência P95 > 3000ms
- Throughput muito baixo
- > 5% de erros

## 🔧 Troubleshooting

### Teste Falha Imediatamente

**Causa**: Serviços não estão acessíveis
**Solução**:
```powershell
# Verifique se os containers estão rodando
docker ps

# Inicie os serviços
docker-compose up -d

# Aguarde os serviços iniciarem
Start-Sleep -Seconds 30
```

### Alta Taxa de Erro

**Causas Possíveis**:
1. Recursos insuficientes (CPU/Memory)
2. Banco de dados lento
3. RabbitMQ sobrecarregado
4. Configuração incorreta

**Solução**:
```powershell
# Verifique logs dos containers
docker logs usuarios-api --tail 100
docker logs catalogo-api --tail 100
docker logs vendas-api --tail 100

# Verifique recursos
docker stats
```

### Latência Alta

**Causas Possíveis**:
1. Cold start (primeira requisição)
2. Banco de dados não otimizado
3. Queries lentas
4. Falta de índices

**Solução**:
- Executar warm-up antes do teste
- Otimizar queries do banco
- Adicionar índices necessários
- Aumentar recursos do container

## 📝 Customização

### Adicionar Novos Endpoints

Edite `performance-config.yml`:

```yaml
endpoints:
  usuarios-api:
    - path: /api/usuarios
      method: GET
      expectedStatus: 200
      
    - path: /api/usuarios/login
      method: POST
      expectedStatus: 200
      body: '{"email":"test@test.com","password":"Test@123"}'
```

### Ajustar Thresholds

Edite `performance-config.yml`:

```yaml
performance:
  thresholds:
    minSuccessRate: 98        # Mais rigoroso
    maxAverageLatency: 1000   # Mais rigoroso
    maxP95Latency: 3000       # Mais rigoroso
```

## 📚 Referências

- [Horizontal Pod Autoscaler](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)
- [Performance Testing Best Practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/performance-testing)
- [Load Testing Guidelines](https://learn.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/)

## 🤝 Contribuindo

Para adicionar novos testes ou melhorar os existentes:

1. Crie uma branch: `git checkout -b feature/new-performance-test`
2. Adicione/modifique scripts em `scripts/`
3. Atualize este README
4. Teste localmente
5. Abra um Pull Request

## 📞 Suporte

Problemas ou dúvidas? Abra uma issue no repositório.

---

**Última atualização**: 07/01/2026  
**Versão**: 1.0.0
