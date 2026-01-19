# CI/CD Setup Guide - GitHub Actions com GCP

**Data:** 16 de Janeiro de 2026  
**Método:** Service Account Key (Opção B)

## 📋 Pré-requisitos

- Acesso ao projeto GCP: `project-62120210-43eb-4d93-954`
- Permissões de Owner ou Editor no projeto
- Acesso ao repositório GitHub com permissões de admin
- gcloud CLI instalado e autenticado

## 🔑 Passo 1: Criar Service Account no GCP

### 1.1 Criar Service Account

```bash
# Definir variáveis
export PROJECT_ID="project-62120210-43eb-4d93-954"
export SA_NAME="github-actions-deployer"
export SA_DISPLAY_NAME="GitHub Actions Deployer"

# PowerShell
$PROJECT_ID = "project-62120210-43eb-4d93-954"
$SA_NAME = "github-actions-deployer"
$SA_DISPLAY_NAME = "GitHub Actions Deployer"

# Criar Service Account
gcloud iam service-accounts create $SA_NAME `
  --display-name="$SA_DISPLAY_NAME" `
  --project=$PROJECT_ID
```

### 1.2 Conceder Permissões

O Service Account precisa de:
- **Kubernetes Engine Admin**: Gerenciar deployments
- **Storage Admin**: Push de imagens Docker para GCR
- **Service Account User**: Executar operações

```bash
# PowerShell
$SA_EMAIL = "$SA_NAME@$PROJECT_ID.iam.gserviceaccount.com"

# Kubernetes Engine Admin
gcloud projects add-iam-policy-binding $PROJECT_ID `
  --member="serviceAccount:$SA_EMAIL" `
  --role="roles/container.admin"

# Storage Admin (para GCR)
gcloud projects add-iam-policy-binding $PROJECT_ID `
  --member="serviceAccount:$SA_EMAIL" `
  --role="roles/storage.admin"

# Service Account User
gcloud projects add-iam-policy-binding $PROJECT_ID `
  --member="serviceAccount:$SA_EMAIL" `
  --role="roles/iam.serviceAccountUser"

# Viewer (para listar recursos)
gcloud projects add-iam-policy-binding $PROJECT_ID `
  --member="serviceAccount:$SA_EMAIL" `
  --role="roles/viewer"
```

### 1.3 Criar e Baixar Key

```bash
# PowerShell
$KEY_FILE = "gcp-github-actions-key.json"

gcloud iam service-accounts keys create $KEY_FILE `
  --iam-account=$SA_EMAIL `
  --project=$PROJECT_ID

Write-Host "`n✅ Key criada: $KEY_FILE" -ForegroundColor Green
Write-Host "⚠️  IMPORTANTE: Mantenha este arquivo seguro!" -ForegroundColor Yellow
```

**⚠️ SEGURANÇA:**
- Este arquivo dá acesso total ao projeto
- Nunca commite no git
- Delete após configurar no GitHub
- Guarde backup em local seguro (password manager)

## 🔐 Passo 2: Configurar GitHub Secrets

### 2.1 Converter Key para Base64

```powershell
# PowerShell
$KEY_CONTENT = Get-Content -Path "gcp-github-actions-key.json" -Raw
$KEY_BASE64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($KEY_CONTENT))

# Copiar para clipboard
$KEY_BASE64 | Set-Clipboard
Write-Host "`n✅ Key Base64 copiada para clipboard!" -ForegroundColor Green
```

### 2.2 Adicionar Secrets no GitHub

1. Acesse: https://github.com/guilhermesoatto/TheThroneOfGames/settings/secrets/actions

2. Clique em **"New repository secret"**

3. Adicione os seguintes secrets:

#### Secret 1: GCP_SA_KEY
- **Name:** `GCP_SA_KEY`
- **Value:** Cole o conteúdo Base64 da clipboard (Ctrl+V)

#### Secret 2: GCP_PROJECT_ID
- **Name:** `GCP_PROJECT_ID`
- **Value:** `project-62120210-43eb-4d93-954`

#### Secret 3: GKE_CLUSTER_NAME
- **Name:** `GKE_CLUSTER_NAME`
- **Value:** `autopilot-cluster-1`

#### Secret 4: GKE_ZONE
- **Name:** `GKE_ZONE`
- **Value:** `southamerica-east1`

#### Secret 5: DOCKER_REGISTRY (opcional)
- **Name:** `DOCKER_REGISTRY`
- **Value:** `gcr.io/project-62120210-43eb-4d93-954`

### 2.3 Verificar Secrets Configurados

Após adicionar, você deve ver:
- ✅ GCP_SA_KEY
- ✅ GCP_PROJECT_ID
- ✅ GKE_CLUSTER_NAME
- ✅ GKE_ZONE
- ✅ DOCKER_REGISTRY

## 🚀 Passo 3: Atualizar GitHub Actions Workflow

O workflow `.github/workflows/ci-cd.yml` precisa ser atualizado para usar os secrets.

**Job de Deploy deve ter:**

```yaml
- name: Authenticate to Google Cloud
  uses: google-github-actions/auth@v1
  with:
    credentials_json: ${{ secrets.GCP_SA_KEY }}

- name: Set up Cloud SDK
  uses: google-github-actions/setup-gcloud@v1

- name: Configure Docker for GCR
  run: gcloud auth configure-docker gcr.io

- name: Get GKE credentials
  run: |
    gcloud container clusters get-credentials ${{ secrets.GKE_CLUSTER_NAME }} \
      --region ${{ secrets.GKE_ZONE }} \
      --project ${{ secrets.GCP_PROJECT_ID }}
```

## 🧪 Passo 4: Testar Localmente

Antes de commitar, teste a autenticação localmente:

```powershell
# Ativar Service Account localmente
gcloud auth activate-service-account --key-file=gcp-github-actions-key.json

# Testar acesso ao cluster
gcloud container clusters get-credentials autopilot-cluster-1 `
  --region southamerica-east1 `
  --project project-62120210-43eb-4d93-954

# Testar kubectl
kubectl get pods -n thethroneofgames

# Voltar para sua conta pessoal
gcloud config set account seu-email@gmail.com
```

## ✅ Passo 5: Validar Pipeline

### 5.1 Criar Branch de Teste

```bash
git checkout -b test/ci-cd-pipeline
git push origin test/ci-cd-pipeline
```

### 5.2 Abrir Pull Request

1. Vá para: https://github.com/guilhermesoatto/TheThroneOfGames/pulls
2. Crie PR de `test/ci-cd-pipeline` para `master`
3. Observe os checks do GitHub Actions

### 5.3 Verificar Logs

- Acesse: https://github.com/guilhermesoatto/TheThroneOfGames/actions
- Clique no workflow em execução
- Verifique cada job:
  - ✅ Build & Test
  - ✅ Docker Build
  - ✅ Security Scan
  - ✅ Deploy

## 🔒 Passo 6: Segurança Pós-Setup

### 6.1 Deletar Key Local

```powershell
# Após configurar no GitHub, delete o arquivo local
Remove-Item "gcp-github-actions-key.json" -Force
Write-Host "✅ Key local deletada" -ForegroundColor Green
```

### 6.2 Rotação de Keys (Recomendado a cada 90 dias)

```bash
# Listar keys existentes
gcloud iam service-accounts keys list `
  --iam-account=github-actions-deployer@project-62120210-43eb-4d93-954.iam.gserviceaccount.com

# Criar nova key
gcloud iam service-accounts keys create new-key.json `
  --iam-account=github-actions-deployer@project-62120210-43eb-4d93-954.iam.gserviceaccount.com

# Atualizar secret no GitHub

# Deletar key antiga
gcloud iam service-accounts keys delete KEY_ID `
  --iam-account=github-actions-deployer@project-62120210-43eb-4d93-954.iam.gserviceaccount.com
```

## 🎯 Passo 7: Estrutura do Pipeline

### Fluxo Completo

```
┌─────────────────────────────────────────────────────────────┐
│                    GitHub Push/PR                           │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 1: Build & Test                                        │
│  - Restore dependencies                                     │
│  - Build solution                                           │
│  - Run unit tests                                           │
│  - Upload test results                                      │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 2: Docker Build (se master)                           │
│  - Build 3 imagens (usuarios, catalogo, vendas)            │
│  - Tag com SHA do commit                                    │
│  - Push para GCR                                            │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 3: Security Scan                                       │
│  - Trivy vulnerability scan                                 │
│  - Upload SARIF to GitHub Security                          │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│  Job 4: Deploy to GKE (se master)                          │
│  - Authenticate to GCP                                      │
│  - Get GKE credentials                                      │
│  - Apply Kubernetes manifests                               │
│  - Verify deployment rollout                                │
│  - Run smoke tests                                          │
└─────────────────────────────────────────────────────────────┘
```

## 📊 Monitoramento do Pipeline

### GitHub Actions Dashboard

```
https://github.com/guilhermesoatto/TheThroneOfGames/actions
```

### Verificar Deployment no GKE

```bash
# Ver rollout status
kubectl rollout status deployment/usuarios-api -n thethroneofgames
kubectl rollout status deployment/catalogo-api -n thethroneofgames
kubectl rollout status deployment/vendas-api -n thethroneofgames

# Ver história de rollouts
kubectl rollout history deployment/usuarios-api -n thethroneofgames

# Rollback se necessário
kubectl rollout undo deployment/usuarios-api -n thethroneofgames
```

## 🐛 Troubleshooting

### Erro: "Permission denied" durante deploy

**Causa:** Service Account sem permissões adequadas

**Solução:**
```bash
# Verificar roles atuais
gcloud projects get-iam-policy project-62120210-43eb-4d93-954 \
  --flatten="bindings[].members" \
  --filter="bindings.members:github-actions-deployer@*"

# Adicionar role faltante
gcloud projects add-iam-policy-binding project-62120210-43eb-4d93-954 \
  --member="serviceAccount:github-actions-deployer@project-62120210-43eb-4d93-954.iam.gserviceaccount.com" \
  --role="roles/ROLE_NAME"
```

### Erro: "Invalid credentials" no Docker push

**Causa:** GCR não autenticado

**Solução no workflow:**
```yaml
- name: Configure Docker
  run: gcloud auth configure-docker gcr.io
```

### Erro: "Cluster not found"

**Causa:** Nome ou região incorretos

**Solução:**
```bash
# Listar clusters disponíveis
gcloud container clusters list --project=project-62120210-43eb-4d93-954

# Atualizar secrets no GitHub com valores corretos
```

## 📚 Recursos Adicionais

- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [Google Cloud IAM Best Practices](https://cloud.google.com/iam/docs/best-practices)
- [GKE Authentication](https://cloud.google.com/kubernetes-engine/docs/how-to/api-server-authentication)
- [GitHub Security](https://docs.github.com/en/code-security)

## ✅ Checklist Final

- [ ] Service Account criado no GCP
- [ ] Permissões configuradas (container.admin, storage.admin)
- [ ] Key JSON gerada e baixada
- [ ] Secrets configurados no GitHub (GCP_SA_KEY, etc)
- [ ] Workflow CI/CD atualizado
- [ ] Pipeline testado com PR de teste
- [ ] Key local deletada (segurança)
- [ ] Documentação do processo salva

---

**Status:** Pronto para CI/CD automatizado! 🚀  
**Última atualização:** 16 de Janeiro de 2026
