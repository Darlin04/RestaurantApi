using System.Net;
using System.Net.Http.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Users;

[TestFixture]
public sealed class CreateUserTests
{
    [Test]
    public async Task Create_Waiter_Returns201()
    {
        // Arrange
        var factory = new ApiFactory();
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var email = $"{Guid.NewGuid():N}@test.local";

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            firstName = "Mesa",
            lastName = "Uno",
            password = "Test-Passw0rd!",
            role = "Waiter",
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<UserPayload>();
        Assert.That(body!.Role, Is.EqualTo("Waiter"));
        Assert.That(body.IsActive, Is.True);
        Assert.That(response.Headers.Location?.ToString(), Does.Contain($"/api/v1/users/{body.Id}"));
    }

    [Test]
    public async Task Create_DuplicateEmail_Returns409()
    {
        // Arrange
        var factory = new ApiFactory();
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var email = $"{Guid.NewGuid():N}@test.local";
        var payload = new { email, firstName = "Juan", lastName = "Perez", password = "Test-Passw0rd!", role = "Waiter" };
        await admin.PostAsJsonAsync("/api/v1/users", payload);

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/users", payload);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Create_InvalidRole_Returns400()
    {
        // Arrange
        var factory = new ApiFactory();
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"{Guid.NewGuid():N}@test.local",
            firstName = "Juan",
            lastName = "Perez",
            password = "Test-Passw0rd!",
            role = "Chef",
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Create_WaiterCannotCreate_Returns403()
    {
        // Arrange
        var factory = new ApiFactory();
        var waiter = await TestAuth.CreateClientAsync(factory, "Waiter");

        // Act
        var response = await waiter.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"{Guid.NewGuid():N}@test.local",
            firstName = "Juan",
            lastName = "Perez",
            password = "Test-Passw0rd!",
            role = "Waiter",
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private sealed record UserPayload(Guid Id, string Role, bool IsActive);
}
