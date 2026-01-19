# Desenvolvimento: Migração para Bounded Contexts (pre-microservices)

Objetivo: transformar o monólito atual em três bounded contexts isolados (GameStore.Catalogo, GameStore.Usuarios, GameStore.Vendas) como passo preparatório para uma futura migração para microservices. Este arquivo funciona como um "prompt" de desenvolvimento — descreve o que precisa ser feito, as alterações propostas e contém a lista de tarefas atual para guiar o trabalho.

Instruções de uso para desenvolvedores e agentes automatizados:
- Leia este arquivo antes de fazer mudanças significativas. Ele contém a todo-list atual e os objetivos de cada passo.
- Ao modificar a solução ou namespaces, atualize a seção "Status da todo-list" abaixo.
- Mantemos pequenas commits por tarefa (um commit por task concluída) e não alteramos a solução (.sln) até que os csproj esqueleto sejam verificados.

Assunções razoáveis (se não especificado):
- Alvo: .NET 6/7 (compatível com o restante do projeto). Ajustar se outra versão for detectada.
- Não remover código existente sem antes garantir testes ou criar backups (mover em vez de deletar).
- Comunicação entre contexts inicialmente por eventos e DTOs; integração via API/gRPC ficará para fases posteriores.

Pequeno contrato para cada contexto (inputs/outputs):
- Input: classes/serviços relacionados ao domínio atual do monólito que pertencem ao contexto.
- Output: projeto independente com namespace apropriado, contratos (DTOs), handlers e infraestrutura mínima para persistência.
- Erros: falhas de build ou testes; revert flow com commits menores.

Edge cases a considerar:
- Entidades compartilhadas entre contexts (extrair um pacote comum ou copiar inicialmente e documentar divergência)
- Migrations/EF modelos que precisam ser adaptados
- Referências circulares entre projetos (evitar; usar eventos ou interfaces)

---

Status da todo-list (espelho; atualize quando concluir tarefas):

```
- [ ] Criar pastas e placeholders dos bounded contexts
  - Criar a estrutura de diretórios para GameStore.Catalogo, GameStore.Usuarios e GameStore.Vendas com subpastas Domain/Application/Infrastructure e READMEs/.gitkeep
- [ ] Adicionar arquivo de memória
  - Criar .github/instructions/memory.instruction.md com front matter para armazenar preferências
- [ ] Adicionar README de arquitetura
  - Criar um README explicando o objetivo, convenções e próximos passos para migração a microservices
- [ ] Criar esqueleto de projetos (.csproj)
  - Criar csproj mínimos para cada contexto (só esqueleto; não compilar ainda)
- [ ] Analisar e mapear código existente
  - Mapear onde estão as entidades/serviços atuais que pertencem a cada contexto para planejar migração
- [ ] Migrar código e atualizar solution
  - Mover/duplicar arquivos e adicionar projetos à solução; ajustar namespaces e referências
- [ ] Criar testes e garantir build
  - Adicionar testes unitários mínimos e rodar build/ testes para validar
- [ ] Documentar mudanças e próximos passos
  - Atualizar relatorio_entrega.txt e docs/ com o que foi feito e decisões arquiteturais
```

Nota: a todo-list acima deve ser mantida no gerenciador de tarefas (ex.: ISSUE tracker) além deste arquivo para rastreabilidade em CI/CD.

Tarefas imediatas sugeridas (primeiros commits):
1. Criar csproj esqueleto para cada contexto (`GameStore.Catalogo`, `GameStore.Usuarios`, `GameStore.Vendas`).
2. Adicionar os projetos à solution localmente e verificar que `dotnet build` passa (se problemas, reverter a adição à solution e tratar localmente).
3. Mover uma entidade pequena (ex: `Jogo`, `Usuario`, `Pedido`) para os projetos correspondentes como prova de conceito.
4. Criar testes unitários mínimos por contexto.

Comandos úteis (PowerShell) — opcionais para desenvolvedores:

```powershell
# criar csproj (exemplo)
cd c:\Users\Guilherme\source\repos\TheThroneOfGames
dotnet new classlib -n GameStore.Catalogo -o GameStore.Catalogo
dotnet new classlib -n GameStore.Usuarios -o GameStore.Usuarios
dotnet new classlib -n GameStore.Vendas -o GameStore.Vendas

# adicionar à solution (verificar nome da .sln)
dotnet sln TheThroneOfGames.sln add GameStore.Catalogo\GameStore.Catalogo.csproj ; dotnet sln TheThroneOfGames.sln add GameStore.Usuarios\GameStore.Usuarios.csproj ; dotnet sln TheThroneOfGames.sln add GameStore.Vendas\GameStore.Vendas.csproj

# buildar
dotnet build TheThroneOfGames.sln
```

Arquivos criados até agora (checklist mínimo):
- `GameStore.Catalogo/` (pastas e README)
- `GameStore.Usuarios/` (pastas e README)
- `GameStore.Vendas/` (pastas e README)
- `ARCHITECTURE_README.md`
- `.github/instructions/memory.instruction.md`
- `DEVELOPMENT_PROMPT.md` (este arquivo)

---

Notas finais e próximos passos imediatos:
- Atualize a seção "Status da todo-list" quando concluir cada tarefa. Mantenha pequenas alterações em commits separados.
- Próximo passo automatizado recomendado: gerar os csproj esqueleto e adicioná-los à solution, depois rodar `dotnet build` e `dotnet test`.

