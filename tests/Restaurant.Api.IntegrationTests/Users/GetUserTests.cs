using System.Net;
using System.Net.Http.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Users;

[TestFixture]
public sealed class GetUserTests
{
    [Test]
    public async Task Get_Existing_Returns200()
    {
        // Arrange
        var factory = new ApiFactory();
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var email = $"{Guid.NewGuid():N}@test.local";
        var created = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            email,
            firstName = "Juan",
            lastName = "Perez",
            password = "Test-Passw0rd!",
            role = "Waiter",
        });
        var createdBody = await created.Content.ReadFromJsonAsync<IdPayload>();

        // Act
        var response = await admin.GetAsync($"/api/v1/users/{createdBody!.Id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Get_Missing_Returns404()
    {
        // Arrange
        var factory = new ApiFactory();
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");

        // Act
        var response = await admin.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private sealed record IdPayload(Guid Id);
}
