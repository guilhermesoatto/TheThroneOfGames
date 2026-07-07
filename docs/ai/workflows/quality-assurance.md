# [WORKFLOW: QUALITY ASSURANCE & TESTING VERIFICATION]

**Gatilho:** Este workflow DEVE ser executado implicitamente após a criação de qualquer nova regra de negócio, refatoração de Use Case, criação de Adapters ou quando o usuário solicitar "teste isso" ou "verifique a qualidade".

## FASE 1: TESTES DE DOMÍNIO (Unitários e Puros)
- **Alvo:** Arquivos na pasta `/domain`.
- **Regra de Ouro:** ZERO MOCKS. Como o domínio é puro e não tem dependências externas, instancie as Entidades e Value Objects diretamente.
- **O que testar:** 1. Invariantes de negócio (ex: criar um objeto com estado inválido DEVE estourar um `DomainError`).
  2. Transições de estado (ex: chamar `order.pay()` deve alterar o status para 'PAID' e gerar um `DomainEvent`).
- **Padrão:** Utilize o padrão AAA (Arrange, Act, Assert).

## FASE 2: TESTES DE APLICAÇÃO (Use Cases)
- **Alvo:** Arquivos na pasta `/application`.
- **Estratégia de Mocks:** Aqui você DEVE mockar as Portas (Interfaces de repositórios, serviços externos e mensageria). NUNCA conecte a um banco de dados real nesta fase.
- **O que testar:**
  1. Fluxo de Sucesso (Happy Path).
  2. Fluxos de Exceção (ex: o que o Use Case retorna se o repositório não encontrar o ID?).
  3. Interações (verifique se o método `repository.save()` foi chamado exatamente 1 vez com os parâmetros corretos).

## FASE 3: TESTES DE INTEGRAÇÃO (Adapters & Infraestrutura)
- **Alvo:** Arquivos nas pastas `/infrastructure` e `/presentation`.
- **Estratégia:** SEM MOCKS PARA I/O. Se estiver testando um `PostgresUserRepository`, o teste deve rodar contra um banco de dados real (via Testcontainers ou banco em memória equivalente). Se for testar um Controller, suba a aplicação e faça chamadas HTTP simuladas (ex: Supertest).
- **Verificação de Contrato:** Garanta que o Adapter respeita exatamente a Interface (Port) definida no Use Case.

## FASE 4: AUDITORIA FINAL DO AGENTE (Self-Check)
Antes de confirmar ao usuário que o código está pronto, o agente deve auditar internamente:
- [ ] O tratamento de erros (exceções) foi testado?
- [ ] O `correlation_id` foi repassado e testado nos Adapters de I/O?
- [ ] A cobertura de testes do Core Domain (`/domain`) está em 100% dos caminhos lógicos?
- [ ] O código introduziu alguma vulnerabilidade de segurança (CVE) nas dependências de teste?

Se qualquer um desses itens falhar, o agente deve corrigir o código e os testes ANTES de avisar o usuário.
