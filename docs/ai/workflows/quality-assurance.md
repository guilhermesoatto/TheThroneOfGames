# [WORKFLOW: QUALITY ASSURANCE & TESTING VERIFICATION]

**Gatilho:** Este workflow DEVE ser executado implicitamente após a criação de qualquer nova regra de negócio, refatoração de Use Case, criação de Adapters ou quando o usuário solicitar "teste isso" ou "verifique a qualidade".

## FASE 1: TESTES DE DOMÍNIO (Unitários e Puros)
- **Alvo:** Classes no projeto `/Domain`.
- **Framework:** xUnit + FluentAssertions (conforme `xunit-native-testing.md`).
- **Regra de Ouro:** ZERO MOCKS. Como o domínio é puro e não tem dependências externas, instancie as Entidades e Value Objects diretamente.
- **O que testar:**
  1. Invariantes de negócio (ex: criar um Value Object com estado inválido DEVE retornar `Result<T>.Failure` com um `DomainError`).
  2. Transições de estado (ex: chamar `order.Pay()` deve alterar o status para `OrderStatus.Paid` e gerar um `DomainEvent`).
- **Padrão:** Utilize o padrão AAA (Arrange, Act, Assert) com comentários explícitos.
- **Exemplo:**
  ```csharp
  [Fact]
  public void Email_Create_WithValidEmail_ShouldReturnSuccess()
  {
      // Arrange
      var emailAddress = "user@domain.com";

      // Act
      var result = Email.Create(emailAddress);

      // Assert
      result.IsSuccess.Should().BeTrue();
      result.Value.Address.Should().Be(emailAddress);
  }

  [Fact]
  public void Order_Pay_ShouldChangeStatusAndEmitEvent()
  {
      // Arrange
      var order = Order.Create("customer-1", 100.00m).Value;

      // Act
      order.Pay();

      // Assert
      order.Status.Should().Be(OrderStatus.Paid);
      order.DomainEvents.Should().ContainSingle()
          .Which.Should().BeOfType<OrderPaidEvent>();
  }
  ```

## FASE 2: TESTES DE APLICAÇÃO (Use Cases)
- **Alvo:** Classes no projeto `/Application`.
- **Framework:** xUnit + FluentAssertions + NSubstitute.
- **Estratégia de Mocks:** Aqui você DEVE mockar as Portas (Interfaces de repositórios, serviços externos e mensageria) via NSubstitute. NUNCA conecte a um banco de dados real nesta fase.
- **O que testar:**
  1. Fluxo de Sucesso (Happy Path).
  2. Fluxos de Exceção (ex: o que o Use Case retorna via `Result<T>` se o repositório não encontrar o ID?).
  3. Interações (verifique se o método `repository.SaveAsync()` foi chamado exatamente 1 vez com os parâmetros corretos via `Received(1)`).
- **Exemplo:**
  ```csharp
  [Fact]
  public async Task CreateUserUseCase_WhenEmailAlreadyExists_ShouldReturnFailure()
  {
      // Arrange
      var repository = Substitute.For<IUserRepository>();
      repository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
          .Returns(true);
      var useCase = new CreateUserUseCase(repository);

      // Act
      var result = await useCase.ExecuteAsync(
          new CreateUserInput("John", "existing@email.com"),
          CancellationToken.None);

      // Assert
      result.IsFailure.Should().BeTrue();
      result.Error.Should().BeOfType<EmailAlreadyExistsError>();
      await repository.DidNotReceive().SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
  }
  ```

## FASE 3: TESTES DE INTEGRAÇÃO (Adapters & Infraestrutura)
- **Alvo:** Classes nos projetos `/Infrastructure` e `/WebApi`.
- **Framework:** xUnit + FluentAssertions + Testcontainers.NET + WebApplicationFactory<Program>.
- **Estratégia:** SEM MOCKS PARA I/O. Se estiver testando um `EfCoreUserRepository`, o teste deve rodar contra um banco de dados real (via Testcontainers.NET com PostgreSQL/SQL Server). Se for testar um Controller, suba a aplicação com `WebApplicationFactory<Program>` e faça chamadas HTTP simuladas.
- **Verificação de Contrato:** Garanta que o Adapter respeita exatamente a Interface (Port) definida no Use Case.
- **Exemplo:**
  ```csharp
  public class EfCoreUserRepositoryTests : IClassFixture<PostgresFixture>
  {
      private readonly AppDbContext _context;

      public EfCoreUserRepositoryTests(PostgresFixture fixture)
      {
          _context = fixture.CreateDbContext();
      }

      [Fact]
      public async Task SaveAsync_ShouldPersistUser()
      {
          // Arrange
          var repository = new EfCoreUserRepository(_context);
          var user = User.Create("John", "john@test.com").Value;

          // Act
          await repository.SaveAsync(user, CancellationToken.None);

          // Assert
          var persisted = await _context.Users.FindAsync(user.Id);
          persisted.Should().NotBeNull();
          persisted!.Name.Should().Be("John");
      }
  }
  ```

## FASE 4: AUDITORIA FINAL DO AGENTE (Self-Check)
Antes de confirmar ao usuário que o código está pronto, o agente deve auditar internamente:
- [ ] O tratamento de erros (via `Result<T>`) foi testado para todos os caminhos de falha?
- [ ] O `CorrelationId` foi repassado e testado nos Adapters de I/O?
- [ ] A cobertura de testes do Core Domain (`/Domain`) está em 100% dos caminhos lógicos?
- [ ] O código introduziu alguma vulnerabilidade de segurança (CVE) nas dependências de teste? (Execute `dotnet list package --vulnerable`)
- [ ] Os testes ArchUnitNET de pureza de domínio continuam passando? (Execute `dotnet test --filter "FullyQualifiedName~DomainPurityTests"`)

Se qualquer um desses itens falhar, o agente deve corrigir o código e os testes ANTES de avisar o usuário.
