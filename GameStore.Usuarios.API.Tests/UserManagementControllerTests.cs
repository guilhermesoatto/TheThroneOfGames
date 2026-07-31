using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace GameStore.Usuarios.API.Tests;

/// <summary>
/// Testa GameStore.Usuarios.API/Controllers/Admin/UserManagementController.cs — endpoint que
/// fechou o gap "Administrador... administra usuários" (Fase 1). Ver docs/ai/tasks/
/// fix-admin-user-management-gap.md para o achado original.
/// </summary>
[Trait("Category", "Integration")]
public class UserManagementControllerTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client;

    public UserManagementControllerTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return result!["token"];
    }

    /// <summary>Registra, ativa e loga um usuário comum novo — usado pra testar 403 e como alvo
    /// dos endpoints de disable/enable/change-role sem mexer no único Admin da fixture.</summary>
    private async Task<(Guid Id, string Email, string Token)> CreateAndLoginRegularUserAsync()
    {
        var email = $"user.{Guid.NewGuid():N}@test.com";
        var preRegister = await _client.PostAsJsonAsync("/api/usuario/pre-register", new UserDTO
        {
            Name = "Regular User",
            Email = email,
            Password = "User@12345",
            Role = "User"
        });
        preRegister.EnsureSuccessStatusCode();
        var preRegisterBody = await preRegister.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var activationToken = preRegisterBody!["activationToken"];

        var activate = await _client.PostAsync($"/api/usuario/activate?activationToken={activationToken}", null);
        activate.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO { Email = email, Password = "User@12345" });
        var loginBody = await login.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        var getAll = await _client.GetAsync("/api/admin/user-management");
        var users = await getAll.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var id = Guid.Parse(users!.First(u => u["email"].ToString() == email)["id"].ToString()!);

        return (id, email, loginBody!["token"]);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/admin/user-management");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithRegularUserToken_ReturnsForbidden()
    {
        var (_, _, userToken) = await CreateAndLoginRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await _client.GetAsync("/api/admin/user-management");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithAdminToken_ReturnsOk()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        var response = await _client.GetAsync("/api/admin/user-management");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistentUser_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        var response = await _client.GetAsync($"/api/admin/user-management/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_NewUser_ReturnsCreatedAndUserCanLogInAfterEnable()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
        var email = $"created.{Guid.NewGuid():N}@test.com";

        var createResponse = await _client.PostAsJsonAsync("/api/admin/user-management", new UserDTO
        {
            Name = "Created By Admin",
            Email = email,
            Password = "Created@123",
            Role = "User"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = Guid.Parse(created!["id"].ToString()!);
        Assert.False(((System.Text.Json.JsonElement)created["isActive"]).GetBoolean());

        // Usuário criado pelo admin nasce inativo (RN-005) — sem enable, o login deve falhar.
        var loginBeforeEnable = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO { Email = email, Password = "Created@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginBeforeEnable.StatusCode);

        var enableResponse = await _client.PostAsync($"/api/admin/user-management/{id}/enable", null);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var loginAfterEnable = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO { Email = email, Password = "Created@123" });
        Assert.Equal(HttpStatusCode.OK, loginAfterEnable.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        var response = await _client.PostAsJsonAsync("/api/admin/user-management", new UserDTO
        {
            Name = "Duplicate Admin",
            Email = "admin@test.com", // já existe (seed da fixture)
            Password = "Whatever@123",
            Role = "User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_ExistingUser_UpdatesRole()
    {
        var (id, _, _) = await CreateAndLoginRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        var response = await _client.PatchAsync($"/api/admin/user-management/{id}/role",
            JsonContent.Create(new { NewRole = "Admin" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/admin/user-management/{id}");
        var user = await getResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("Admin", user!["role"].ToString());
    }

    [Fact]
    public async Task Disable_OwnAccount_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        // "sub" do token do admin da fixture — GetAll é a forma mais simples de descobrir o Id
        // sem duplicar lógica de parsing de JWT no teste.
        var getAll = await _client.GetAsync("/api/admin/user-management");
        var users = await getAll.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        var adminId = Guid.Parse(users!.First(u => u["email"].ToString() == "admin@test.com")["id"].ToString()!);

        var response = await _client.PostAsync($"/api/admin/user-management/{adminId}/disable", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Disable_ThenEnable_OtherUser_RoundTripsCorrectly()
    {
        var (id, email, _) = await CreateAndLoginRegularUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        var disableResponse = await _client.PostAsync($"/api/admin/user-management/{id}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        var loginWhileDisabled = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO { Email = email, Password = "User@12345" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginWhileDisabled.StatusCode);

        var enableResponse = await _client.PostAsync($"/api/admin/user-management/{id}/enable", null);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var loginAfterEnable = await _client.PostAsJsonAsync("/api/usuario/login", new LoginDTO { Email = email, Password = "User@12345" });
        Assert.Equal(HttpStatusCode.OK, loginAfterEnable.StatusCode);
    }
}
