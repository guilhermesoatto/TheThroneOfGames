# 🧪 Estratégia de Testes - TheThroneOfGames

## 📋 Visão Geral

Este documento define a estratégia de testes para o projeto **TheThroneOfGames**, com objetivo de manter **100% de cobertura de testes** em todas as camadas da arquitetura. Os testes devem acompanhar qualquer mudança na arquitetura para validar o funcionamento do sistema em tempo real.

---

## 🎯 Objetivo

- ✅ Garantir 100% de cobertura de código
- ✅ Validar funcionamento em cada refatoração arquitetônica
- ✅ Detectar regressões rapidamente
- ✅ Documentar comportamento esperado via testes
- ✅ Facilitar manutenção e evolução do sistema

---

## 📊 Estado Atual da Cobertura

### Análise Quantitativa

| Camada | Testes Existentes | Cobertura | Status |
|--------|------------------|-----------|--------|
| **API Controllers** | 8 | ✅ 80% | Parcial |
| **Application Services** | 2 | ⚠️ 15% | Mínimo |
| **Domain Entities** | 0 | ❌ 0% | Nenhum |
| **Infrastructure/Repository** | 0 | ❌ 0% | Nenhum |
| **Business Logic (CQRS)** | 24 | ✅ 70% | Bom |
| **Messaging (RabbitMQ)** | 2 | ✅ 60% | Bom |
| **Security (Auth/JWT)** | 3 | ✅ 85% | Bom |
| **Resilience Policies** | 9 | ✅ 100% | Completo |
| **Mappers/DTOs** | 5 | ✅ 75% | Bom |

**Cobertura Geral: ~35-40%**

---

## 🏗️ Estrutura de Testes por Camada

### 1️⃣ **Domain Layer Tests** (CRÍTICO - 0% → 100%)

#### Localização
```
TheThroneOfGames.Domain.Tests/
├── Entities/
│   ├── UserEntityTests.cs
│   ├── GameEntityTests.cs
│   ├── PurchaseEntityTests.cs
│   └── PromotionEntityTests.cs
├── ValueObjects/
│   ├── MoneyTests.cs
│   └── PriceTests.cs
├── Services/
│   ├── UsuarioDomainTests.cs
│   ├── GamesDomainTests.cs
│   ├── PurchaseDomainTests.cs
│   └── PromotionDomainTests.cs
└── Repositories/
    ├── IUserRepositoryTests.cs
    ├── IGameRepositoryTests.cs
    ├── IPurchaseRepositoryTests.cs
    └── IPromotionRepositoryTests.cs
```

#### Tipos de Testes

##### A. Entity Validation Tests
```csharp
[TestClass]
public class UserEntityTests
{
    [TestMethod]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var name = "Test User";
        
        // Act
        var user = new UserEntity 
        { 
            Id = userId, 
            Email = email, 
            Name = name 
        };
        
        // Assert
        Assert.AreEqual(userId, user.Id);
        Assert.AreEqual(email, user.Email);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        // Arrange & Act
        var user = new UserEntity { Email = "invalid-email" };
    }
}
```

##### B. Business Rule Tests
```csharp
[TestClass]
public class UsuarioDomainTests
{
    [TestMethod]
    public void Activate_ShouldSetIsActive()
    {
        // Arrange
        var usuario = new UsuarioDomain();
        
        // Act
        usuario.Activate();
        
        // Assert
        Assert.IsTrue(usuario.IsActive);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void IsValidToRegister_WithInvalidPassword_ShouldThrow()
    {
        // Arrange
        var usuario = new UsuarioDomain 
        { 
            Password = "weak" // < 8 chars
        };
        
        // Act & Assert
        usuario.IsValidToRegister();
    }
}
```

---

### 2️⃣ **Application Layer Tests** (CRÍTICO - 15% → 100%)

#### Localização
```
TheThroneOfGames.Application.Tests/
├── Services/
│   ├── UsuarioServiceTests.cs (expandir)
│   ├── GameServiceTests.cs
│   ├── PromotionServiceTests.cs
│   └── PurchaseServiceTests.cs
├── Commands/
│   ├── CreateUserCommandHandlerTests.cs
│   ├── UpdateGameCommandHandlerTests.cs
│   └── CreatePromotionCommandHandlerTests.cs
├── Queries/
│   ├── GetUserQueryHandlerTests.cs
│   ├── ListGamesQueryHandlerTests.cs
│   └── GetPromotionQueryHandlerTests.cs
└── Validation/
    ├── UserValidatorTests.cs
    ├── GameValidatorTests.cs
    └── PromotionValidatorTests.cs
```

#### Tipos de Testes

##### A. Service Tests
```csharp
[TestClass]
public class GameServiceTests
{
    private Mock<IBaseRepository<GameEntity>> _repositoryMock;
    private GameService _gameService;
    
    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IBaseRepository<GameEntity>>();
        _gameService = new GameService(_repositoryMock.Object);
    }
    
    [TestMethod]
    public async Task AddAsync_WithValidGame_ShouldCallRepository()
    {
        // Arrange
        var game = new GameEntity { Id = Guid.NewGuid(), Name = "Game 1" };
        
        // Act
        await _gameService.AddAsync(game);
        
        // Assert
        _repositoryMock.Verify(r => r.AddAsync(game), Times.Once);
    }
    
    [TestMethod]
    public async Task GetAllAsync_ShouldReturnAllGames()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new GameEntity { Id = Guid.NewGuid(), Name = "Game 1" },
            new GameEntity { Id = Guid.NewGuid(), Name = "Game 2" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(games);
        
        // Act
        var result = await _gameService.GetAllAsync();
        
        // Assert
        Assert.AreEqual(2, result.Count());
    }
}
```

##### B. CQRS Handler Tests
```csharp
[TestClass]
public class CreateGameCommandHandlerTests
{
    private Mock<IBaseRepository<GameEntity>> _repositoryMock;
    private CreateGameCommandHandler _handler;
    
    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IBaseRepository<GameEntity>>();
        _handler = new CreateGameCommandHandler(_repositoryMock.Object);
    }
    
    [TestMethod]
    public async Task Handle_WithValidCommand_ShouldCreateGame()
    {
        // Arrange
        var command = new CreateGameCommand 
        { 
            Name = "New Game", 
            Genre = "Action",
            Price = 29.99m 
        };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(result);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<GameEntity>()), Times.Once);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task Handle_WithInvalidCommand_ShouldThrow()
    {
        // Arrange
        var command = new CreateGameCommand { Name = null };
        
        // Act & Assert
        await _handler.Handle(command, CancellationToken.None);
    }
}
```

---

### 3️⃣ **Infrastructure Layer Tests** (CRÍTICO - 0% → 100%)

#### Localização
```
TheThroneOfGames.Infrastructure.Tests/
├── Persistence/
│   ├── AppDbContextTests.cs
│   ├── MainDbContextTests.cs
│   └── MongoDbContextTests.cs
├── Repository/
│   ├── BaseRepositoryTests.cs
│   ├── UserRepositoryTests.cs
│   ├── GameRepositoryTests.cs
│   ├── PurchaseRepositoryTests.cs
│   └── PromotionRepositoryTests.cs
├── Messaging/
│   ├── RabbitMqAdapterTests.cs (expandir)
│   └── EventPublisherTests.cs
└── Data/
    └── Configurations/
        ├── UserConfigurationTests.cs
        ├── GameConfigurationTests.cs
        └── PromotionConfigurationTests.cs
```

#### Tipos de Testes

##### A. Repository Tests (Integration)
```csharp
[TestClass]
public class GameRepositoryTests
{
    private DbContextOptions<AppDbContext> _options;
    private AppDbContext _context;
    private GameEntityRepository _repository;
    
    [TestInitialize]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
            .Options;
        _context = new AppDbContext(_options);
        _repository = new GameEntityRepository(_context);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }
    
    [TestMethod]
    public async Task Add_ShouldPersistGame()
    {
        // Arrange
        var game = new GameEntity 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Game",
            Price = 29.99m 
        };
        
        // Act
        await _repository.AddAsync(game);
        
        // Assert
        var retrieved = await _repository.GetByIdAsync(game.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(game.Name, retrieved.Name);
    }
    
    [TestMethod]
    public async Task GetAll_ShouldReturnAllGames()
    {
        // Arrange
        var games = new List<GameEntity>
        {
            new GameEntity { Id = Guid.NewGuid(), Name = "Game 1" },
            new GameEntity { Id = Guid.NewGuid(), Name = "Game 2" }
        };
        foreach (var game in games)
        {
            await _repository.AddAsync(game);
        }
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.AreEqual(2, result.Count());
    }
}
```

##### B. DbContext Tests
```csharp
[TestClass]
public class AppDbContextTests
{
    [TestMethod]
    public void OnModelCreating_ShouldConfigureAllEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        
        // Act
        using (var context = new AppDbContext(options))
        {
            // Assert
            var model = context.Model;
            Assert.IsNotNull(model.FindEntityType(typeof(UserEntity)));
            Assert.IsNotNull(model.FindEntityType(typeof(GameEntity)));
            Assert.IsNotNull(model.FindEntityType(typeof(PurchaseEntity)));
            Assert.IsNotNull(model.FindEntityType(typeof(PromotionEntity)));
        }
    }
}
```

---

### 4️⃣ **API Layer Tests** (Bום - 80% → 100%)

#### Localização
```
Test/Integration/ (expandir)
├── GameControllerTests.cs
├── PromotionControllerTests.cs
├── PurchaseControllerTests.cs
├── AdminControllerTests.cs
├── AuthControllerTests.cs
└── ErrorHandlingTests.cs
```

#### Tipos de Testes

##### A. Controller Integration Tests
```csharp
[TestClass]
public class GameControllerTests
{
    private CustomWebApplicationFactory<Program> _factory;
    private HttpClient _client;
    
    [TestInitialize]
    public void Setup()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
    
    [TestMethod]
    public async Task GetAll_ShouldReturnOkWithGames()
    {
        // Act
        var response = await _client.GetAsync("/api/games");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("\"id\""));
    }
    
    [TestMethod]
    public async Task GetById_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/api/games/{gameId}");
        
        // Assert
        Assert.IsTrue(response.StatusCode == HttpStatusCode.OK || 
                     response.StatusCode == HttpStatusCode.NotFound);
    }
}
```

---

### 5️⃣ **Security & Authorization Tests** (BOUN - 85% → 100%)

#### Localização
```
Test/Security/
├── JwtTokenTests.cs (expandir)
├── AuthenticationTests.cs (expandir)
├── AuthorizationTests.cs (expandir)
├── PasswordHashingTests.cs
└── RolePermissionTests.cs
```

#### Tipos de Testes

##### A. JWT Token Tests
```csharp
[TestClass]
public class JwtTokenTests
{
    [TestMethod]
    public void GenerateToken_ShouldCreateValidToken()
    {
        // Arrange
        var user = new UserEntity 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        
        // Act
        var token = JwtTokenGenerator.Generate(user);
        
        // Assert
        Assert.IsNotNull(token);
        Assert.IsTrue(token.Length > 0);
    }
    
    [TestMethod]
    public void ValidateToken_WithValidToken_ShouldSucceed()
    {
        // Arrange
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "test@example.com" };
        var token = JwtTokenGenerator.Generate(user);
        
        // Act
        var result = JwtTokenValidator.Validate(token);
        
        // Assert
        Assert.IsTrue(result.IsValid);
    }
}
```

---

### 6️⃣ **Resilience & Performance Tests** (COMPLETO - 100%)

#### Localização
```
Test/Application/Policies/
└── ResiliencePoliciesTests.cs (manter & expandir)
```

Já existe cobertura completa. Adicionar:
- Testes de carga
- Testes de timeout
- Circuit breaker tests

---

## 📈 Plano de Implementação

### Fase 1: Foundation (Semanas 1-2)
- [ ] Setup dos testes de Domain
- [ ] Setup dos testes de Infrastructure (Repository)
- [ ] Criar base de testes com Mocks e Fixtures
- [ ] Configurar cobertura de código (OpenCover/Coverlet)

**Target: 50% cobertura**

### Fase 2: Core Services (Semanas 3-4)
- [ ] Completar Application Services tests
- [ ] Expandir CQRS handlers tests
- [ ] Adicionar Validation tests
- [ ] Testes de Mapeamento (Mappers)

**Target: 70% cobertura**

### Fase 3: Integration & Security (Semanas 5-6)
- [ ] Expandir API Controller tests
- [ ] Adicionar Security & Authorization tests
- [ ] Testes de Messaging (RabbitMQ)
- [ ] Testes de Database (DbContext)

**Target: 85% cobertura**

### Fase 4: Edge Cases & Performance (Semanas 7-8)
- [ ] Testes de exceções e error handling
- [ ] Testes de concorrência
- [ ] Testes de carga (performance)
- [ ] Testes de regressão

**Target: 100% cobertura**

---

## 🛠️ Ferramentas & Configuração

### NuGet Packages Necessários
```xml
<!-- Testes -->
<PackageReference Include="Microsoft.VisualStudio.TestTools.UnitTesting" Version="2.2.10" />

<!-- Mocking -->
<PackageReference Include="Moq" Version="4.20.69" />

<!-- In-Memory Testing -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />

<!-- Cobertura -->
<PackageReference Include="OpenCover" Version="4.7.1221" />
<PackageReference Include="ReportGenerator" Version="5.2.0" />

<!-- Assertions -->
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

### Configuração de Cobertura (runsettings)
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage" assemblyQualifiedName="Coverlet.Collector.DataCollection.CoverletInstrumentationProvider, coverlet.collector">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[*.Tests]*,[*]Tests.*</Exclude>
          <MinimumCoveragePercentage>100</MinimumCoveragePercentage>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

---

## 🔄 Processo de Refatoração com Testes

### Procedimento Padrão

1. **Escrita de Teste Primeiro (TDD)**
   ```
   1. Escrever teste para novo comportamento
   2. Teste falha (Red)
   3. Implementar funcionalidade
   4. Teste passa (Green)
   5. Refatorar código
   6. Validar cobertura
   ```

2. **Refatoração Arquitetônica**
   ```
   1. Executar suite completa de testes
   2. Fazer mudança na arquitetura
   3. Corrigir testes se necessário
   4. Validar que todos passam
   5. Verificar cobertura não diminuiu
   6. Commit com testes passando
   ```

3. **Validação Contínua**
   - Cada PR deve ter +100% testes passando
   - Coverage não deve diminuir
   - All green tests required before merge

---

## 📊 Métricas & Monitoramento

### KPIs
- ✅ Code Coverage: 100%
- ✅ Test Pass Rate: 100%
- ✅ Build Time: < 5 minutos
- ✅ Test Execution Time: < 2 minutos

### Relatórios
- Executar cobertura: `dotnet test /p:CollectCoverage=true`
- Gerar relatório: `reportgenerator -reports:coverage.xml -targetdir:coverage`
- Dashboard CI/CD: GitHub Actions + Badge

---

## 🚀 Comandos Úteis

### Executar Testes
```bash
# Todos os testes
dotnet test

# Com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Testes específicos
dotnet test --filter "TestCategory=Unit"

# Com resultado detalhado
dotnet test -v d
```

### Validação de Cobertura
```bash
# Gerar relatório HTML
reportgenerator -reports:"**/coverage.xml" -targetdir:./coverage

# Validar mínimo de cobertura
dotnet test /p:CoverletOutputFormat=cobertura /p:Threshold=100
```

---

## 📝 Template de Teste

### Estrutura Padrão
```csharp
[TestClass]
public class [Feature]Tests
{
    // Arrange
    private Mock<IDependency> _dependencyMock;
    private [ServiceUnderTest] _sut; // System Under Test
    
    [TestInitialize]
    public void Setup()
    {
        _dependencyMock = new Mock<IDependency>();
        _sut = new [ServiceUnderTest](_dependencyMock.Object);
    }
    
    [TestMethod]
    public void [Method]_[Scenario]_[ExpectedResult]()
    {
        // Arrange
        var input = PrepareInput();
        
        // Act
        var result = _sut.Method(input);
        
        // Assert
        Assert.IsNotNull(result);
        _dependencyMock.Verify(d => d.Call(It.IsAny<object>()), Times.Once);
    }
}
```

---

## ✅ Checklist de Testes para Refatoração

Antes de commitar qualquer mudança arquitetônica:

- [ ] Todos os testes unitários passam
- [ ] Todos os testes de integração passam
- [ ] Coverage em 100%
- [ ] Sem warnings no build
- [ ] Performance dentro dos limites
- [ ] Security tests passam
- [ ] Code review aprovado
- [ ] CI/CD pipeline completo

---

## 📚 Referências

- [Microsoft Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [Moq Documentation](https://github.com/moq/moq4)
- [xUnit vs MSTest vs NUnit](https://stackoverflow.com/questions/6716644/)
- [TDD Best Practices](https://martinfowler.com/bliki/TestDrivenDevelopment.html)

---

**Última Atualização:** 2025-12-08  
**Status:** 🟡 Em Implementação (40% → 100%)  
**Próximo Sprint:** Implementar Domain Layer Tests
