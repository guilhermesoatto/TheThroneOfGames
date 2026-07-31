# Deploy no Oracle Cloud (OKE) — Fase 4 (plano B)

Guia alternativo ao `docs/eks-deploy.md`. Criado em 2026-07-31 porque a conta AWS usada para
este projeto ficou presa numa restrição de elegibilidade de tipo de instância de conta nova
(erro `InvalidParameterCombination: not eligible for Free Tier` ao tentar `t3.medium`,
confirmado pelo AWS Support como não relacionado a região nem a saldo — ver histórico do
chamado) e a resolução está pendente do lado da AWS. O edital permite qualquer cloud
("à sua escolha"), então este é o plano B pronto para usar se a AWS não destravar a tempo.

> **Por que Oracle Cloud e não outra alternativa?** O Always Free da Oracle inclui um cluster
> OKE básico gratuito (control plane sem custo) e VMs ARM Ampere (`VM.Standard.A1.Flex`) sem a
> mesma fricção de "conta nova" que travou a AWS — não depende de aprovação alguma para usar
> além do free tier. Vale notar: a Oracle **reduziu silenciosamente** essa cota pela metade em
> 15/06/2026 (de 4 OCPU/24GB para **2 OCPU/12GB total**, sem aviso público) — ainda assim,
> 12GB é ~3x o que esta stack pede no mínimo (~4,1GB, ver cálculo abaixo), então continua
> viável.

## 0. Compatibilidade ARM64 — leia antes de tudo

`VM.Standard.A1.Flex` é **ARM64 (Ampere)**, não x86_64. As imagens .NET das 4 aplicações
precisam ser buildadas para `linux/arm64` (ou multi-arch) — uma imagem x86_64 simplesmente não
roda ali ("exec format error"). O pipeline (`deploy-oke.yml`, abaixo) já builda com
`platforms: linux/arm64` via Buildx. As imagens de infraestrutura usadas em `k8s/deployments/`
(postgres, rabbitmq, elasticsearch, mongodb, jaeger, prometheus, grafana, nginx) já publicam
manifests multi-arch incluindo arm64 nas tags usadas — **exceto possivelmente o Azurite**
(`mcr.microsoft.com/azure-storage/azurite`), que vale checar (`docker manifest inspect
mcr.microsoft.com/azure-storage/azurite:latest`) antes de assumir que funciona; se não tiver
arm64, a Function de notificações que depende dele fica sem Azurite (afeta só o Storage
emulado do Functions, não os outros 3 microsserviços).

## 1. Dimensionamento — por que 2 nós de 1 OCPU/6GB

Somando os `requests` reais de `k8s/deployments/` (Postgres, RabbitMQ, Elasticsearch, Jaeger,
Prometheus, Grafana, API Gateway ×2, Notifications, kube-state-metrics + as 3 APIs com
`minReplicas: 2` do HPA cada), o mínimo é **~4.100Mi (~4,1GB)** de memória — ver a conta
completa na conversa que originou este documento. 2 nós × 6GB = 12GB no total, dá folga
confortável (Elasticsearch sozinho, o maior pod individual, pede só 768Mi — cabe em qualquer
um dos dois nós sem problema). Também bate com o total Always Free (2 OCPU + 12GB).

## 2. Criar a conta OCI (manual, só você pode fazer)

1. Cadastro em https://signup.oraclecloud.com — pede verificação de identidade e cartão (não
   cobra nada em recursos Always Free, é só verificação anti-abuso, igual a fricção que já
   tentamos evitar na AWS, mas aqui não bloqueia o tipo de instância depois).
2. Anote, no console (Profile → Tenancy/User Settings):
   - **Tenancy OCID** (`ocid1.tenancy.oc1..…`)
   - **User OCID** (`ocid1.user.oc1..…`)
   - **Region** (ex.: `sa-saopaulo-1`, se disponível pra sua conta — senão `us-ashburn-1`)
3. Gerar uma API Signing Key (Profile → My Profile → API Keys → Add API Key → "Generate API
   Key Pair") — baixe a chave privada, anote o **Fingerprint** mostrado.
4. Instalar OCI CLI localmente: `winget install Oracle.OCICLI` (ou
   `pip install oci-cli` como alternativa) e rodar `oci setup config`, colando os valores do
   passo 2/3 quando pedido — isso cria `~/.oci/config`, usado pelos comandos abaixo.

## 3. Criar o cluster (uma vez, manualmente — via Console, não CLI)

Diferente do `eksctl create cluster` (que cria VPC+subnets+cluster num comando só), o
`oci ce cluster create` via CLI exige VCN/subnets/gateways já existentes — criar tudo isso às
cegas por CLI é mais arriscado que útil pra um setup único. A própria Oracle recomenda o
assistente do Console pra isso:

1. Console OCI → **Developer Services → Kubernetes Clusters (OKE)** → **Create Cluster** →
   **Quick Create** (não "Custom Create" — o Quick Create cria a VCN, subnets, gateways e o
   cluster automaticamente, com boas práticas padrão).
2. Nome do cluster: `gamestore-cluster` (mesmo nome usado no plano AWS, por consistência).
3. Kubernetes version: a mais recente estável oferecida.
4. Visibility: **Public Endpoint** (equivalente ao `publicAccess=true` do EKS).
5. Node type: **Managed**. Shape: **VM.Standard.A1.Flex**, **1 OCPU / 6GB** por nó, **2 nós**
   (ver §1 acima).
6. Confirme e crie — leva alguns minutos (bem mais rápido que o control plane do EKS).

Ao final da gravação, **destrua o cluster** (mesma exigência do edital que já vale para
qualquer cloud): Console → o cluster → **Delete**, ou:

```sh
oci ce cluster delete --cluster-id <OCID_DO_CLUSTER> --force
```

## 4. Criar o Auth Token para o OCIR (usado pelo pipeline)

OCIR (registry de imagens da Oracle, equivalente ao ECR) não usa a API Signing Key do passo 2
— usa um **Auth Token** separado:

Console → Profile → **My Profile → Auth Tokens → Generate Token** — copie o valor na hora (só
é mostrado uma vez).

## 5. Configurar os Secrets no GitHub

Repositório → **Settings → Secrets and variables → Actions**:

| Tipo | Nome | Valor |
|---|---|---|
| Secret | `OCI_CLI_USER` | User OCID (passo 2) |
| Secret | `OCI_CLI_TENANCY` | Tenancy OCID (passo 2) |
| Secret | `OCI_CLI_FINGERPRINT` | Fingerprint da API key (passo 3) |
| Secret | `OCI_CLI_KEY_CONTENT` | Conteúdo da chave privada `.pem` (passo 3), inteiro |
| Secret | `OCI_CLI_REGION` | Region (ex.: `sa-saopaulo-1`) |
| Secret | `OCIR_USERNAME` | `<tenancy-namespace>/<seu-usuário>` — namespace vem de Console → Governance → Tenancy Details |
| Secret | `OCIR_AUTH_TOKEN` | Auth Token gerado no passo 4 |
| Secret | `OKE_CLUSTER_ID` | OCID do cluster criado no passo 3 (Console → o cluster → Cluster Information) |
| Variable | `OCI_REGION_KEY` | Chave curta da região pro domínio do OCIR (ex.: `gru` para São Paulo, `iad` para Ashburn) — ver [lista de region keys](https://docs.oracle.com/en-us/iaas/Content/Registry/Concepts/registryprerequisites.htm#regional-availability) |

## 6. Rodar o pipeline

- **Manual**: aba *Actions* → `Deploy to Oracle Cloud (OKE) — Fallback` → *Run workflow*.
- Não dispara automaticamente em push (diferente do `deploy-eks.yml`) — é um fallback, não o
  caminho principal; dispare só quando decidir usar este plano.

O pipeline builda as 4 imagens **multi-arch/arm64**, publica no OCIR, escaneia com Trivy
(mesmo padrão do `deploy-eks.yml`), aplica `k8s/` inteiro e substitui a imagem de cada
Deployment pela recém-publicada — reaproveita os manifests em `k8s/` sem nenhuma alteração
(não há nada específico de AWS neles: sem `storageClassName` fixo, o `ingressClassName: nginx`
já é genérico).

## 7. Ingress e metrics-server (equivalentes aos passos do EKS)

```sh
# metrics-server — mesmo manifesto do EKS, funciona sem flags extras (certs do kubelet OKE
# são válidos, diferente do kind):
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Ingress Controller — variante "cloud" genérica, NÃO a "aws" usada no guia do EKS. O Cloud
# Controller Manager do OKE provisiona um Load Balancer OCI real para o Service da controller.
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/cloud/deploy.yaml
```

## 8. Acessar a aplicação

```sh
oci ce cluster create-kubeconfig --cluster-id <OCID_DO_CLUSTER> --file $HOME/.kube/config --region <region> --token-version 2.0.0
kubectl get ingress -n gamestore   # ADDRESS = IP do Load Balancer OCI provisionado
```

## 9. Teardown

```sh
# Remove só a aplicação, mantém o cluster:
kubectl delete namespace gamestore

# Remove o cluster inteiro (fazer isso após gravar o vídeo):
oci ce cluster delete --cluster-id <OCID_DO_CLUSTER> --force
```

## 10. Se voltar a usar a AWS

Este documento é um fallback — não substitui `docs/eks-deploy.md`. Se o chamado da AWS for
resolvido, o caminho principal (EKS, `sa-east-1`, `t3.medium`) continua o plano A: mais simples
(sem a fricção de ARM64/multi-arch) e já validado ponta a ponta localmente via kind.
