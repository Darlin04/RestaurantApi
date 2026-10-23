using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Auth;

[TestFixture]
public sealed class LoginTests
{
    [Test]
    public async Task Login_AdminSeeded_ReturnsTokenWithAdminRole()
    {
        // Arrange
        var factory = new ApiFactory();
        string adminEmail = string.Empty;
        string adminPassword = string.Empty;
        await factory.ExecuteDbContextAsync(_ =>
        {
            var config = factory.Services.GetRequiredService<IConfiguration>();
            adminEmail = config["Seed:AdminEmail"]!;
            adminPassword = config["Seed:AdminPassword"]!;
            return Task.CompletedTask;
        });

        var client = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = adminEmail, password = adminPassword });
        var body = await response.Content.ReadFromJsonAsync<LoginResponsePayload>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body!.AccessToken, Is.Not.Empty);
        Assert.That(body.Roles, Does.Contain("Admin"));
        Assert.That(body.ExpiresAtUtc, Is.GreaterThan(DateTimeOffset.UtcNow.AddMinutes(50)));
        Assert.That(body.ExpiresAtUtc, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(61)));
    }

    [Test]
    public async Task Login_WrongPassword_Returns401InvalidCredentials()
    {
        // Arrange
        var factory = new ApiFactory();
        var client = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "noexiste@test.local", password = "Wrong-Passw0rd!" });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.That(problem!.Code, Is.EqualTo("Auth.InvalidCredentials"));
    }

    [Test]
    public async Task Login_LockedOut_Returns401()
    {
        // Arrange
        var factory = new ApiFactory();
        var email = $"{Guid.NewGuid():N}@test.local";
        const string password = "Test-Passw0rd!";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Restaurant.Infrastructure.Identity.ApplicationUser>>();
            var user = new Restaurant.Infrastructure.Identity.ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            await users.CreateAsync(user, password);
            await users.AddToRoleAsync(user, "Waiter");
        }

        var client = TestAuth.CreateAnonymousClient(factory);
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Wrong-Passw0rd!" });
        }

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Login_RateLimited_Returns429()
    {
        // Arrange
        var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:AuthPermitLimit"] = "5" })));
        var client = factory.CreateClient();

        HttpResponseMessage? last = null;

        // Act
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "x@test.local", password = "Wrong-Passw0rd!" });
        }

        // Assert
        Assert.That(last!.StatusCode, Is.EqualTo((HttpStatusCode)429));
    }

    [Test]
    public async Task Me_DeactivatedToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory();
        var client = await TestAuth.CreateClientAsync(factory, "Waiter");
        await factory.ExecuteDbContextAsync(async db =>
        {
            var user = db.Set<Restaurant.Infrastructure.Identity.ApplicationUser>().First();
            // Desactivar un usuario distinto del admin sembrado para no romper otros tests que comparten la base.
            var target = db.Set<Restaurant.Infrastructure.Identity.ApplicationUser>().FirstOrDefault(u => u.Email!.EndsWith("@test.local"));
            if (target is not null)
            {
                target.IsActive = false;
                await db.SaveChangesAsync();
            }
        });

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_WithoutToken_Returns401()
    {
        // Arrange
        var factory = new ApiFactory();
        var client = TestAuth.CreateAnonymousClient(factory);

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Me_WithToken_ReturnsUserData()
    {
        // Arrange
        var factory = new ApiFactory();
        var client = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<MePayload>();
        Assert.That(body!.Email, Does.EndWith("@test.local"));
    }

    private sealed record LoginResponsePayload(string AccessToken, DateTimeOffset ExpiresAtUtc, Guid UserId, string Email, IReadOnlyList<string> Roles);

    private sealed record ProblemPayload(string Code);

    private sealed record MePayload(Guid UserId, string Email);
}
