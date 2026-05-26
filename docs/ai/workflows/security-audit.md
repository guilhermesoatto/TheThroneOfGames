# [WORKFLOW: SECURITY & VULNERABILITY AUDIT (RALPH LOOP)]

**Gatilho:** Acionado antes de grandes merges, quando o usuário solicitar "faça uma auditoria de segurança", ou periodicamente para avaliar a saúde do projeto.

## A FILOSOFIA DE AUDITORIA (Zero-Trust)
Trate todo código gerado, dependência adicionada e input externo como malicioso até que se prove o contrário. Execute esta auditoria em loops isolados para não degradar seu contexto.

## FASE 1: AUDITORIA DE DEPENDÊNCIAS (Supply Chain Security)
1. **Verificação de Manifesto:** Leia os arquivos `.csproj` de cada projeto e o `Directory.Build.props`.
2. **Análise de CVEs:** Execute `dotnet list package --vulnerable` no terminal para identificar pacotes com vulnerabilidades conhecidas. Para cada pacote principal, utilize também sua base de conhecimento interna para verificar se há CVEs públicos nas versões especificadas.
3. **Pacotes Transitivos:** Execute `dotnet list package --vulnerable --include-transitive` para verificar vulnerabilidades em dependências transitivas.
4. **Ação:** Se encontrar pacotes defasados ou comprometidos, gere um relatório imediato sugerindo o *bump* da versão ou substituição do pacote. Pause o workflow e aguarde aprovação.

### Comandos de Auditoria:
```bash
# Verificar pacotes vulneráveis (diretos)
dotnet list package --vulnerable

# Verificar pacotes vulneráveis (incluindo transitivos)
dotnet list package --vulnerable --include-transitive

# Verificar pacotes desatualizados
dotnet list package --outdated

# Verificar pacotes depreciados
dotnet list package --deprecated
```

## FASE 2: LOOP DE ANÁLISE ESTÁTICA (SAST)
Inicie um loop de verificação nos arquivos do diretório `src/`, focando exclusivamente nas fronteiras (Adapters):
- **Alvo 1: Controllers (`/WebApi`)**
  - [ ] Os inputs (`[FromBody]`, `[FromQuery]`, `[FromRoute]`) estão sendo validados estritamente antes de serem repassados ao Use Case? (Utilize FluentValidation ou validação manual no Adapter, NUNCA no Domain).
  - [ ] Existe risco de IDOR (Insecure Direct Object Reference)? (Ex: O usuário pode acessar recursos de outro ID alterando o payload?)
  - [ ] Os endpoints estão protegidos com `[Authorize]` quando necessário?
  - [ ] O CORS está configurado restritivamente (não usar `AllowAnyOrigin()` em produção)?
- **Alvo 2: Repositories (`/Infrastructure`)**
  - [ ] Há alguma concatenação de strings bruta em queries de banco de dados? (Prevenção de SQL Injection — sempre usar parâmetros via EF Core ou Dapper parametrizado).
  - [ ] As connection strings estão vindo de `IConfiguration` / variáveis de ambiente e não hardcoded?
- **Alvo 3: Configuração e Secrets**
  - [ ] Há chaves de API, senhas ou tokens hardcoded no código fonte? (Tudo deve vir de `IConfiguration`, variáveis de ambiente ou Azure Key Vault / User Secrets em desenvolvimento).
  - [ ] O `appsettings.json` de produção está no `.gitignore`? Ou melhor, usa apenas variáveis de ambiente?
  - [ ] O `Program.cs` configura HTTPS redirection em produção?

### Roslyn Analyzers como Gate Automático:
O pipeline DEVE incluir os seguintes analyzers como barreira estática:
```xml
<!-- Em Directory.Build.props -->
<PropertyGroup>
  <AnalysisLevel>latest-all</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```
Execute `dotnet build` — com `TreatWarningsAsErrors`, qualquer warning de segurança do Roslyn bloqueia o build.

## FASE 3: AUDITORIA ARQUITETURAL (Boundary Enforcement)
Verifique se a arquitetura desenhada em `architecture.md` foi violada:
- [ ] O domínio (`/Domain`) referencia pacotes NuGet externos ou projetos de outras camadas? (Se sim, é uma falha crítica de design).
  - Validação: `dotnet test --filter "FullyQualifiedName~DomainPurityTests"` (testes ArchUnitNET).
- [ ] Os Controllers estão acessando repositórios diretamente bypassando o Use Case?
- [ ] O projeto `Infrastructure` referencia o projeto `WebApi`? (Referência circular — PROIBIDO).
- [ ] O projeto `Domain` tem `<PackageReference>` no `.csproj`? (PROIBIDO — deve ter zero referências externas).

### Verificação Manual do .csproj do Domain:
```bash
# O output deve ser VAZIO (zero PackageReference)
Select-String -Path "src/NomeDoAggregate.Domain/*.csproj" -Pattern "PackageReference"
```

## FASE 4: AUDITORIA DE CONTAINERS E RUNTIME (Docker & K8s)
- [ ] O Dockerfile usa imagens base oficiais da Microsoft? (`mcr.microsoft.com/dotnet/aspnet:10.0-alpine` para runtime, `mcr.microsoft.com/dotnet/sdk:10.0-alpine` para build).
- [ ] O Dockerfile usa multi-stage build para não incluir o SDK no container final?
- [ ] O container roda como non-root user?
- [ ] O K8s Deployment utiliza `securityContext` com `runAsNonRoot: true` e `readOnlyRootFilesystem: true`?
- [ ] As comunicações inter-pod usam mTLS (ex: via Istio/Linkerd service mesh)?
- [ ] Os Secrets do K8s são injetados como variáveis de ambiente (não como arquivos montados em paths previsíveis)?

### Dockerfile Seguro Padrão (.NET):
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/NomeDoAggregate.Domain/NomeDoAggregate.Domain.csproj", "src/NomeDoAggregate.Domain/"]
COPY ["src/NomeDoAggregate.Application/NomeDoAggregate.Application.csproj", "src/NomeDoAggregate.Application/"]
COPY ["src/NomeDoAggregate.Infrastructure/NomeDoAggregate.Infrastructure.csproj", "src/NomeDoAggregate.Infrastructure/"]
COPY ["src/NomeDoAggregate.WebApi/NomeDoAggregate.WebApi.csproj", "src/NomeDoAggregate.WebApi/"]
RUN dotnet restore "src/NomeDoAggregate.WebApi/NomeDoAggregate.WebApi.csproj"
COPY . .
RUN dotnet publish "src/NomeDoAggregate.WebApi/NomeDoAggregate.WebApi.csproj" \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "NomeDoAggregate.WebApi.dll"]
```

## FASE 5: RELATÓRIO FINAL
Gere um artefato markdown detalhado listando os achados categorizados por:
- 🔴 CRÍTICO (Bloqueante, ex: vazamento de secret, SQL injection, pacote com CVE crítico).
- 🟡 AVISO (ex: dependência levemente desatualizada, warning de analyzer não crítico).
- 🟢 PASS (Áreas verificadas e seguras).

### Formato do Relatório:
```markdown
# Security Audit Report — [NomeDoAggregate] — [Data]

## Sumário
| Categoria | Total |
|-----------|-------|
| 🔴 CRÍTICO | X |
| 🟡 AVISO   | X |
| 🟢 PASS    | X |

## Achados Detalhados
### 🔴 CRÍTICO
- **[SEC-001]** Descrição do achado...
  - **Arquivo:** `src/Infrastructure/...`
  - **Remediação:** ...

### 🟡 AVISO
- **[SEC-002]** Descrição do achado...

### 🟢 PASS
- Pureza de domínio verificada (ArchUnitNET: 0 falhas)
- `dotnet list package --vulnerable`: 0 vulnerabilidades
- Dockerfile: multi-stage, non-root, imagens oficiais
