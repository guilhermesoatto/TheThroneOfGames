# 🏛️ AI Architect Instructions — Hexagonal Pattern Suitcase

> **Auto-generated on {{GENERATED_DATE}}** by `tools/generate-readme.js` after each PR merge.
> To update manually: `node tools/generate-readme.js`

A complete set of **AI instruction markdowns** that transform any AI coding agent (GitHub Copilot, Cursor, Cline, Windsurf) into a **Senior Cloud-Native Software Architect** that:

- Respects domain isolation (DDD + Hexagonal Architecture)
- Never violates layered boundaries (Ports & Adapters)
- Generates code with built-in observability (OpenTelemetry)
- Follows Zero-Trust security posture
- Works in iterative anti-context-degradation loops (Ralph Pattern)
- Speaks the business language of your domain (ChromaDB-grounded)
- Knows **when to apply which Design Pattern** — from business triggers, not theory

---

## 📦 Available Stacks

{{STACKS_TABLE}}

---

## 🧠 What is this?

This repository is a **development suitcase** — a portable set of AI instruction files you copy into your project. Once installed, your AI agent will:

1. Read business requirements in natural language
2. Query the **ChromaDB semantic knowledge base** to match terms and patterns
3. Propose domain models in business language — **waiting for your validation before coding**
4. Scaffold the full architecture iteratively via the **Ralph Pattern** (no context rot)
5. Enforce hexagonal boundaries via ESLint (TypeScript/Angular) and ArchUnitNET (.NET)
6. Generate production-ready code with tests, OpenTelemetry, and Docker/K8s manifests

---

## 🗂️ Layer Hierarchy

```
Layer 0  ►  copilot-instructions.md          — Inviolable guardrails (security, EDA, layers)
Layer 1  ►  docs/ai/architecture.md          — Architectural blueprint (DDD + Hexagonal + K8s)
Layer 2  ►  docs/ai/workflows/*.md           — Decision processes (when to do what)
Layer 3  ►  docs/ai/skills/*.md              — Code patterns (how to implement)
Knowledge ► docs/ai/knowledge/*.md + ChromaDB — Semantic memory (patterns + domain glossary)
```

> **Conflict rule:** If a skill (Layer 3) contradicts a guardrail (Layer 0), the agent MUST follow Layer 0 and report the conflict.

---

## 🚀 Quick Install

{{INSTALL_SECTION}}

---

## 🧬 ChromaDB — Semantic Knowledge Base

The knowledge base gives your agent **semantic memory**: before proposing any design, it queries the embedded vector database to find the right pattern and validate domain terminology.

{{CHROMA_SECTION}}

The `docs/ai/knowledge/chroma/` directory is **committed** — no re-embedding needed after cloning.

---

## 💬 Business Language Intake

Instead of writing code specs, tell the agent what the business needs:

> "I need to manage customer loyalty points"

The agent will:
1. Query ChromaDB for relevant patterns and glossary terms
2. Propose a domain model using **your project's own business language**
3. **Pause and ask you to validate** names, rules, and bounded context
4. Generate `docs/ai/tasks/prd-loyalty.json` as execution state
5. Implement in isolated loops — never losing context

See the workflow: `docs/ai/workflows/business-to-code.md`

---

## 📚 Skills (Code Patterns)

### TypeScript / Node.js
{{TS_SKILLS}}

### C# / .NET 10
{{DOTNET_SKILLS}}

### Angular
{{ANGULAR_SKILLS}}

---

## 🔄 Workflows (Decision Processes)

### TypeScript / Node.js
{{TS_WORKFLOWS}}

### C# / .NET 10
{{DOTNET_WORKFLOWS}}

### Angular
{{ANGULAR_WORKFLOWS}}

---

## 🗄️ Knowledge Base Files

{{KNOWLEDGE_FILES}}

---

## 🔁 PR Automation

When a PR is merged to `main`, the GitHub Action `.github/workflows/update-readme.yml` automatically:
1. Runs `node tools/generate-readme.js`
2. Commits the updated `README.md` back to `main` with `[skip ci]`

This ensures the README always reflects the current state of the suitcase.

---

## 📐 Design Patterns — When to Apply

The suitcase includes a business-triggered patterns catalog at `docs/ai/knowledge/design-patterns.md`.

Quick reference:
| Pattern | Business Trigger |
|---|---|
| Strategy | Rule changes per customer/context/region |
| Factory Method | Object creation varies by subtype |
| Builder | Complex objects with 5+ optional fields |
| Adapter | External API returns wrong shape |
| Decorator | Add cross-cutting behavior (cache, audit) without changing class |
| Facade | Coordinate 3+ subsystems transparently |
| Domain Event | Something happened — others need to react (decoupled) |
| Outbox | Guarantee event delivery even if service crashes mid-transaction |
| Saga | Multi-service operation needing compensating rollback |
| CQRS | Read and write have very different performance/complexity needs |
| Specification | Business eligibility rules combined in AND/OR/NOT |
| Smart/Dumb | Angular component mixing data orchestration and rendering |

---

## 🔒 Security Posture (Zero-Trust)

- All inputs validated at adapter boundaries — never inside domain
- No secrets hardcoded — environment variables only
- Docker images: pinned digests, non-root user, no shell
- K8s NetworkPolicy per Aggregate
- Angular: no `HttpClient` in components, no `bypassSecurityTrust*`, CSRF configured
- Supply chain: `npm audit` runs before every dependency change

---

_Last auto-generated: {{GENERATED_DATE}}_
