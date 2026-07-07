# [WORKFLOW: SECURITY & VULNERABILITY AUDIT (RALPH LOOP)]

**Gatilho:** Acionado antes de grandes merges, quando o usuário solicitar "faça uma auditoria de segurança", ou periodicamente para avaliar a saúde do projeto.

## A FILOSOFIA DE AUDITORIA (Zero-Trust)
Trate todo código gerado, dependência adicionada e input externo como malicioso até que se prove o contrário. Execute esta auditoria em loops isolados para não degradar seu contexto.

## FASE 1: AUDITORIA DE DEPENDÊNCIAS (Supply Chain Security)
1. **Verificação de Manifesto:** Leia o `package.json`.
2. **Análise de CVEs:** Para cada pacote principal, execute `npm audit --audit-level=moderate` e analise o output.
3. **Critérios de Bloqueio:**
   - **CRITICAL / HIGH:** Instalação BLOQUEADA. Busque pacote alternativo ou versão corrigida.
   - **MODERATE:** Avalie o impacto. Se afeta código em runtime (não apenas devDependency), BLOQUEIE.
   - **LOW:** Aceitar com registro no log de decisão. Monitorar em próxima sprint.
4. **Critérios de Seleção de Pacotes:** Antes de `npm install <pacote>`:
   - [ ] Downloads semanais > 1.000 no npmjs.com?
   - [ ] Último commit no repositório < 6 meses?
   - [ ] Zero CVEs HIGH/CRITICAL abertos no GitHub Advisory Database?
   - [ ] Licença compatível (MIT, Apache-2.0, BSD-2/3-Clause)?
   - [ ] Não é um pacote com namespace squatting ou typosquatting?
5. **Ação:** Se encontrar pacotes defasados ou comprometidos, gere um relatório sugerindo bump ou substituição. Pause e aguarde aprovação.

## FASE 2: LOOP DE ANÁLISE ESTÁTICA (SAST)
Inicie um loop de verificação nos arquivos do diretório `src/`, focando exclusivamente nas fronteiras (Adapters):
- **Alvo 1: Controllers (`/presentation`)**
  - [ ] Os inputs (`req.body`, `req.query`) estão sendo validados estritamente antes de serem repassados ao Use Case?
  - [ ] Existe risco de IDOR (Insecure Direct Object Reference)?
  - [ ] Payloads com `__proto__`, `constructor`, `prototype` são rejeitados? (Prototype Pollution)
  - [ ] `Content-Length` máximo definido? Rejeitar payloads > 1MB por padrão.
- **Alvo 2: Repositories (`/infrastructure`)**
  - [ ] Há alguma concatenação de strings bruta em queries? (SQL/NoSQL Injection)
  - [ ] Todas as queries usam prepared statements / parameterized queries?
  - [ ] Se MongoDB, operadores de query (`$gt`, `$ne`) não recebem input direto do usuário?
- **Alvo 3: Configuração e Secrets**
  - [ ] Há chaves de API, senhas ou tokens hardcoded no código fonte?
  - [ ] Tudo vem de `process.env`?
  - [ ] `child_process.exec()` NUNCA é chamado com input do usuário? (Command Injection)

## FASE 3: AUDITORIA ARQUITETURAL (Boundary Enforcement)
Verifique se a arquitetura desenhada em `architecture.md` foi violada:
- [ ] O domínio (`/domain`) importa algo de fora? (Executar `npx eslint src/domain/ --config .eslintrc.domain.json`)
- [ ] Os Controllers estão acessando repositórios diretamente bypassando o Use Case?
- [ ] Domain Events implementam `IDomainEvent` com todos os campos obrigatórios?
- [ ] O `Either` canônico está sendo usado (não variações ad-hoc)?

## FASE 4: SEGURANÇA DE CONTAINERS (Docker & K8s Hardening)

### 4.1 Dockerfile Hardening
Todo Dockerfile de Aggregate DEVE seguir:
```dockerfile
# Base image: SEMPRE versão pinada com digest, nunca :latest
FROM node:20-alpine@sha256:<digest> AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

FROM node:20-alpine@sha256:<digest>
# Usuário não-root obrigatório
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser
WORKDIR /app
COPY --from=builder /app/node_modules ./node_modules
COPY dist ./dist
COPY package.json .
# Sem shell para reduzir superfície de ataque
ENTRYPOINT ["node", "dist/index.js"]
```

### 4.2 Scanning de Imagem
```bash
# Obrigatório no CI/CD antes de push para registry
trivy image --severity HIGH,CRITICAL --exit-code 1 <image-name>:<tag>
trivy image --scanners secret <image-name>:<tag>
```
- Se HIGH/CRITICAL encontrado → Build BLOQUEADO.
- Se secrets embedados → Build BLOQUEADO + rotacionar credencial comprometida.

### 4.3 NetworkPolicy Obrigatória
Todo Aggregate DEVE ter NetworkPolicy que restringe tráfego ingress/egress:
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: <aggregate-name>-netpol
  namespace: <bounded-context>
spec:
  podSelector:
    matchLabels:
      app: <aggregate-name>
  policyTypes: [Ingress, Egress]
  ingress:
    - from:
        - podSelector: { matchLabels: { role: api-gateway } }
      ports: [{ port: 3000, protocol: TCP }]
  egress:
    - to:
        - podSelector: { matchLabels: { role: database } }
      ports: [{ port: 5432, protocol: TCP }]
    - to:
        - podSelector: { matchLabels: { role: message-broker } }
      ports: [{ port: 5672, protocol: TCP }]
    - to:
        - namespaceSelector: {}
          podSelector: { matchLabels: { k8s-app: kube-dns } }
      ports: [{ port: 53, protocol: UDP }]
```

### 4.4 Secrets Management
- **Proibido:** `kubectl create secret` com valores em texto puro no repositório.
- **Obrigatório:** External Secrets Operator (ESO) apontando para vault externo (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault).
- **Rotação:** Max 90 dias. Automática via ESO `refreshInterval`.

### 4.5 RBAC e ServiceAccount
- Cada Pod DEVE ter `ServiceAccount` dedicado com permissões mínimas.
- NUNCA usar o default ServiceAccount do namespace.
- `automountServiceAccountToken: false` se o Pod não precisa acessar a API do K8s.

## FASE 5: mTLS E SERVICE MESH (Comunicação Pod-to-Pod)
- **mTLS Obrigatório:** Toda comunicação entre Pods DEVE ser criptografada via mTLS.
- **Implementação:** Utilizar Istio ou Linkerd como Service Mesh.
- **PeerAuthentication:** Configurar `mode: STRICT` no namespace do Bounded Context:
```yaml
apiVersion: security.istio.io/v1beta1
kind: PeerAuthentication
metadata:
  name: <bounded-context>-mtls
  namespace: <bounded-context>
spec:
  mtls:
    mode: STRICT
```
- **AuthorizationPolicy:** Restringir quais services podem se comunicar:
```yaml
apiVersion: security.istio.io/v1beta1
kind: AuthorizationPolicy
metadata:
  name: <aggregate-name>-authz
  namespace: <bounded-context>
spec:
  selector:
    matchLabels:
      app: <aggregate-name>
  rules:
    - from:
        - source:
            principals: ["cluster.local/ns/<bounded-context>/sa/api-gateway-sa"]
      to:
        - operation:
            methods: ["GET", "POST", "PUT", "DELETE"]
```

## FASE 6: CI/CD SECURITY GATES (Pipeline Obrigatório)
O pipeline de CI/CD DEVE executar os seguintes gates na ordem estrita:

```
1. npm audit --audit-level=high         → Falha bloqueia merge
2. npx eslint src/domain/ --config .eslintrc.domain.json  → Valida pureza do domínio
3. npx tsc --noEmit                     → Compilação TypeScript sem erros
4. node --test                          → Testes nativos passam (unit + application)
5. docker build                         → Build da imagem
6. trivy image --severity HIGH,CRITICAL → Scanning de vulnerabilidades
7. kubectl apply --dry-run=server       → Valida manifests K8s
8. Deploy em staging                    → Smoke tests de integração
9. Deploy em produção                   → Apenas após aprovação do Domain Expert
```

### Regras de Gate:
- **Gates 1-6 são automáticos:** Qualquer falha bloqueia o pipeline.
- **Gate 7-8:** Falha gera alerta mas permite retry manual.
- **Gate 9:** Requer aprovação humana explícita (Human-in-the-Loop).

## FASE 7: HEADERS DE SEGURANÇA HTTP (Template de Controller)
Todo Controller REST DEVE configurar os seguintes headers de resposta:
```typescript
// Middleware obrigatório em /presentation/middlewares/security-headers.ts
const securityHeaders = (req: Request, res: Response, next: NextFunction) => {
  res.setHeader('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');
  res.setHeader('Content-Security-Policy', "default-src 'none'");
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('X-Frame-Options', 'DENY');
  res.setHeader('X-XSS-Protection', '0'); // Desabilitado — CSP é suficiente
  res.setHeader('Referrer-Policy', 'strict-origin-when-cross-origin');
  res.setHeader('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');
  next();
};
```

### CORS:
- **Proibido:** `Access-Control-Allow-Origin: *` em produção.
- **Obrigatório:** Origin whitelist configurada via variável de ambiente `ALLOWED_ORIGINS`.
- **Métodos:** Apenas os métodos necessários para os endpoints expostos.

### Rate Limiting:
- **Default:** 100 requests/minuto por IP.
- **Autenticado:** 1000 requests/minuto por user ID.
- **Crítico:** Endpoints de escrita podem ter limites menores (ex: 10 req/min para criação de recursos).
- **Implementação:** Via middleware em `/presentation/middlewares/rate-limiter.ts` usando Redis (ver `scaling-io.md`).

## FASE 8: RELATÓRIO FINAL
Gere um artefato em `docs/ai/tasks/security-report-[data].md` listando os achados categorizados por:
- 🔴 CRÍTICO (Bloqueante, ex: vazamento de secret, injeção, CVE HIGH).
- 🟡 AVISO (ex: dependência levemente desatualizada, header faltando).
- 🟢 PASS (Áreas verificadas e seguras).

### Checklist Final do Agente:
- [ ] `npm audit` retorna zero vulnerabilidades HIGH/CRITICAL?
- [ ] Dockerfile usa imagem pinada + usuário não-root?
- [ ] `trivy image` passou sem findings HIGH/CRITICAL?
- [ ] NetworkPolicy aplicada e restringe ingress/egress?
- [ ] Secrets via External Secrets Operator (não hardcoded)?
- [ ] ServiceAccount dedicado com `automountServiceAccountToken: false`?
- [ ] mTLS/PeerAuthentication `STRICT` configurado?
- [ ] AuthorizationPolicy restringe comunicação inter-service?
- [ ] Pipeline CI/CD com 9 gates configurados?
- [ ] Headers de segurança HTTP configurados no middleware?
- [ ] CORS com origin whitelist (não `*`)?
- [ ] Rate limiting configurado por IP e por user?
- [ ] ESLint domain purity guard sem erros?
- [ ] Inputs validados e sanitizados na Presentation Layer?
