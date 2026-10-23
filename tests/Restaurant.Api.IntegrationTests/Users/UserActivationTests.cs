using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Users;

[TestFixture]
public sealed class UserActivationTests
{
    private const string Password = "Test-Passw0rd!";

    [Test]
    public async Task Deactivate_Waiter_Returns204AndRevokesAccess()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_act_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var waiter = await CreateWaiterAsync(admin);
        var waiterClient = await LoginAsync(factory, waiter.Email);

        // Act
        var response = await admin.PatchAsync($"/api/v1/users/{waiter.Id}/deactivate", null);
        var meAfter = await waiterClient.GetAsync("/api/v1/auth/me");
        var loginAfter = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = waiter.Email, password = Password });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(meAfter.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(loginAfter.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await ReadCodeAsync(loginAfter), Is.EqualTo("Auth.InvalidCredentials"));
    }

    [Test]
    public async Task Deactivate_Self_Returns422WithCannotDeactivateSelf()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_act_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var me = await admin.GetFromJsonAsync<MePayload>("/api/v1/auth/me");

        // Act
        var response = await admin.PatchAsync($"/api/v1/users/{me!.Id}/deactivate", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("User.CannotDeactivateSelf"));
    }

    [Test]
    public async Task Activate_InactiveWaiter_Returns204AndAllowsLoginAgain()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_act_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var waiter = await CreateWaiterAsync(admin);
        await admin.PatchAsync($"/api/v1/users/{waiter.Id}/deactivate", null);

        // Act
        var response = await admin.PatchAsync($"/api/v1/users/{waiter.Id}/activate", null);
        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = waiter.Email, password = Password });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Deactivate_UnknownUser_Returns404WithUserNotFound()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_act_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PatchAsync($"/api/v1/users/{Guid.NewGuid()}/deactivate", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ReadCodeAsync(response), Is.EqualTo("User.NotFound"));
    }

    [Test]
    public async Task Deactivate_WaiterCaller_Returns403()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_act_" + Guid.NewGuid().ToString("N"));
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PatchAsync($"/api/v1/users/{Guid.NewGuid()}/deactivate", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static async Task<CreatedWaiter> CreateWaiterAsync(HttpClient admin)
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var response = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            firstName = "Mesa",
            lastName = "Dos",
            password = Password,
            role = "Waiter",
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreatedUserPayload>();
        return new CreatedWaiter(body!.Id, email);
    }

    private static async Task<HttpClient> LoginAsync(ApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private sealed record CreatedWaiter(Guid Id, string Email);

    private sealed record CreatedUserPayload(Guid Id);

    private sealed record LoginPayload(string AccessToken);

    private sealed record MePayload(Guid Id);
}
