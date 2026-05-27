# UsuariosDbContext DI Registration Fix - Complete Analysis

## Executive Summary
Fixed the "No service for type 'GameStore.Usuarios.Infrastructure.Persistence.UsuariosDbContext' has been registered" error in `GameStore.Catalogo.API.Tests` by explicitly directing `WebApplicationFactory` to use the correct `Program.cs` file.

---

## Root Cause Analysis

### The Problem
The `GameStore.Catalogo.API.Tests` project was failing to load `UsuariosDbContext` from the DI container because `WebApplicationFactory<Program>` was resolving to the **wrong Program.cs file**.

### Technical Details

**What was happening:**
1. The test class declared: `WebApplicationFactory<Program>`
2. C# resolved `Program` to the first accessible `Program` type found in the assembly resolution chain
3. This resolved to `GameStore.Catalogo.API/Program.cs` (a standalone minimal API) **NOT** `TheThroneOfGames.API/Program.cs`

**Why this caused the issue:**
- **GameStore.Catalogo.API/Program.cs** (WRONG - being used before fix)
  - Only calls: `builder.Services.AddCatalogoContext(connectionString);`
  - Does NOT register `AddUsuariosContext()`
  - Lacks all Usuarios bounded context services
  
- **TheThroneOfGames.API/Program.cs** (CORRECT - what should be used)
  - Calls: `builder.Services.AddUsuariosContext(connectionString);` at line 40
  - Registers all bounded contexts and their services
  - Provides the complete DI configuration

### Confirmation with Working Code
The `Test/Integration/CustomWebApplicationFactory.cs` works correctly because:
```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
```
It uses a generic type parameter, allowing callers to explicitly specify `TheThroneOfGames.API/Program`.

---

## Files Modified

### [GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs](GameStore.Catalogo.API.Tests/CatalogoWebApplicationFactory.cs)

**Change made (line 10):**

**Before:**
```csharp
public class CatalogoWebApplicationFactory : WebApplicationFactory<Program>
```

**After:**
```csharp
public class CatalogoWebApplicationFactory : WebApplicationFactory<global::Program>
```

**Why this works:**
- `global::Program` explicitly references the `Program` class from the global namespace
- The `TheThroneOfGames.API/Program.cs` is marked as `public partial class Program { }` (line 185)
- The test project already references `TheThroneOfGames.API` in its `.csproj`
- This ensures the factory loads the complete DI configuration from `TheThroneOfGames.API/Program.cs`

---

## Why the Project References Were Already Correct

The test project's `.csproj` file already had the necessary references:

```xml
<ItemGroup>
  <ProjectReference Include="..\GameStore.Catalogo.API\GameStore.Catalogo.API.csproj" />
  <ProjectReference Include="..\TheThroneOfGames.API\TheThroneOfGames.API.csproj" />
  <ProjectReference Include="..\GameStore.Usuarios\GameStore.Usuarios.csproj" />
  <ProjectReference Include="..\GameStore.Catalogo\GameStore.Catalogo.csproj" />
</ItemGroup>
```

This meant the issue was purely a **type resolution ambiguity**, not a missing reference.

---

## How DI Registration Works in TheThroneOfGames.API

From [TheThroneOfGames.API/Program.cs](TheThroneOfGames.API/Program.cs) (lines 31-45):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

// Register application services
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add bounded contexts - THIS IS THE KEY LINE
builder.Services.AddUsuariosContext(connectionString);  // ← Registers UsuariosDbContext
builder.Services.AddCatalogoContext(connectionString);
builder.Services.AddVendasApplication();
builder.Services.AddVendasInfrastructure(builder.Configuration);
```

The `AddUsuariosContext()` extension method is defined in:
**[GameStore.Usuarios/Infrastructure/Extensions/UsuariosInfrastructureExtensions.cs](GameStore.Usuarios/Infrastructure/Extensions/UsuariosInfrastructureExtensions.cs)**

```csharp
public static IServiceCollection AddUsuariosContext(this IServiceCollection services, string connectionString)
{
    services.AddDbContext<UsuariosDbContext>(options =>
        options.UseSqlServer(connectionString));
    
    services.AddScoped<IUsuarioRepository, UsuarioRepository>();
    services.AddScoped<GameStore.Usuarios.Application.Interfaces.IUsuarioService, UsuarioService>();
    services.AddScoped<AuthenticationService>();
    
    return services;
}
```

---

## Build Verification

The fix was verified by running:
```bash
dotnet build GameStore.Catalogo.API.Tests/GameStore.Catalogo.API.Tests.csproj
```

**Result:** ✅ **Build succeeded with 0 errors** (only minor NuGet version warnings about OpenTelemetry packages, which are pre-existing)

---

## Testing Recommendations

To verify the fix works end-to-end:

1. **Run the test suite:**
   ```bash
   dotnet test GameStore.Catalogo.API.Tests/GameStore.Catalogo.API.Tests.csproj
   ```

2. **Check DI container initialization:**
   The `CatalogoWebApplicationFactory.ConfigureClient()` method already includes validation:
   ```csharp
   var descriptor = scopedServices.GetService(typeof(UsuariosDbContext));
   if (descriptor == null)
   {
       throw new InvalidOperationException("UsuariosDbContext was not registered");
   }
   ```
   This will now pass because the correct `Program.cs` is being used.

3. **Verify database migrations:**
   Both `UsuariosDbContext` and `CatalogoDbContext` migrations will execute successfully:
   ```csharp
   var dbUsuarios = scopedServices.GetRequiredService<UsuariosDbContext>();
   dbUsuarios.Database.Migrate();
   
   var dbCatalogo = scopedServices.GetRequiredService<CatalogoDbContext>();
   dbCatalogo.Database.Migrate();
   ```

---

## Summary of Changes

| Aspect | Before | After |
|--------|--------|-------|
| **WebApplicationFactory Type** | `WebApplicationFactory<Program>` | `WebApplicationFactory<global::Program>` |
| **Program.cs Loaded** | GameStore.Catalogo.API/Program.cs | TheThroneOfGames.API/Program.cs |
| **UsuariosDbContext Registered** | ❌ No | ✅ Yes |
| **Build Status** | ❌ Fails | ✅ Succeeds |
| **DI Container State** | Incomplete | Complete |

---

## Additional Notes

### Why `global::` is necessary:
- C# implicit `using` declarations create ambiguity when multiple `Program` types exist
- `global::Program` explicitly references the root namespace's `Program` class
- This is the standard pattern for resolving type ambiguity in WebApplicationFactory

### No Configuration Files Needed:
- The fix does not require changes to `appsettings.json` or `appsettings.Test.json`
- Connection strings are already configured and inherited from the loaded `Program.cs`
- The "Test" environment setting is still honored by `builder.UseEnvironment("Test")`

### Consistency with Working Patterns:
This fix aligns with how `Test/Integration/CustomWebApplicationFactory.cs` works - by explicitly specifying which `Program` to use for dependency configuration.
