# GitHub Actions Secrets Configuration

Para que o pipeline CI/CD funcione corretamente com deploy no GKE, você precisa configurar os seguintes secrets no GitHub:

## 🔐 Secrets Necessários

### 1. `GCP_CREDENTIALS`
**Descrição**: JSON com as credenciais da conta de serviço do Google Cloud

**Como obter**:
```bash
# 1. Crie uma conta de serviço
gcloud iam service-accounts create github-actions \
    --display-name="GitHub Actions"

# 2. Conceda permissões necessárias
gcloud projects add-iam-policy-binding PROJECT_ID \
    --member="serviceAccount:github-actions@PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/container.admin"

gcloud projects add-iam-policy-binding PROJECT_ID \
    --member="serviceAccount:github-actions@PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/storage.admin"

gcloud projects add-iam-policy-binding PROJECT_ID \
    --member="serviceAccount:github-actions@PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/artifactregistry.admin"

# 3. Crie e baixe a chave
gcloud iam service-accounts keys create key.json \
    --iam-account=github-actions@PROJECT_ID.iam.gserviceaccount.com

# 4. Copie o conteúdo de key.json e adicione como secret
```

**Valor**: Todo o conteúdo do arquivo `key.json`

### 2. `GCP_PROJECT_ID`
**Descrição**: ID do projeto no Google Cloud

**Como obter**:
```bash
gcloud config get-value project
```

**Valor**: Ex: `project-62120210-43eb-4d93-954` (seu ID do projeto)

## 📝 Como Adicionar Secrets no GitHub

1. Acesse seu repositório no GitHub
2. Vá em **Settings** > **Secrets and variables** > **Actions**
3. Clique em **New repository secret**
4. Adicione cada secret:
   - Nome: `GCP_CREDENTIALS`
   - Valor: Cole o conteúdo completo do arquivo `key.json`
   
   - Nome: `GCP_PROJECT_ID`
   - Valor: Cole o ID do seu projeto (ex: `project-62120210-43eb-4d93-954`)

## 🎯 Cluster GKE

O cluster configurado no pipeline:
- **Nome**: `autopilot-cluster-1`
- **Região**: `southamerica-east1`
- **Tipo**: GKE Autopilot

### Verificar se o cluster existe:
```bash
gcloud container clusters list --region southamerica-east1
```

### Criar o cluster (se não existir):
```bash
gcloud container clusters create-auto autopilot-cluster-1 \
    --region=southamerica-east1 \
    --project=PROJECT_ID
```

### Obter credenciais do cluster:
```bash
gcloud container clusters get-credentials autopilot-cluster-1 \
    --region southamerica-east1 \
    --project PROJECT_ID
```

## 🚀 Deploy Manual (Teste)

Antes de usar o GitHub Actions, teste o deploy manualmente:

```bash
# 1. Autentique no GCP
gcloud auth login

# 2. Configure o projeto
gcloud config set project PROJECT_ID

# 3. Obtenha credenciais do cluster
gcloud container clusters get-credentials autopilot-cluster-1 \
    --region southamerica-east1

# 4. Configure Docker para GCR
gcloud auth configure-docker

# 5. Build e push das imagens
docker build -t gcr.io/PROJECT_ID/usuarios-api:latest -f GameStore.Usuarios.API/Dockerfile .
docker push gcr.io/PROJECT_ID/usuarios-api:latest

docker build -t gcr.io/PROJECT_ID/catalogo-api:latest -f GameStore.Catalogo.API/Dockerfile .
docker push gcr.io/PROJECT_ID/catalogo-api:latest

docker build -t gcr.io/PROJECT_ID/vendas-api:latest -f GameStore.Vendas.API/Dockerfile .
docker push gcr.io/PROJECT_ID/vendas-api:latest

# 6. Atualize os manifestos k8s com suas imagens
sed -i "s|image:.*usuarios-api.*|image: gcr.io/PROJECT_ID/usuarios-api:latest|g" k8s/usuarios-api-deployment.yaml
sed -i "s|image:.*catalogo-api.*|image: gcr.io/PROJECT_ID/catalogo-api:latest|g" k8s/catalogo-api-deployment.yaml
sed -i "s|image:.*vendas-api.*|image: gcr.io/PROJECT_ID/vendas-api:latest|g" k8s/vendas-api-deployment.yaml

# 7. Deploy no cluster
kubectl apply -f k8s/

# 8. Verifique o status
kubectl get pods
kubectl get services
kubectl get hpa
kubectl get ingress
```

## 🔍 Validação do Deploy

Após o deploy, verifique:

```bash
# Status dos pods
kubectl get pods -o wide

# Logs de um pod específico
kubectl logs -f POD_NAME

# Describe de um deployment
kubectl describe deployment usuarios-api

# Status do HPA
kubectl get hpa

# Ingress e IP externo
kubectl get ingress
```

## 🛑 Rollback (se necessário)

```bash
# Ver histórico de deployments
kubectl rollout history deployment/usuarios-api

# Fazer rollback para versão anterior
kubectl rollout undo deployment/usuarios-api

# Rollback para revisão específica
kubectl rollout undo deployment/usuarios-api --to-revision=2
```

## 📊 Monitoramento

```bash
# Ver métricas dos pods
kubectl top pods

# Ver métricas dos nodes
kubectl top nodes

# Ver eventos
kubectl get events --sort-by='.lastTimestamp'
```

## 🧹 Limpeza (Economia de Custos)

Após a demonstração, limpe os recursos:

```bash
# Deletar todos os recursos do namespace
kubectl delete -f k8s/

# Ou deletar o cluster inteiro
gcloud container clusters delete autopilot-cluster-1 \
    --region southamerica-east1 \
    --quiet
```

## 📝 Checklist de Configuração

- [ ] Conta de serviço criada no GCP
- [ ] Permissões concedidas à conta de serviço
- [ ] Chave JSON criada e baixada
- [ ] Secret `GCP_CREDENTIALS` adicionado no GitHub
- [ ] Secret `GCP_PROJECT_ID` adicionado no GitHub
- [ ] Cluster GKE criado e funcionando
- [ ] Docker configurado para GCR
- [ ] Manifestos Kubernetes testados localmente
- [ ] Deploy manual testado com sucesso
- [ ] Pipeline GitHub Actions testado

## 🎬 Fluxo do Pipeline

1. **Push/PR** → Trigger do workflow
2. **Build & Test** → Compila código e roda testes unitários
3. **Docker Build** → Cria imagens Docker otimizadas
4. **Performance Tests** → Valida throughput e latência
5. **Security Scan** → Scanner de vulnerabilidades (Trivy)
6. **Deploy GKE** → Deploy automático no GKE (apenas master/main)
7. **Summary** → Relatório consolidado

## 🔗 Links Úteis

- [GKE Documentation](https://cloud.google.com/kubernetes-engine/docs)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [Helm Documentation](https://helm.sh/docs/)

---

**Última atualização**: 07/01/2026  
**Versão**: 1.0.0
