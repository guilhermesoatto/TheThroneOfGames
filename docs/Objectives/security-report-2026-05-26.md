# Security Audit Report
**Date:** 2026-05-26  
**Scope:** `docs/Objectives/sprint-1`, `sprint-2`, `sprint-3` — all task files  
**Workflow reference:** `ia-arquiteto-hexagon-pattern/docs/ai/workflows/security-audit.md`  
**Audited by:** GitHub Copilot (automated — Phases 1–8)

---

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| 🔴 CRITICAL | 7 | 7 | 0 |
| 🟡 WARNING | 5 | 5 | 0 |
| 🟢 PASS | 13 | — | — |

---

## 🔴 CRITICAL Findings (All Fixed)

### C-01 — Base image not pinned to SHA digest
**File:** `sprint-1/tasks/T01-dockerfile.md`  
**Risk:** A bare tag (e.g., `node:20-alpine`) can be silently overwritten in the registry, introducing a malicious or vulnerable layer without any code change.  
**Fix applied:** Acceptance criteria updated to require `node:20-alpine@sha256:<digest>`. Best Practices and Technical Notes updated with `docker inspect` command to retrieve the current digest.

---

### C-02 — `trivy --scanners secret` missing from CI/CD gates
**Files:** `sprint-1/tasks/T01-dockerfile.md`, `sprint-1/tasks/T03-cd-pipeline.md`  
**Risk:** A developer could accidentally commit a `.env` file or API key, which gets baked into an image layer and pushed to the Container Registry without detection.  
**Fix applied:** Both files now require `trivy image --scanners secret --exit-code 1` as a blocking gate before `docker push`.

---

### C-03 — No `npm audit` or SAST gate in CI Pipeline
**File:** `sprint-1/tasks/T02-ci-pipeline.md`  
**Risk:** Vulnerable npm dependencies and static code issues (SQL injection patterns, insecure regex) would reach `main` without automated detection.  
**Fix applied:** Three new gates added (in order): `npm audit --audit-level=high` → Semgrep/ESLint SAST → `npx tsc --noEmit`. Pipeline YAML example updated with `security-audit` job as first dependency.

---

### C-04 — No `trivy` CVE scan gate in CD Pipeline before push
**File:** `sprint-1/tasks/T03-cd-pipeline.md`  
**Risk:** A vulnerable build artifact would be pushed to the Container Registry and deployed without any vulnerability gate.  
**Fix applied:** CD Pipeline now builds the image locally first (`push: false, load: true`), runs both CVE and secret scans, and only pushes on zero findings.

---

### C-05 — No `kubectl apply --dry-run=server` gate in CD
**File:** `sprint-1/tasks/T03-cd-pipeline.md`  
**Risk:** Invalid Kubernetes manifests (missing required fields, wrong API version) would cause a failed deploy in production with no pre-validation step.  
**Fix applied:** `kubectl apply --dry-run=server` added as a blocking gate before the live apply.

---

### C-06 — Missing `NetworkPolicy` for Microservices
**File:** `sprint-3/tasks/T04-kubernetes-manifests.md`  
**Risk:** Without NetworkPolicy, any compromised Pod in the `fcg` namespace can reach any other Pod (including databases) \u2014 violates zero-trust and DDD isolation requirements.  
**Fix applied:** `NetworkPolicy` added as a mandatory acceptance criterion for each Microservice, with an example manifest that restricts ingress to the API Gateway Pod only and egress to the respective database + DNS.

---

### C-07 — Missing Pod `securityContext` and dedicated `ServiceAccount`
**File:** `sprint-3/tasks/T04-kubernetes-manifests.md`  
**Risk:** Without `readOnlyRootFilesystem`, a compromised container can write malware to disk. Without `allowPrivilegeEscalation: false`, a container binary with a setuid bit can escalate to root. The `default` ServiceAccount auto-mounts a Kubernetes API token, enabling lateral movement.  
**Fix applied:** Acceptance criteria, Best Practices, and Technical Notes updated with `securityContext` block and dedicated `ServiceAccount` with `automountServiceAccountToken: false`.

---

## 🟡 WARNING Findings (All Fixed)

### W-01 — No Prototype Pollution protection on `req.body`
**File:** `sprint-2/tasks/T01-users-microservice.md`  
**Risk:** A malicious Player could send `{ "__proto__": { "isAdmin": true } }` to pollute the Object prototype and bypass authorization checks.  
**Fix applied:** Acceptance criteria now requires keys `__proto__`, `constructor`, `prototype` to be explicitly rejected at the validation layer with HTTP 400.

---

### W-02 — No Content-Length limit (DoS vector)
**File:** `sprint-2/tasks/T01-users-microservice.md`  
**Risk:** An attacker can send an arbitrarily large payload to exhaust memory or CPU.  
**Fix applied:** Acceptance criteria now requires payloads > 1MB to be rejected with HTTP 413.

---

### W-03 — JWT `alg:none` attack not explicitly blocked
**File:** `sprint-2/tasks/T06-api-gateway.md`  
**Risk:** Some JWT libraries accept `alg: none` in the token header, effectively disabling signature verification. An attacker crafts a token granting admin access to any Player ID.  
**Fix applied:** Acceptance criteria now explicitly requires the Gateway to reject tokens with `alg` other than HS256/RS256.

---

### W-04 — CORS `Access-Control-Allow-Origin: *` not explicitly forbidden
**File:** `sprint-2/tasks/T06-api-gateway.md`  
**Risk:** A wildcard CORS policy allows any website to make credentialed requests to the FCG API on behalf of a logged-in Player.  
**Fix applied:** Acceptance criteria now forbids `*` and requires an `ALLOWED_ORIGINS` environment variable whitelist.

---

### W-05 — HTTP Security Headers absent from API Gateway and Microservice specs
**Files:** `sprint-2/tasks/T01-users-microservice.md`, `sprint-2/tasks/T06-api-gateway.md`  
**Risk:** Missing `Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` exposes Players to clickjacking, MIME sniffing, and protocol downgrade attacks.  
**Fix applied:** Both files now list the required security headers as acceptance criteria items.

---

## 🟢 PASS (No Action Required)

| # | Check | File |
|---|-------|------|
| P-01 | Non-root container user defined | T01-dockerfile.md |
| P-02 | Multi-stage build separates build from runtime | T01-dockerfile.md |
| P-03 | HEALTHCHECK instruction present | T01-dockerfile.md |
| P-04 | OIDC (Workload Identity) recommended over long-lived keys | T03-cd-pipeline.md |
| P-05 | Private registry specified (no public image exposure) | T04-container-registry.md |
| P-06 | Image immutable tag recommended | T04-container-registry.md |
| P-07 | Passwords stored as bcrypt (cost \u2265 12) | sprint-2/T01-users-microservice.md |
| P-08 | JWT secret via environment variable, not hardcoded | sprint-2/T01-users-microservice.md |
| P-09 | Payment credentials injected via env vars | sprint-2/T03-payments-microservice.md |
| P-10 | No PII in Event payloads (email_hash, not email) | sprint-2/T07-event-sourcing.md |
| P-11 | SealedSecrets / ESO recommended for K8s secrets | sprint-3/T06-configmaps-secrets.md |
| P-12 | Rate limiting defined at API Gateway | sprint-2/T06-api-gateway.md |
| P-13 | Idempotency key on payment provider calls | sprint-2/T03-payments-microservice.md |

---

## Outstanding Recommendations (Not yet in tasks — future sprints)

These items are in the security-audit.md workflow but are considered advanced and should be addressed when the team reaches production hardening:

| # | Item | Workflow Reference | Suggested Sprint |
|---|------|--------------------|-----------------|
| R-01 | mTLS between Pods (Istio `PeerAuthentication: STRICT`) | Phase 5 | Post-Sprint 3 |
| R-02 | Istio `AuthorizationPolicy` per Microservice | Phase 5 | Post-Sprint 3 |
| R-03 | External Secrets Operator (`refreshInterval: 90 days`) replacing SealedSecrets | Phase 4.4 | Sprint 3 hardening |
| R-04 | `PodDisruptionBudget` per Microservice | Phase 4 | Sprint 3 |
| R-05 | Login endpoint rate-limit tightened to 10 req/min (separate from global 100/min) | Phase 7 | Sprint 2 hardening |

---

## Final Checklist Status

- [x] Dockerfile pinned to digest + non-root user
- [x] `trivy image` CVE + secret scan in CI/CD
- [x] `npm audit` HIGH/CRITICAL gate in CI
- [x] NetworkPolicy applied per Microservice
- [x] Kubernetes Secrets via SealedSecrets (not plaintext Git)
- [x] Dedicated ServiceAccount with `automountServiceAccountToken: false`
- [x] JWT `alg:none` attack blocked at Gateway
- [x] CORS origin whitelist (no `*` in production)
- [x] HTTP Security Headers in API Gateway and User-facing Microservice
- [x] Prototype Pollution protection on `req.body`
- [x] Content-Length limit (>1MB rejected)
- [ ] mTLS / Service Mesh (deferred — see R-01)
- [ ] External Secrets Operator rotation (deferred — see R-03)
