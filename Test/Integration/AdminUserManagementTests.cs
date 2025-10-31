using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheThroneOfGames.API.Models.DTO;

namespace Test.Integration;

[TestFixture]
public class AdminUserManagementTests : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _outboxPath;
    private Guid _testUserId;

    public AdminUserManagementTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Set up the outbox path (same as EmailService)
        _outboxPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Infrastructure", "Outbox"));
        if (!Directory.Exists(_outboxPath))
        {
            Directory.CreateDirectory(_outboxPath);
        }
    }

    [SetUp]
    public void SetUp()
    {
        // Clean up any existing test emails before each test
        foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
        {
            File.Delete(file);
        }
    }

    private async Task<string> GetAdminToken()
    {
        var response = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = "admin@test.com",
            Password = "Admin@123!"
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Admin login failed");

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(result);
        Assert.That(result.ContainsKey("token"), "Token not found in response");

        return result["token"];
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Guid> CreateTestUser()
    {
        var user = new UserDTO
        {
            Name = "Test User",
            Email = "testuser@example.com",
            Password = "Test@123!",
            Role = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Wait for email file
        await Task.Delay(100);

        // Get activation token from email
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1));
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);

        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        // Activate account
        var activateResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);
        Assert.That(activateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Get user ID from list
        var adminToken = await GetAdminToken();
        SetAuthToken(adminToken);
        var usersResponse = await _client.GetFromJsonAsync<List<UserListDTO>>("/api/admin/user-management");
        Assert.IsNotNull(usersResponse);
        var user2 = usersResponse.FirstOrDefault(u => u.Email == "testuser@example.com");
        Assert.IsNotNull(user2);

        return user2.Id;
    }

    [Test]
    public async Task AdminCanManageUserRoles()
    {
        // Arrange
        var token = await GetAdminToken();
        SetAuthToken(token);
        _testUserId = await CreateTestUser();

        // Act - Update role to Admin
        var updateRoleResponse = await _client.PutAsJsonAsync($"/api/admin/user-management/{_testUserId}/role", new UserRoleUpdateDTO
        {
            Role = "Admin"
        });

        // Assert
        Assert.That(updateRoleResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify role was updated
        var userResponse = await _client.GetFromJsonAsync<UserListDTO>($"/api/admin/user-management/{_testUserId}");
        Assert.IsNotNull(userResponse);
        Assert.That(userResponse.Role, Is.EqualTo("Admin"));

        // Change role back to User
        updateRoleResponse = await _client.PutAsJsonAsync($"/api/admin/user-management/{_testUserId}/role", new UserRoleUpdateDTO
        {
            Role = "User"
        });

        Assert.That(updateRoleResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        userResponse = await _client.GetFromJsonAsync<UserListDTO>($"/api/admin/user-management/{_testUserId}");
        Assert.IsNotNull(userResponse);
        Assert.That(userResponse.Role, Is.EqualTo("User"));
    }

    [Test]
    public async Task AdminCanDisableAndEnableUsers()
    {
        // Arrange
        var token = await GetAdminToken();
        SetAuthToken(token);
        _testUserId = await CreateTestUser();

        // Act - Disable user
        var disableResponse = await _client.PostAsync($"/api/admin/user-management/{_testUserId}/disable", null);

        // Assert
        Assert.That(disableResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify user is disabled
        var userResponse = await _client.GetFromJsonAsync<UserListDTO>($"/api/admin/user-management/{_testUserId}");
        Assert.IsNotNull(userResponse);
        Assert.That(userResponse.IsActive, Is.False);

        // Re-enable user
        var enableResponse = await _client.PostAsync($"/api/admin/user-management/{_testUserId}/enable", null);

        Assert.That(enableResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        userResponse = await _client.GetFromJsonAsync<UserListDTO>($"/api/admin/user-management/{_testUserId}");
        Assert.IsNotNull(userResponse);
        Assert.That(userResponse.IsActive, Is.True);
    }

    [Test]
    public async Task NonAdminCannotAccessUserManagement()
    {
        // Arrange - Create and login as regular user
        var user = new UserDTO
        {
            Name = "Regular User",
            Email = "regularuser@example.com",
            Password = "User@123!",
            Role = "User"
        };

        var preRegisterResponse = await _client.PostAsJsonAsync("/api/Usuario/pre-register", user);
        Assert.That(preRegisterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Need to wait for the email file to be written
        await Task.Delay(100);

        // Get activation token from email
        var emailFiles = Directory.GetFiles(_outboxPath, "*.eml");
        Assert.That(emailFiles, Has.Length.EqualTo(1));
        var emailContent = await File.ReadAllTextAsync(emailFiles[0]);

        var activationTokenStart = emailContent.IndexOf("activationToken=") + "activationToken=".Length;
        var activationTokenEnd = emailContent.IndexOf("\n", activationTokenStart);
        var activationToken = emailContent.Substring(activationTokenStart, activationTokenEnd - activationTokenStart).Trim();

        // Activate the account
        var activateResponse = await _client.PostAsync($"/api/Usuario/activate?activationToken={activationToken}", null);
        Assert.That(activateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginResponse = await _client.PostAsJsonAsync("/api/Usuario/login", new LoginDTO
        {
            Email = user.Email,
            Password = user.Password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.IsNotNull(result);
        SetAuthToken(result["token"]);

        // Act & Assert - Try to access admin endpoints
        var getResponse = await _client.GetAsync("/api/admin/user-management");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var updateRoleResponse = await _client.PutAsJsonAsync($"/api/admin/user-management/{Guid.NewGuid()}/role", new UserRoleUpdateDTO
        {
            Role = "Admin"
        });
        Assert.That(updateRoleResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var disableResponse = await _client.PostAsync($"/api/admin/user-management/{Guid.NewGuid()}/disable", null);
        Assert.That(disableResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    public void Dispose()
    {
        // Clean up email files
        if (Directory.Exists(_outboxPath))
        {
            foreach (var file in Directory.GetFiles(_outboxPath, "*.eml"))
            {
                File.Delete(file);
            }
        }

        _client.Dispose();
        _factory.Dispose();
    }
}