# Deploy no Amazon EKS — Fase 4

Guia para provisionar o cluster (passo manual, deliberado) e configurar o pipeline
(`.github/workflows/deploy-eks.yml`) que builda as 4 imagens, publica no ECR e implanta em
`k8s/` automaticamente a cada push em `release/fase-4-kubernetes` (ou via disparo manual).

> **Por que a criação do cluster não é automática?** Um cluster EKS custa (~US$0.10/hora só
> o control plane, mais os nós) e leva ~15-20min para subir. Automatizar isso para rodar em
> todo push seria caro e arriscado. A criação é um passo manual único, documentado abaixo —
> o pipeline só builda/publica imagens e faz `kubectl apply` contra um cluster que já existe.

## 1. Criar o cluster (uma vez, manualmente)

Requer [eksctl](https://eksctl.io/) e [AWS CLI](https://docs.aws.amazon.com/cli/) configurados
localmente com credenciais de administrador.

```sh
eksctl create cluster \
  --name gamestore-cluster \
  --region sa-east-1 \
  --nodegroup-name gamestore-nodes \
  --node-type t3.medium \
  --nodes 2 \
  --nodes-min 2 \
  --nodes-max 4 \
  --managed

# metrics-server é exigido pelos HPA em k8s/hpa/ — instalar se o cluster ainda não tiver:
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Ingress Controller (ingress-nginx) para k8s/ingress/ingress.yaml:
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/aws/deploy.yaml
```

Ao final da gravação do vídeo de demonstração, **destrua o cluster** para não gerar custo
(orientação explícita do edital — ver `docs/Objectives/TC NETT - Fase 4 (1).md`):

```sh
eksctl delete cluster --name gamestore-cluster --region sa-east-1
```

## 2. Criar o usuário/chave IAM usado pelo pipeline

O usuário IAM cujas chaves vão para os Secrets do GitHub precisa de permissão para:
publicar imagens no ECR, descrever/autenticar no cluster EKS, e (crucial) estar mapeado
nas **Access Entries** do cluster (ou no `aws-auth` ConfigMap, em clusters mais antigos) com
permissão para `kubectl apply` no namespace `gamestore`.

```sh
# Política mínima sugerida (ajuste o ARN do cluster/conta):
# - AmazonEC2ContainerRegistryFullAccess (ECR)
# - AmazonEKSClusterPolicy + eks:DescribeCluster (autenticação do kubectl)

# Mapear o usuário IAM no cluster (EKS Access Entries, clusters 1.23+):
aws eks create-access-entry \
  --cluster-name gamestore-cluster \
  --region sa-east-1 \
  --principal-arn arn:aws:iam::<ACCOUNT_ID>:user/<NOME_DO_USUARIO_DO_PIPELINE>

aws eks associate-access-policy \
  --cluster-name gamestore-cluster \
  --region sa-east-1 \
  --principal-arn arn:aws:iam::<ACCOUNT_ID>:user/<NOME_DO_USUARIO_DO_PIPELINE> \
  --policy-arn arn:aws:eks::aws:cluster-access-policy/AmazonEKSClusterAdminPolicy \
  --access-scope type=cluster
```

## 3. Configurar os Secrets/Variables no GitHub

Repositório → **Settings → Secrets and variables → Actions**:

| Tipo | Nome | Valor |
|---|---|---|
| Secret | `AWS_ACCESS_KEY_ID` | Access key do usuário IAM do passo 2 |
| Secret | `AWS_SECRET_ACCESS_KEY` | Secret key correspondente |
| Variable *(opcional)* | `AWS_REGION` | Default: `sa-east-1` |
| Variable *(opcional)* | `EKS_CLUSTER_NAME` | Default: `gamestore-cluster` |

## 4. Rodar o pipeline

- **Automático**: qualquer push em `release/fase-4-kubernetes` dispara `deploy-eks.yml`.
- **Manual**: aba *Actions* → `Deploy to AWS EKS (Fase 4)` → *Run workflow*. Marque
  `run_load_test` para, ao final do deploy, rodar `k8s/load-test/load-test-job.yaml` e
  validar o HPA escalando no cluster real (mesma validação já feita localmente em kind —
  ver `docs/k8s-architecture-flow.md` §7).

O pipeline builda as 4 imagens (3 APIs + Function), publica em repositórios ECR
`gamestore/<serviço>` (criados automaticamente se não existirem), aplica `k8s/` inteiro, e
substitui a imagem de cada Deployment pela recém-publicada (`kubectl set image`, tag = SHA
do commit) — os manifests em `k8s/` continuam com a imagem placeholder `gamestore/<serviço>:latest`
e funcionam tanto para EKS quanto para um cluster local (kind), sem hardcodar o registry.

## 5. Acessar a aplicação

```sh
aws eks update-kubeconfig --name gamestore-cluster --region sa-east-1
kubectl get ingress -n gamestore   # ADDRESS = DNS do ELB provisionado pelo ingress-nginx
```

Use o `ADDRESS` do Ingress (ou configure um domínio real apontando para ele via Route 53)
como a URL base para a gravação do vídeo de demonstração e para `tools/record-delivery.js`
(via as variáveis de ambiente `BASE_URL_*`, ver o cabeçalho do script).

## 6. Teardown

```sh
# Remove só a aplicação, mantém o cluster:
kubectl delete namespace gamestore

# Remove o cluster inteiro (fazer isso após gravar o vídeo — ver aviso no topo deste doc):
eksctl delete cluster --name gamestore-cluster --region sa-east-1
```
