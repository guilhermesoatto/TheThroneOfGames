# Program.cs CQRS Configuration Fix

## Contexto
- **Projeto:** TheThroneOfGames
- **Fase:** Phase 3 - CQRS Implementation  
- **Data:** 2025-12-03
- **Arquivo:** `TheThroneOfGames.API/Program.cs`
- **Linha:** 71-93 (Query Handlers Registration)

## Problema Identificado
O build está falhando com múltiplos erros de compilação:
- `CS0246: O nome do tipo ou do namespace "GetUserByIdQuery" não pode ser encontrado`
- `CS0246: O nome do tipo ou do namespace "GetUserByIdQueryHandler" não pode ser encontrado`
- Erros similares para todas as queries do contexto Usuarios

## Causa Raiz
### **Análise Técnica:**
1. **Using Statements Incompletos:** Program.cs inclui `using GameStore.Usuarios.Application.Queries` mas as queries não estão sendo encontradas
2. **Queries Implementadas Localmente:** As queries estão definidas em `UsuarioQueries.cs` mas não estão sendo exportadas corretamente
3. **Namespace Mismatch:** Possível conflito entre queries definidas em arquivos diferentes
4. **Falta de Export:** As classes de queries podem não estar publicadas no namespace correto

### **Arquitetural:**
O padrão CQRS exige que todas as queries e handlers estejam registrados no container DI para injeção de dependência. O Program.cs é o composition root onde todos os componentes CQRS devem ser registrados.

## Solução Implementada
### **Passo 1: Verificar Estrutura das Queries**
```csharp
// Em GameStore.Usuarios.Application.Queries.UsuarioQueries.cs
public record GetUserByIdQuery(Guid Id) : IQuery<UsuarioDTO?>;
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UsuarioDTO?>
```

### **Passo 2: Corrigir Program.cs**
Adicionar using statements específicos e registrar handlers corretamente:
```csharp
// Adicionar using statements específicos se necessário
using GameStore.Usuarios.Application.Queries.UsuarioQueries;

// Registrar Query Handlers com namespace completo
builder.Services.AddScoped<IQueryHandler<GetUserByIdQuery, UsuarioDTO?>, 
    GameStore.Usuarios.Application.Queries.UsuarioQueries.GetUserByIdQueryHandler>();
```

### **Passo 3: Verificar Handlers**
Confirmar que todos os handlers implementam a interface correta:
```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UsuarioDTO?>
{
    private readonly IUsuarioRepository _repository;
    
    public GetUserByIdQueryHandler(IUsuarioRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<UsuarioDTO?> HandleAsync(GetUserByIdQuery query)
    {
        // Implementação
    }
}
```

## Impacto
- **Build:** Crítico - Impede compilação do projeto
- **Testes:** 0 testes executáveis devido a erros de build
- **Cobertura:** 0% - Não é possível medir sem build funcional
- **CQRS:** Implementação incompleta - Queries não registradas no DI

## Decisão Técnica
### **Por que esta abordagem:**
1. **Manutenção do Padrão:** CQRS exige registro explícito no DI container
2. **Type Safety:** Interfaces genéricas garantem compile-time checking
3. **Testabilidade:** Handlers registrados permitem mocking em testes
4. **Performance:** Resolução de dependências otimizada em runtime

### **Design Considerations:**
- **Namespace Claro:** Evitar ambiguidade entre contexts
- **Interface Consistente:** Todos os handlers seguem `IQueryHandler<TQuery, TResult>`
- **Dependency Injection:** Centralizado no Program.cs para visibilidade

## Alternativas Consideradas
1. **Auto-Registration:** Usar reflection para registrar automaticamente
   - **Pros:** Menos código boilerplate
   - **Cons:** Menos controle, performance impact, dificuldade em debug
   
2. **Separate Registration File:** Criar `DependencyInjectionConfig.cs`
   - **Pros:** Program.cs mais limpo
   - **Cons:** Mais complexidade, indireção adicional

3. **Attributes Registration:** Usar atributos nos handlers
   - **Pros:** Declarativo
   - **Cons:** Requer framework adicional, menos flexível

**Decisão:** Manter registro explícito no Program.cs por simplicidade e controle.

## Próximos Passos
1. **Immediate:** Corrigir using statements e registro no Program.cs
2. **Validation:** Executar `dotnet build` para confirmar sucesso
3. **Testing:** Executar `dotnet test` para verificar cobertura
4. **Documentation:** Atualizar arquitetura com padrão CQRS completo
5. **Integration:** Testar endpoints com queries registradas

## Referências
- **Arquivos Relacionados:**
  - `GameStore.Usuarios.Application.Queries.UsuarioQueries.cs`
  - `GameStore.Catalogo.Application.Queries.CatalogoQueries.cs`
  - `GameStore.Vendas.Application.Queries.VendasQueries.cs`
  - `GameStore.CQRS.Abstractions.IQueryHandler<T,TResult>`

- **Documentação Técnica:**
  - `docs/PHASE_3_COMMAND_CQRS_IMPLEMENTATION.md`
  - `docs/CONTINUATION_GUIDE.md`
  - `ARCHITECTURE_README.md`

- **Padrões Arquitetônicos:**
  - CQRS Pattern (Greg Young)
  - Dependency Injection Principle
  - Composition Root Pattern

## Status
- **Estado:** Em Progresso
- **Responsável:** AI Agent
- **Prioridade:** Alta - Bloqueia todos os testes e cobertura
- **Data Limite:** 2025-12-03 (hoje)
