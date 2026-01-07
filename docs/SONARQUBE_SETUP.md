# 📊 SONARQUBE - GUIA DE INSTALAÇÃO E CONFIGURAÇÃO

**Projeto:** The Throne of Games  
**Componente:** SonarQube para Análise de Qualidade de Código  
**Data:** 07/01/2026

---

## 📋 VISÃO GERAL

SonarQube é uma plataforma open-source para análise contínua de qualidade de código. Detecta:
- Bugs e vulnerabilidades
- Code smells
- Duplicação de código
- Cobertura de testes
- Complexidade ciclomática
- Débito técnico

---

## 🚀 INSTALAÇÃO LOCAL (Docker Compose)

### Pré-requisitos

- Docker Desktop instalado e rodando
- Mínimo 4GB RAM disponível
- Mínimo 10GB espaço em disco

### Deploy Rápido

```powershell
# 1. Subir SonarQube e PostgreSQL
docker-compose -f docker-compose.sonarqube.yml up -d

# 2. Aguardar inicialização (2-3 minutos)
docker-compose -f docker-compose.sonarqube.yml logs -f sonarqube

# 3. Acessar interface web
Start-Process http://localhost:9000
```

**Credenciais padrão:**
- Username: `admin`
- Password: `admin` (será solicitado alteração no primeiro login)

### Verificar Status

```powershell
# Ver logs
docker-compose -f docker-compose.sonarqube.yml logs -f

# Ver status dos containers
docker-compose -f docker-compose.sonarqube.yml ps

# Health check
curl http://localhost:9000/api/system/status
```

### Parar e Remover

```powershell
# Parar containers
docker-compose -f docker-compose.sonarqube.yml stop

# Remover containers (mantém dados)
docker-compose -f docker-compose.sonarqube.yml down

# Remover tudo incluindo volumes
docker-compose -f docker-compose.sonarqube.yml down -v
```

---

## ☸️ INSTALAÇÃO EM KUBERNETES

### Arquivos Criados

Todos em [k8s/sonarqube/](../k8s/sonarqube/):

1. **secrets.yaml** - Credenciais PostgreSQL e SonarQube
2. **postgres.yaml** - StatefulSet PostgreSQL + Service
3. **sonarqube.yaml** - Deployment SonarQube + PVCs + Service

### Deploy em Kubernetes

```powershell
# 1. Aplicar secrets
kubectl apply -f k8s/sonarqube/secrets.yaml

# 2. Deploy PostgreSQL
kubectl apply -f k8s/sonarqube/postgres.yaml

# 3. Aguardar PostgreSQL ficar pronto
kubectl wait --for=condition=ready pod -l app=sonarqube-postgres -n thethroneofgames-monitoring --timeout=180s

# 4. Deploy SonarQube
kubectl apply -f k8s/sonarqube/sonarqube.yaml

# 5. Aguardar SonarQube ficar pronto (pode demorar 3-5 minutos)
kubectl wait --for=condition=ready pod -l app=sonarqube -n thethroneofgames-monitoring --timeout=300s

# 6. Acessar via port-forward
kubectl port-forward svc/sonarqube 9000:9000 -n thethroneofgames-monitoring

# Abrir navegador: http://localhost:9000
```

### Verificar Status em K8s

```powershell
# Ver pods
kubectl get pods -n thethroneofgames-monitoring -l app=sonarqube

# Ver logs do SonarQube
kubectl logs -f deployment/sonarqube -n thethroneofgames-monitoring

# Ver logs do PostgreSQL
kubectl logs -f statefulset/sonarqube-postgres -n thethroneofgames-monitoring

# Ver PVCs
kubectl get pvc -n thethroneofgames-monitoring

# Descrever deployment
kubectl describe deployment sonarqube -n thethroneofgames-monitoring
```

---

## 🔧 CONFIGURAÇÃO INICIAL

### 1. Primeiro Acesso

1. Acesse http://localhost:9000
2. Login com `admin/admin`
3. Altere a senha (ex: `Admin@2026!`)
4. Skip tutorial ou faça o tour guiado

### 2. Criar Token de Autenticação

1. Vá em **Administration → Security → Users**
2. Clique no menu do usuário `admin`
3. Clique em **Tokens**
4. Gere um novo token:
   - Name: `TheThroneOfGames-CI`
   - Type: `Project Analysis Token` ou `Global Analysis Token`
   - Expires in: `90 days` ou `No expiration`
5. **COPIE O TOKEN** (não será mostrado novamente)
6. Exemplo: `sqp_abc123def456ghi789jkl012mno345pqr`

### 3. Criar Projeto

#### Opção A: Manual

1. Clique em **Create Project**
2. Escolha **Manually**
3. Preencha:
   - Project key: `thethroneofgames`
   - Display name: `The Throne of Games`
4. Clique **Set Up**
5. Escolha **Locally**
6. Escolha **Use existing token** ou crie um novo
7. Escolha **Other** (for CI/CD) → **.NET**
8. Siga as instruções exibidas

#### Opção B: Via GitHub Actions (recomendado)

Será configurado automaticamente no CI/CD pipeline.

---

## 🔗 INTEGRAÇÃO COM CI/CD

### 1. Adicionar Secrets no GitHub

Vá em **Settings → Secrets and variables → Actions** e adicione:

```
SONAR_HOST_URL=http://sonarqube:9000
SONAR_TOKEN=sqp_abc123def456ghi789jkl012mno345pqr
```

**Atenção:** Se SonarQube estiver em K8s, use o LoadBalancer IP ou configure Ingress.

### 2. Configuração no CI/CD (já implementada)

O arquivo [.github/workflows/ci-cd.yml](../.github/workflows/ci-cd.yml) já possui o job `code-quality`:

```yaml
code-quality:
  runs-on: ubuntu-latest
  needs: build-and-test
  steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Shallow clones disabled for better analysis
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x
    
    - name: Setup SonarQube Scanner
      run: |
        dotnet tool install --global dotnet-sonarscanner
        echo "$HOME/.dotnet/tools" >> $GITHUB_PATH
    
    - name: Begin SonarQube Analysis
      run: |
        dotnet sonarscanner begin \
          /k:"thethroneofgames" \
          /d:sonar.host.url="${{ secrets.SONAR_HOST_URL }}" \
          /d:sonar.token="${{ secrets.SONAR_TOKEN }}" \
          /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Tests with Coverage
      run: |
        dotnet test --no-build --verbosity normal \
          /p:CollectCoverage=true \
          /p:CoverletOutputFormat=opencover
    
    - name: End SonarQube Analysis
      run: dotnet sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN }}"
```

### 3. Habilitar o Job

Edite [.github/workflows/ci-cd.yml](../.github/workflows/ci-cd.yml) e descomente o job `code-quality`:

```yaml
# Descomente estas linhas:
  code-quality:
    runs-on: ubuntu-latest
    needs: build-and-test
    # ... resto do job
```

---

## 📊 MÉTRICAS E QUALITY GATES

### Quality Gates Padrão

O SonarQube vem com um Quality Gate padrão "Sonar way" que exige:

- **Coverage:** ≥ 80% de cobertura de testes
- **Duplications:** ≤ 3% de código duplicado
- **Maintainability Rating:** A (sem code smells críticos)
- **Reliability Rating:** A (sem bugs críticos)
- **Security Rating:** A (sem vulnerabilidades críticas)
- **Security Review Rating:** A (sem hotspots de segurança não revisados)

### Customizar Quality Gate

1. Vá em **Quality Gates**
2. Crie um novo ou edite o padrão
3. Adicione/edite condições:
   ```
   Coverage on New Code ≥ 80%
   Duplicated Lines (%) on New Code ≤ 3%
   Bugs ≤ 0
   Vulnerabilities ≤ 0
   Security Hotspots Reviewed ≥ 100%
   Code Smells ≤ 10
   ```

### Aplicar ao Projeto

1. Vá em **Project Settings → Quality Gate**
2. Selecione o Quality Gate desejado
3. Salve

---

## 📈 DASHBOARDS E RELATÓRIOS

### Métricas Principais

1. **Overview Tab:**
   - Bugs, Vulnerabilities, Code Smells
   - Coverage, Duplications
   - Quality Gate Status

2. **Issues Tab:**
   - Todos os issues encontrados
   - Filtrar por tipo, severidade, assignee

3. **Measures Tab:**
   - Métricas detalhadas
   - Histórico de métricas

4. **Code Tab:**
   - Navegação pelo código
   - Hotspots de segurança

5. **Activity Tab:**
   - Histórico de análises
   - Evolução de métricas

### Exportar Relatórios

1. Vá em **More → Export**
2. Escolha formato (PDF, CSV)
3. Download

---

## 🔐 SEGURANÇA

### Rotação de Senhas

```sql
-- Se necessário acessar PostgreSQL diretamente
docker exec -it sonarqube-postgres psql -U sonar -d sonarqube

-- Alterar senha do SonarQube via interface web em:
-- Administration → Security → Users → admin → Change password
```

### Backup

```powershell
# Backup do banco de dados PostgreSQL
docker exec sonarqube-postgres pg_dump -U sonar sonarqube > sonarqube-backup-$(Get-Date -Format "yyyyMMdd").sql

# Backup dos dados do SonarQube
docker run --rm -v sonarqube-data:/data -v ${PWD}:/backup alpine tar czf /backup/sonarqube-data-backup-$(Get-Date -Format "yyyyMMdd").tar.gz -C /data .
```

### Restore

```powershell
# Restore banco de dados
cat sonarqube-backup-20260107.sql | docker exec -i sonarqube-postgres psql -U sonar -d sonarqube

# Restore dados
docker run --rm -v sonarqube-data:/data -v ${PWD}:/backup alpine tar xzf /backup/sonarqube-data-backup-20260107.tar.gz -C /data
```

---

## 🛠️ TROUBLESHOOTING

### Problema: SonarQube não inicia

**Sintoma:** Container reinicia continuamente

**Solução:**
```powershell
# Verificar logs
docker logs sonarqube

# Erros comuns:
# 1. vm.max_map_count too low
#    Windows: Aumentar memória WSL2
#    wsl -d docker-desktop sysctl -w vm.max_map_count=524288

# 2. Memória insuficiente
#    Aumentar memória do Docker Desktop para 4GB+

# 3. PostgreSQL não pronto
#    Aguardar PostgreSQL ficar healthy antes
docker-compose -f docker-compose.sonarqube.yml up -d sonarqube-postgres
docker-compose -f docker-compose.sonarqube.yml logs -f sonarqube-postgres
# Aguardar "database system is ready to accept connections"
docker-compose -f docker-compose.sonarqube.yml up -d sonarqube
```

### Problema: Análise falha no CI/CD

**Sintoma:** Job `code-quality` falha

**Soluções:**
```yaml
# 1. Verificar secrets configurados
#    SONAR_HOST_URL e SONAR_TOKEN devem estar corretos

# 2. Verificar conectividade
#    SonarQube deve ser acessível do GitHub Actions runner
#    Use LoadBalancer ou Ingress público

# 3. Verificar token
#    Token pode ter expirado ou ter permissões insuficientes
#    Gere novo token com permissões de análise

# 4. Verificar projeto existe
#    Projeto deve ser criado manualmente antes ou criar automaticamente:
dotnet sonarscanner begin \
  /k:"thethroneofgames" \
  /n:"The Throne of Games" \
  /d:sonar.host.url="..." \
  /d:sonar.token="..."
```

### Problema: Coverage não aparece

**Sintoma:** Cobertura mostra 0% ou N/A

**Solução:**
```yaml
# 1. Instalar Coverlet no projeto de testes
dotnet add Test/Test.csproj package coverlet.collector

# 2. Executar testes com coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# 3. Verificar arquivo coverage.opencover.xml foi gerado
ls **/coverage.opencover.xml

# 4. Passar path correto ao SonarQube
/d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
```

---

## 📚 RECURSOS

### Documentação Oficial
- [SonarQube Documentation](https://docs.sonarqube.org/latest/)
- [SonarScanner for .NET](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/)
- [Quality Gates](https://docs.sonarqube.org/latest/user-guide/quality-gates/)

### Plugins Recomendados
- C# Plugin (incluído na Community Edition)
- SonarLint (para IDEs - Visual Studio, VS Code)

### Community
- [SonarQube Community Forum](https://community.sonarsource.com/)
- [Stack Overflow - sonarqube tag](https://stackoverflow.com/questions/tagged/sonarqube)

---

## ✅ CHECKLIST DE CONFIGURAÇÃO

### Instalação Local
- [ ] Docker Compose up
- [ ] SonarQube acessível em http://localhost:9000
- [ ] Login admin/admin funcionando
- [ ] Senha alterada
- [ ] Token gerado e copiado
- [ ] Projeto criado

### Integração CI/CD
- [ ] SONAR_HOST_URL configurado no GitHub Secrets
- [ ] SONAR_TOKEN configurado no GitHub Secrets
- [ ] Job `code-quality` descomentado no ci-cd.yml
- [ ] Push para testar pipeline
- [ ] Análise aparece no SonarQube

### Quality Gates
- [ ] Quality Gate configurado
- [ ] Quality Gate aplicado ao projeto
- [ ] Limites de qualidade definidos
- [ ] Notificações configuradas (opcional)

---

**Última atualização:** 07/01/2026  
**Autor:** DevOps Team  
**Versão:** 1.0
