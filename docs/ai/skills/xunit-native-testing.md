# [SKILL: xUnit + FluentAssertions TESTING]

**Descrição:** Este documento define a sintaxe e as ferramentas estritas para a criação de testes automatizados. A regra de ouro é: dependências de teste padronizadas e minimais. Como um agente de IA, você DEVE usar exclusivamente xUnit como test runner, FluentAssertions para asserções fluentes, e NSubstitute para mocking.

## 1. PACKAGES OBRIGATÓRIOS E ASSERÇÕES
- **Test Runner:** Utilize estritamente xUnit (`xunit` + `xunit.runner.visualstudio`). Nunca use MSTest ou frameworks de teste não padronizados.
- **Asserções:** Utilize SEMPRE FluentAssertions: `using FluentAssertions;`. A sintaxe fluente `.Should().Be()` é obrigatória.
- **Mocking:** Utilize NSubstitute (`NSubstitute`) para mocks na camada de Application. Nunca use Moq ou FakeItEasy no Core.
- **Proibições:** Não use `Assert.Equal()` do xUnit diretamente — prefira FluentAssertions para consistência e mensagens de erro ricas. Não use `[Theory]` sem `[InlineData]` ou `[MemberData]`.

### Pacotes NuGet Padrão para Projetos de Teste:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  <PackageReference Include="FluentAssertions" Version="7.*" />
  <PackageReference Include="NSubstitute" Version="5.*" />
  <PackageReference Include="NSubstitute.Analyzers.CSharp" Version="1.*" />
</ItemGroup>
```

### Pacotes Adicionais para Testes de Integração:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  <PackageReference Include="Testcontainers" Version="4.*" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
  <PackageReference Include="Testcontainers.Redis" Version="4.*" />
</ItemGroup>
```

## 2. SINTAXE DE ASSERÇÃO (FluentAssertions)
O agente deve mapear mentalmente a intenção do teste para a sintaxe FluentAssertions:
- **Igualdade de Valor:** `actual.Should().Be(expected);` (para primitivos).
- **Igualdade Profunda:** `actual.Should().BeEquivalentTo(expected);` (para objetos, DTOs e coleções).
- **Verificação de Exceções (Domain Errors via Result):**
  Para garantir que um Value Object retorne um `Result<T>` com erro de domínio:
  ```csharp
  [Fact]
  public void Email_Create_WithInvalidEmail_ShouldReturnFailure()
  {
      // Arrange & Act
      var result = Email.Create("invalid-email");

      // Assert
      result.IsFailure.Should().BeTrue();
      result.Error.Should().BeOfType<InvalidEmailError>();
      result.Error.Message.Should().Be("The provided email is invalid");
  }
  ```
- **Verificação de Exceções (quando aplicável em Adapters):**
  ```csharp
  var act = () => someAdapter.DoSomething();
  act.Should().ThrowExactly<ArgumentNullException>()
      .WithMessage("*parameter*");
  ```
- **Verificação de Tipo:**
  ```csharp
  result.Value.Should().BeOfType<User>();
  ```
- **Verificação de Coleções:**
  ```csharp
  users.Should().HaveCount(3);
  users.Should().ContainSingle(u => u.Email == "test@domain.com");
  ```
- **Verificação de Nulidade:**
  ```csharp
  result.Should().NotBeNull();
  result.Value.Should().NotBeNull();
  ```
- **Verificação de Mock (NSubstitute):**
  ```csharp
  await repository.Received(1).SaveAsync(Arg.Is<User>(u => u.Id == userId), Arg.Any<CancellationToken>());
  await publisher.DidNotReceive().PublishAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>());
  ```

## 3. ESTRUTURA DE TESTE (Arrange-Act-Assert)
Todo teste DEVE seguir o padrão AAA com comentários explícitos:
```csharp
[Fact]
public async Task CreateUserUseCase_WithValidInput_ShouldReturnUser()
{
    // Arrange
    var repository = Substitute.For<IUserRepository>();
    var useCase = new CreateUserUseCase(repository);
    var input = new CreateUserInput("John", "john@example.com");

    // Act
    var result = await useCase.ExecuteAsync(input, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Name.Should().Be("John");
    await repository.Received(1).SaveAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
}
```

## 4. TESTES DE INTEGRAÇÃO (WebApplicationFactory + Testcontainers)
Para testes de integração de Controllers e Adapters:
```csharp
public class UserControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UserControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_CreateUser_ReturnsCreated()
    {
        // Arrange
        var payload = new { Name = "John", Email = "john@test.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### CustomWebApplicationFactory com Testcontainers:
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o DbContext registrado e substitui pelo Testcontainer
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
