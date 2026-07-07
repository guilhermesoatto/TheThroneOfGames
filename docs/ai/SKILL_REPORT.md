# SKILL REPORT — ia-arquiteto-hexagon-pattern
**Generated:** 2026-06-20  
**Purpose:** Agent-to-agent handoff report. This file is the single source of truth for any AI agent consuming this repository. Read this file **first**, before reading any skill individually.  
**Scope:** Full semantic analysis, token cost audit, consumption proof, optimization findings, and scoring of all 13 skills across 3 stacks.

---

## 0. ENVIRONMENT STATE

| Check | Status | Detail |
|---|---|---|
| `.vscode/settings.json` | ✅ ACTIVE | `copilot-instructions.md` wired as `codeGeneration.instructions` — agent consumes Layer 0 on every code request |
| Python / pip | ❌ NOT INSTALLED | `python` resolves to Windows Store alias — ChromaDB tooling cannot run |
| ChromaDB vector store | ❌ EMPTY | `docs/ai/knowledge/chroma/` directory does not exist — knowledge base not populated |
| Node.js | ✅ v24.11.1 | Runtime present |
| npm dependencies | ❌ UNMET | `npm install` not run — `chromadb`, `@modelcontextprotocol/sdk`, `typescript` missing |
| `copilot-instructions.md` | ✅ CONSUMED | VS Code Copilot loads it via `settings.json` → Layer 0 guardrails are active |
| Skill files | ⚠️ PARTIALLY CONSUMED | Skills are referenced by `copilot-instructions.md` routing table but only consumed when agent explicitly reads them — no automatic injection of Layer 3 |
| Workflow files | ⚠️ MANUALLY TRIGGERED | Workflows are consumed only when agent is told to run them — not injected by default |

**Key finding:** The copilot-instructions.md (Layer 0, ~1,559 tokens) is the ONLY file automatically injected into every agent request. All skills (Layer 3) are on-demand via routing commands or explicit agent reads. ChromaDB is not functional until Python + `pip install -r tools/requirements.txt` + `python tools/embed-knowledge.py` are executed.

---

## 1. SEMANTIC ANALYSIS — WHAT THIS REPO DOES

### 1.1 Core concept

This repo is a **portable AI behavior constitution** — a set of markdown files that, when installed into a project, make any AI coding agent behave like a Senior Cloud-Native Software Architect following DDD + Hexagonal Architecture + Event-Driven Architecture + Zero-Trust security.

### 1.2 System design followed

```
┌─────────────────────────────────────────────────────────────────────┐
│                     SYSTEM DESIGN: DDD + HEXAGONAL                 │
│                                                                     │
│  Domain (pure)                                                      │
│  ├── Aggregate Root (1 per container/pod)                           │
│  ├── Value Objects (immutable, validate in constructor)             │
│  ├── Domain Events (8-field contract, versioned)                    │
│  └── Domain Errors (typed, no generic throw)                        │
│           ↓ emit events                                             │
│  Application (Use Cases / Ports)                                    │
│  ├── Use Cases return Either<Error,T> — never throw                 │
│  ├── Input Ports (Use Case interfaces)                              │
│  └── Output Ports (Repository/Publisher interfaces)                 │
│           ↓ inject                                                  │
│  Infrastructure (Adapters)                                          │
│  ├── Repositories (EF Core / pg)                                    │
│  ├── Event Publishers (Outbox → Kafka/RabbitMQ)                     │
│  ├── HTTP Adapters (controllers, DTOs)                              │
│  └── Cache (Redis decorator)                                        │
│           ↓ compose                                                 │
│  Composition Root (main.ts / Program.cs)                            │
│  └── ONLY place where concrete classes are instantiated             │
│                                                                     │
│  Cross-cutting (always active)                                      │
│  ├── OpenTelemetry: trace_id + correlation_id on every I/O          │
│  ├── Structured JSON logs (no free-text console.log)                │
│  ├── ESLint domain guard / ArchUnitNET (CI enforcement)             │
│  └── ChromaDB semantic memory (4 collections)                       │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.3 Semantic clusters identified in the repo

| Cluster | Files | Semantic role |
|---|---|---|
| **Behavioral constitution** | `agent-laws.md`, `copilot-instructions.md` | Define HOW the agent behaves — laws, autonomy limits, bootstrap order |
| **Domain purity** | `typescript-clean-arch.md`, `dotnet-clean-arch.md`, `angular-clean-arch.md` | Define WHAT is allowed in the domain layer — import whitelist, patterns |
| **Error & result** | All clean-arch files (Either / Result type) | Define how failures propagate without exceptions — machine-verifiable |
| **Observability** | `opentelemetry-logs.md` (×2), `angular-observability.md` | Define structured log shape and trace propagation — same semantics, 3 syntax variants |
| **Testing contracts** | `node-native-testing.md`, `xunit-native-testing.md`, `angular-testing.md` | Define test syntax per stack — no mocks in domain, infra via Testcontainers |
| **Event schema** | `event-versioning.md` | Define backward-compatible event evolution — 3 strategies |
| **Persistence** | `efcore-postgres.md` | Define Repository, Outbox, schema isolation, migration rules |
| **Knowledge retrieval** | `chroma-search.md` | Define how agent queries semantic memory before decisions |
| **Process automation** | `new-aggregate.md`, `business-to-code.md`, `quality-assurance.md` | Define multi-step decision loops |

### 1.4 Semantic consistency findings

**Strong:** The `Either<L,R>` / `Result<T>` pattern is consistently defined in all 3 stacks with identical semantics (tagged union, factory functions, no external library). Agent can apply it reliably.

**Strong:** Domain purity contract is identical across stacks — the only difference is syntax (TS `interface` vs C# `record` vs Angular `namespace`). Semantics are preserved.

**Inconsistency detected:** `opentelemetry-logs.md` (TypeScript) field names use `camelCase` (`traceId`, `correlationId`) while the `.NET` skill uses the `{TraceId}` Serilog structured logging syntax. The `angular-observability.md` uses camelCase. This is correct per-stack but an agent reading both files on the same project may get confused. **Recommendation:** Add explicit cross-reference note.

**Inconsistency detected:** `node-native-testing.md` mandates `assert.throws()` for domain errors thrown in constructors, but `typescript-clean-arch.md` mandates Value Objects return `Either<L,R>` (no throw). These are different approaches for the same scenario. **Recommendation:** Clarify: domain VOs with factory method → Either; domain VOs with constructor validation → throws (acceptable). The inconsistency is real but the resolution exists in the Either skill.

**Overlap detected:** `agent-laws.md §7` (Routing table) duplicates the routing table in `copilot-instructions.md [ROTEAMENTO DE CONTEXTO]`. Both consumed in same agent session = ~200 duplicate tokens per session.

---

## 2. SKILL SCORING TABLE

Scoring criteria:
- **Token cost** (lower = better): estimated tokens from raw char count ÷ 4. Budget: ≤800T=excellent, ≤1500T=good, ≤2500T=acceptable, >2500T=expensive
- **Signal density** (higher = better): ratio of actionable rules + code examples vs. prose. 1-5 scale.
- **Reliability** (higher = better): how mechanically verifiable are the rules (linter, test, CI vs. agent memory only). 1-5 scale.
- **Duplication** (lower = better): overlap with other files in the same context window.
- **ChromaDB readiness**: structure is chunked by `##` headings → each section is a retrievable vector.

| # | Skill File | Est. Tokens | Signal Density | Reliability | Duplication | ChromaDB Ready | Overall Score |
|---|---|---|---|---|---|---|---|
| 01 | `typescript-clean-arch.md` | 2,646 | 5/5 | 5/5 (ESLint enforced) | Low | ✅ | **A** |
| 02 | `dotnet-clean-arch.md` | 3,511 | 5/5 | 5/5 (ArchUnitNET enforced) | Low | ✅ | **A−** (expensive) |
| 03 | `angular-clean-arch.md` | 2,045 | 5/5 | 4/5 (ESLint, no arch tests) | Low | ✅ | **A** |
| 04 | `efcore-postgres.md` | 2,632 | 5/5 | 4/5 (code enforced, no arch test) | Low | ✅ | **A−** |
| 05 | `node-native-testing.md` | 316 | 4/5 | 5/5 (fails if wrong syntax) | None | ✅ | **A+** (best token/value) |
| 06 | `xunit-native-testing.md` | 1,554 | 5/5 | 5/5 (fails if wrong package) | Low | ✅ | **A** |
| 07 | `opentelemetry-logs.md` (TS) | 574 | 4/5 | 3/5 (no lint enforcement) | Medium (angular-obs) | ✅ | **B+** |
| 08 | `opentelemetry-logs.md` (.NET) | 1,442 | 4/5 | 3/5 (no lint enforcement) | Low | ✅ | **B+** |
| 09 | `angular-observability.md` | 1,658 | 4/5 | 4/5 (GlobalErrorHandler enforced) | Medium (otel-ts) | ✅ | **B+** |
| 10 | `event-versioning.md` | 1,712 | 5/5 | 2/5 (agent memory only, no CI gate) | Low | ✅ | **B** |
| 11 | `chroma-search.md` | 2,080 | 3/5 | 2/5 (requires Python env, not installed) | None | ✅ | **C+** |
| 12 | `agent-laws.md` | 2,431 | 4/5 | 3/5 (agent memory, some CI overlap) | High (duplicates routing) | ✅ | **B−** |
| 13 | `angular-testing.md` | 2,090 | 4/5 | 4/5 (test runner rejects wrong syntax) | Medium (xunit-testing) | ✅ | **B+** |

**Total budget if ALL skills loaded:** ~24,691 tokens  
**Typical session budget (1 stack + laws + otel):** ~6,000–8,000 tokens

---

## 3. TOKEN OPTIMIZATION FINDINGS

### 3.1 Critical over-budget files

**`dotnet-clean-arch.md` — 3,511 tokens** (largest file)
- §6 Bootstrap templates (tsconfig equivalent) + §7 Program.cs template = ~1,200 tokens of boilerplate
- The `ArchUnitNET` test block = ~450 tokens of C# test code that repeats the same pattern 6 times
- **Optimization:** Extract ArchUnitNET tests to a separate `dotnet-arch-tests.md` skill called only during scaffold phase. Extract Program.cs template to `dotnet-bootstrap.md`. Saves ~1,400 tokens (40%).

**`typescript-clean-arch.md` — 2,646 tokens**
- §5 ESLint config block = ~400 tokens of JSON that the agent can generate from 4 rules
- §6 tsconfig + package.json = ~500 tokens of scaffold templates used only once per project
- §7 Composition Root template = ~350 tokens
- **Optimization:** Split into `typescript-clean-arch.md` (core rules, ~900T) + `typescript-bootstrap.md` (scaffold templates, ~800T). Saves ~1,300 tokens in daily sessions.

**`chroma-search.md` — 2,080 tokens**
- The "Maintenance" section and "Reflexion Loop" flow diagram = ~400 tokens rarely needed
- The full parameter table duplicates what the CLI `--help` would show
- **Optimization:** Compress to quick-reference card format. Save ~700 tokens.

**`agent-laws.md` — 2,431 tokens**
- §7 routing table (200T) duplicates `copilot-instructions.md` routing table exactly
- §5 checklist prose could be a compact table
- **Optimization:** Remove §7 (reference to copilot-instructions.md instead). Compress §5 to table format. Saves ~400 tokens.

### 3.2 Under-budget wins (keep as-is)

- **`node-native-testing.md`** — 316 tokens. Perfect signal/token ratio. Gold standard.
- **`opentelemetry-logs.md` (TS)** — 574 tokens. Compact, specific, single JSON example. Keep.

### 3.3 Loading strategy recommendation

Instead of loading all skills, use ChromaDB-routed loading:

```
TIER 0 (always loaded — ~1,559T):
  copilot-instructions.md

TIER 1 (load at session start — ~400T):
  docs/ai/knowledge/CONTEXT.md

TIER 2 (load per stack — ~2,000–3,500T):
  [stack]-clean-arch.md  (one stack only)

TIER 3 (load per task — ~300–1,700T):
  Query ChromaDB → get relevant section chunks → load only what matched
  NEVER load full skill file if ChromaDB chunk is enough

TIER 4 (load on trigger only):
  event-versioning.md    → only when "alter/rename/remove event field"
  efcore-postgres.md     → only when "add repository or migration"
  agent-laws.md          → only at session bootstrap (once)
  security-audit.md      → only when "audit" trigger
  scaling-io.md          → only when "bottleneck" trigger
```

**Projected token budget with tiered loading:**
- Typical code task: ~4,000–5,500 tokens (vs. 24,691 full load)
- 78% token reduction with no reliability loss

---

## 4. RELIABILITY ANALYSIS

### 4.1 Mechanically enforced rules (high reliability)

These rules cannot be violated without a build/lint failure:

| Rule | Enforcement mechanism |
|---|---|
| No infra imports in `/domain` (TypeScript) | `.eslintrc.domain.json` → `no-restricted-imports` |
| No infra imports in `/Domain` (.NET) | ArchUnitNET tests → fail `dotnet test` |
| No Angular imports in `/domain` (Angular) | `.eslintrc.json` override |
| `tsconfig.json strict: true` | TypeScript compiler rejects unsafe code |
| `Directory.Build.props TreatWarningsAsErrors` | .NET compiler rejects warnings |
| xUnit + FluentAssertions pattern | Test runner fails if wrong package referenced |
| node:test native pattern | Fails at import if wrong runner |
| CI security gate | `.github/workflows/ci.yml` runs audit → blocks merge |

### 4.2 Agent-memory-only rules (lower reliability)

These rely on the agent reading and remembering the instruction — no mechanical gate:

| Rule | Risk | Mitigation available |
|---|---|---|
| `correlation_id` propagation in I/O | Agent may forget on new adapters | Add ESLint rule: detect `fetch()` / `axios` calls without `x-correlation-id` header |
| Event versioning strategy (Dual-Write deadline) | Agent may skip dual-write period | Add prd.json field: `eventMigrationDeadline` to make it explicit |
| Domain Expert approval before naming Aggregate | Agent may proceed autonomously | `agent-laws.md §3` documents this but no hard gate |
| `reflect.py` execution after phase approval | Agent may skip | `new-aggregate.md` Phase 3.1 mandates it — but no CI gate verifies ADR was generated |
| ADR contradiction detection | Agent may silently contradict past ADRs | ChromaDB decision-log query at session start (§1 bootstrap) mitigates but requires chroma populated |

### 4.3 Reliability improvement opportunities

1. **Add ESLint rule for correlation_id:** Detect any HTTP call in `/infrastructure` layer that does not pass trace headers. This converts a memory-only rule to a mechanical one.
2. **Add prd.json schema validation:** A JSON schema for prd.json would let CI reject malformed state files before the agent consumes them.
3. **Add ADR count assertion in CI:** `.github/workflows/ci.yml` could check that `decisions/` folder is non-empty after any domain phase is marked complete in a prd.json.

---

## 5. FAST CONTEXT CARD — FOR CONSUMING AGENTS

> **Copy this section into your context window at session start.** It is a compressed version of everything you need to know about this repo before writing code.

```
SYSTEM: ia-arquiteto-hexagon-pattern (DDD + Hexagonal Pattern Suitcase)
DATE: 2026-06-20 | BRANCH: main (clean, only branch)

ARCHITECTURE:
  Domain → Application → Infrastructure → Presentation
  ONE dependency direction: inward only
  Aggregate Root = Docker container = K8s Pod
  Bounded Context = K8s Namespace
  Cross-domain communication: async events ONLY (no sync HTTP between domains)

MANDATORY PATTERNS:
  Error handling:   Either<L,R> (TS/Angular) | Result<T> (C#) — never throw for business rules
  Logging:         JSON only | fields: timestamp,level,aggregate,action,traceId,correlationId
  Events:          8 required fields: eventId,eventType,eventVersion,aggregateId,aggregateName,occurredAt,correlationId,payload
  Testing:         Domain tests = zero mocks, zero infra | Integration = Testcontainers / node:test
  Domain purity:   /domain has ZERO imports from infra, frameworks, or node_modules (except crypto)
  Composition:     main.ts / Program.cs is the ONLY place concrete classes are instantiated

ACTIVE CONSTRAINTS:
  - Python not installed → ChromaDB unavailable → skip semantic search, use direct file reads
  - npm install not run → TypeScript project non-runnable
  - No active Aggregates → CONTEXT.md is blank template

BOOTSTRAP SEQUENCE FOR NEW TASK:
  1. Read CONTEXT.md → check active aggregates and ADRs
  2. Read architecture.md → confirm layer rules
  3. Read [stack]-clean-arch.md for the target stack
  4. Read agent-laws.md §2 (MUST/NEVER laws) and §3 (autonomy boundaries)
  5. Check docs/ai/tasks/prd-[name].json if aggregate work is ongoing
  6. If new feature: run business-to-code.md workflow BEFORE writing code

LAYER HIERARCHY (conflict resolution):
  Layer 0 (copilot-instructions.md) > Layer 1 (architecture.md) > Layer 2 (workflows/) > Layer 3 (skills/)
  If conflict: follow higher layer, report to Domain Expert.

ROUTING SHORTCUTS:
  New aggregate:    docs/ai/workflows/new-aggregate.md
  Pattern choice:   query-knowledge.py "<problem>" --collection architecture
  Business terms:   query-knowledge.py "<term>" --collection business-rules
  Past decisions:   query-knowledge.py "<topic>" --collection decision-log
  After approval:   python tools/reflect.py --prd ... --phase ...
```

---

## 6. SKILL-BY-SKILL DETAILED SCORES

### S01 — `typescript-clean-arch.md` | Score: A | 2,646 tokens

**What it does:** Defines domain layer purity rules, Either<L,R> canonical implementation, ESLint domain guard JSON, tsconfig bootstrap, Composition Root template.

**What the agent can rely on (high confidence):**
- Either<L,R> exact implementation — copy verbatim, zero ambiguity
- ESLint config — machine-verifiable, CI catches violations
- Import whitelist — `crypto.randomUUID()` only exception in domain
- Composition Root pattern — instantiation only in `src/main.ts`

**What requires judgment (lower confidence):**
- "Prefer Either over try/catch for business rules" — but testing skill uses `assert.throws()` → needs context to reconcile (throws = domain constructors, Either = use case return)

**Token optimization target:** Extract §6 + §7 (bootstrap templates) to `typescript-bootstrap.md`. Core rules (§1–§5) = ~950 tokens. Split saves ~1,700 tokens in daily sessions.

**ChromaDB chunks:** 7 sections via `##` heading → good chunk granularity. Section §4 (Either implementation) is the highest-value retrievable chunk.

---

### S02 — `dotnet-clean-arch.md` | Score: A− | 3,511 tokens

**What it does:** C# 13 equivalents of S01 — domain purity, `Result<T>` struct, ArchUnitNET enforcement, Directory.Build.props, Program.cs DI wiring.

**What the agent can rely on (high confidence):**
- `Result<T>` readonly struct — exact code, copy verbatim
- ArchUnitNET test stubs — mechanical enforcement, CI failure if domain imports infra
- `<Nullable>enable</Nullable>` + `TreatWarningsAsErrors` — compiler-enforced

**Token optimization target:** Split: `dotnet-clean-arch.md` (domain rules, §1–§4 = ~1,100T) + `dotnet-arch-tests.md` (ArchUnitNET stubs = ~450T, scaffold-only) + `dotnet-bootstrap.md` (Program.cs + Directory.Build.props = ~600T, scaffold-only). Saves ~1,400 tokens daily.

---

### S03 — `angular-clean-arch.md` | Score: A | 2,045 tokens

**What it does:** Angular 17+ domain purity, Result<T> (different signature from TS Either), Smart/Dumb component pattern, Signals for state, Reactive Forms mandate, correlation interceptor stub.

**Inconsistency to note:** Uses `Result<T>` (success/failure boolean) not `Either<L,R>` (tagged union). This is intentional (Angular/RxJS pattern) but an agent working across TS + Angular must know they are semantically equivalent but syntactically different.

**Token optimization target:** §5 (Interceptor) is 30 tokens and could be a note "see angular-observability.md §2" — it duplicates what angular-observability provides. Save ~200 tokens.

---

### S04 — `efcore-postgres.md` | Score: A− | 2,632 tokens

**What it does:** Full EF Core + PostgreSQL stack: DbContext rules, IEntityTypeConfiguration patterns, Outbox pattern implementation, Repository with domain event enqueue, DI registration, K8s connection string pattern.

**Highest value sections:**
- §3 DbContext: `NEVER` list is gold — 4 hard rules with clear alternatives
- §5 Outbox: complete implementation with partial index — copy verbatim
- §9 Prohibitions table: compact, machine-scannable

**Token optimization target:** §8 connection string section (80 tokens) is redundant — covered by Layer 0 (no hardcode) and K8s docs. Remove or summarize to 2 lines. Save ~80 tokens.

---

### S05 — `node-native-testing.md` | Score: A+ | 316 tokens

**What it does:** Mandates node:test + assert/strict. Provides syntax mapping from Jest to native. Zero boilerplate, pure signal.

**This is the gold standard for token efficiency.** Every other skill should aim for this density. No optimization needed.

---

### S06 — `xunit-native-testing.md` | Score: A | 1,554 tokens

**What it does:** xUnit + FluentAssertions + NSubstitute patterns, AAA structure, Testcontainers integration test setup, `WebApplicationFactory` + PostgreSQL container.

**Token optimization target:** §4 integration test block (Testcontainers setup = ~400T) could be a separate `dotnet-integration-tests.md` called only during Phase 3 (infrastructure). Saves ~400 tokens in domain/use-case phases.

---

### S07 — `opentelemetry-logs.md` (TypeScript) | Score: B+ | 574 tokens

**What it does:** JSON log format, `traceId`/`correlationId` propagation, sanitized error payload example.

**Gap:** No mention of `console.error` vs structured log in the example — the example shows `process.stdout.write(JSON.stringify(...))` which is correct but unusual for devs expecting a logger library.

**No optimization needed.** Already compact.

---

### S08 — `opentelemetry-logs.md` (.NET) | Score: B+ | 1,442 tokens

**What it does:** Serilog setup, OpenTelemetry config, CorrelationId middleware, error + success log pattern.

**Token optimization target:** §1 NuGet package list (100T) duplicates what `dotnet-clean-arch.md §7` already declares for Program.cs. Remove §1 and add "see dotnet-clean-arch.md §7 for NuGet packages". Save ~100 tokens.

---

### S09 — `angular-observability.md` | Score: B+ | 1,658 tokens

**What it does:** `TraceService`, HTTP interceptor, `ErrorLogService` (structured JSON), `GlobalErrorHandler`, 7 inviolable rules table.

**Semantic overlap:** `angular-clean-arch.md §5` already defines a `correlationIdInterceptor` stub. `angular-observability.md` redefines it as `traceInterceptor` with different naming. An agent reading both will implement two interceptors when one is needed.

**Optimization:** Replace `angular-clean-arch.md §5` with "see angular-observability.md for the HTTP trace interceptor". Eliminates duplicated interceptor code (~200T).

---

### S10 — `event-versioning.md` | Score: B | 1,712 tokens

**What it does:** Decision tree for event schema evolution (Payload Expansion, Dual-Write, Parallel Version). Clear strategy selection, code examples, broker routing guidance.

**Reliability gap:** The Dual-Write deadline is a calendar date — nothing in CI enforces that the legado field is removed after the deadline. This is a pure agent-memory dependency.

**No token optimization needed.** Well-structured, good signal density. The decision tree alone justifies loading this skill when event schema changes occur.

---

### S11 — `chroma-search.md` | Score: C+ | 2,080 tokens

**What it does:** Documents how to run `query-knowledge.py`, interpret results, add knowledge, and explains the Reflexion Loop.

**Critical finding:** This file is **non-functional in the current environment** (Python not installed, ChromaDB not populated). An agent reading this file and attempting to follow it will immediately hit an error.

**Reliability:** 2/5 — depends entirely on Python ecosystem being installed.

**Token optimization target:** This skill should be split:
- `chroma-search-quickref.md` (~400T): commands table + relevance thresholds only — loaded daily
- `chroma-search-setup.md` (~600T): setup, maintenance, Reflexion Loop explanation — loaded once at project init

**Functional fix required:** Add a guard at the top:
```
PREREQUISITE CHECK: python --version && pip show chromadb
If either fails → do NOT attempt chromadb queries → use direct file reads instead
```

---

### S12 — `agent-laws.md` | Score: B− | 2,431 tokens

**What it does:** Behavioral constitution — 7 sections covering bootstrap order, 10 MUST + 10 NEVER laws, autonomy boundaries, failure protocol, 25-item self-check, exception protocol, routing table.

**Highest value:** §2 MUST/NEVER tables (compact, machine-scannable, ~350 tokens). §5 checklist (~250 tokens). These two sections alone are worth loading.

**Duplication:** §7 routing table is a verbatim copy of `copilot-instructions.md [ROTEAMENTO DE CONTEXTO]`. In any agent session both are loaded → ~200 duplicate tokens every request.

**Signal overload:** §3 autonomy list and §4 failure protocol are valuable but verbose. An experienced agent can derive most of these from §2 laws. 

**Token optimization target:** 
- Remove §7 entirely (save ~200T) — reference copilot-instructions.md instead
- Compress §3 and §4 to tables (save ~300T)
- Total saving: ~500 tokens (20% reduction)

---

### S13 — `angular-testing.md` | Score: B+ | 2,090 tokens

**What it does:** Karma/Jasmine + Jest patterns, TestBed usage, component testing with signals, service testing with spy, `HttpClientTestingModule`, smart/dumb component testing guidance.

**Gap:** Does not cover `node:test` — Angular uses a different test runner. But there's no explicit statement that Jest is permitted for Angular while prohibited in Node.js TypeScript. An agent working across stacks needs this disambiguation.

**Token optimization target:** The `HttpClientTestingModule` setup (~200T) could be referenced from Angular docs rather than duplicated here.

---

## 7. OPTIMIZATION RECOMMENDATIONS (PRIORITY ORDER)

### P1 — Critical (blocks reliability)

| # | Action | Files affected | Token impact |
|---|---|---|---|
| P1-01 | Add `PREREQUISITE` guard to `chroma-search.md` — check Python before attempting queries | `chroma-search.md` | +10T but prevents broken session |
| P1-02 | Clarify TS testing: constructor → throws; use case → Either (add 2-line note to `node-native-testing.md`) | `node-native-testing.md` | +20T, resolves semantic ambiguity |
| P1-03 | Replace duplicate correlationId interceptor in `angular-clean-arch.md §5` with cross-reference to `angular-observability.md` | `angular-clean-arch.md` | −200T |

### P2 — High value (token savings)

| # | Action | Files affected | Token saving |
|---|---|---|---|
| P2-01 | Remove §7 from `agent-laws.md` (duplicate routing table) | `agent-laws.md` | −200T |
| P2-02 | Extract `dotnet-clean-arch.md` §6+§7 (bootstrap/Program.cs) to `dotnet-bootstrap.md` | `dotnet-clean-arch.md` | −1,400T from daily sessions |
| P2-03 | Extract `typescript-clean-arch.md` §6+§7 to `typescript-bootstrap.md` | `typescript-clean-arch.md` | −1,700T from daily sessions |
| P2-04 | Split `xunit-native-testing.md` — move Testcontainers section to `dotnet-integration-tests.md` | `xunit-native-testing.md` | −400T from domain/app phases |

### P3 — Quality improvements

| # | Action | Impact |
|---|---|---|
| P3-01 | Add ESLint rule for missing correlation headers in `infrastructure/` HTTP calls | Memory rule → mechanical enforcement |
| P3-02 | Add JSON schema for prd.json to enable CI validation | Prevents malformed state files |
| P3-03 | Add CI step: assert `decisions/` has ADR files when prd.json phase = complete | Enforces Reflexion Loop |
| P3-04 | Add cross-stack note in all OTel files: "TS uses camelCase, .NET uses PascalCase — semantics are identical" | Prevents agent confusion on multi-stack projects |

---

## 8. PROJECTED TOKEN BUDGET AFTER OPTIMIZATIONS

| Scenario | Before | After | Saving |
|---|---|---|---|
| Full load (all 13 skills) | 24,691T | ~18,500T | 25% |
| Typical TypeScript session (L0 + CONTEXT + ts-arch + laws + otel-ts) | 7,406T | ~3,500T | 53% |
| Typical .NET session (L0 + CONTEXT + dotnet-arch + laws + serilog) | 8,502T | ~4,500T | 47% |
| Angular feature session (L0 + CONTEXT + ng-arch + ng-obs) | 6,662T | ~3,800T | 43% |

---

## 9. CONSUMPTION PROOF — IS EACH FILE BEING USED?

| File | Consumed by | Evidence | Status |
|---|---|---|---|
| `copilot-instructions.md` | VS Code Copilot | `.vscode/settings.json` `codeGeneration.instructions` | ✅ Auto-injected |
| `docs/ai/architecture.md` | Agent on demand | Routing table in copilot-instructions.md | ⚠️ Demand only |
| `docs/ai/skills/agent-laws.md` | Agent on demand | Routing table: "leis de comportamento" | ⚠️ Demand only |
| `docs/ai/skills/typescript-clean-arch.md` | Agent on demand | Routing table: "padronização de código" | ⚠️ Demand only |
| `docs/ai/skills/node-native-testing.md` | Agent on demand | Referenced in quality-assurance.md workflow | ⚠️ Demand only |
| `docs/ai/skills/opentelemetry-logs.md` | Agent on demand | Referenced in new-aggregate.md workflow | ⚠️ Demand only |
| `docs/ai/skills/event-versioning.md` | Agent on demand | Referenced in architecture.md | ⚠️ Demand only |
| `docs/ai/skills/chroma-search.md` | Agent on demand | Referenced in copilot-instructions.md routing | ❌ Non-functional (Python missing) |
| `dotnet/docs/ai/skills/*` | Agent on demand | Routing in dotnet/copilot-instructions.md | ⚠️ Demand only, dotnet/copilot-instructions.md NOT in .vscode/settings.json |
| `angular/docs/ai/skills/*` | Agent on demand | Routing in angular/copilot-instructions.md | ⚠️ Demand only, angular/copilot-instructions.md NOT in .vscode/settings.json |
| `docs/ai/knowledge/CONTEXT.md` | Agent on demand | Routing + agent-laws §1 bootstrap | ⚠️ Demand only — should be Tier 1 auto-load |
| `docs/ai/knowledge/chroma/*` | ChromaDB | Python tools | ❌ Not populated (Python missing) |
| `.github/workflows/ci.yml` | GitHub Actions | CI pipeline | ✅ Active on push |
| `.eslintrc.domain.json` | ESLint in CI | Workflow step | ✅ Active in CI |

**Critical gap:** The `.NET` and `Angular` `copilot-instructions.md` files are NOT wired into `.vscode/settings.json`. If working on a .NET or Angular sub-folder, the agent falls back to the root guardrails only, not the stack-specific Layer 0 overrides. **Fix:** Add stack-specific instructions to settings.json or document this explicitly.

---

## 10. SUMMARY FOR RECEIVING AGENT

**This repo contains 13 skills + 5 workflows + 1 layer-0 constitution.** The system design is DDD + Hexagonal Architecture with Event-Driven inter-domain communication. The key contracts are:

1. **Error handling:** `Either<L,R>` / `Result<T>` — never throw for business rules
2. **Domain purity:** zero framework/infra imports in `/domain` — enforced by ESLint/ArchUnitNET
3. **Observability:** JSON logs with `traceId` + `correlationId` on every I/O operation
4. **Events:** 8-field contract, versioned, backward-compatible via 3 evolution strategies
5. **Testing:** domain tests = zero mocks; integration = real infra via Testcontainers
6. **Process:** business req → ChromaDB query → domain model validation → prd.json → phase loop → ADR → CONTEXT update

**Current environment gaps:** Python not installed (ChromaDB non-functional), `npm install` not run. All skills are functional as markdown instructions but the semantic search layer is offline.

**Best entry point for new work:** Read `docs/ai/knowledge/CONTEXT.md`, then `copilot-instructions.md [ROTEAMENTO]`, then the appropriate `[stack]-clean-arch.md`. Use `agent-laws.md §2` as your hard constraint checklist throughout.
