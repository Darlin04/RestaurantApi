using System.Net;
using System.Net.Http.Json;

using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Users;

[TestFixture]
public sealed class ListUsersTests
{
    [Test]
    public async Task List_Filtered_ReturnsOnlyActiveWaitersOrdered()
    {
        // Arrange
        var factory = new ApiFactory(databaseName: "usr_list_" + Guid.NewGuid().ToString("N"));
        var admin = await TestAuth.CreateClientAsync(factory, "Admin");
        var emails = new[] { $"zzz-{Guid.NewGuid():N}@test.local", $"aaa-{Guid.NewGuid():N}@test.local" };
        foreach (var email in emails)
        {
            await admin.PostAsJsonAsync("/api/v1/users", new
            {
                email,
                firstName = "Test",
                lastName = "User",
                password = "Test-Passw0rd!",
                role = "Waiter",
            });
        }

        // Act
        var response = await admin.GetAsync("/api/v1/users?role=Waiter&isActive=true");
        var body = await response.Content.ReadFromJsonAsync<List<UserPayload>>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var filtered = body!.Where(u => emails.Contains(u.Email)).ToList();
        Assert.That(filtered.Count, Is.EqualTo(2));
        Assert.That(filtered.Select(u => u.Email).ToList(), Is.Ordered);
    }

    private sealed record UserPayload(Guid Id, string Email, string Role, bool IsActive);
}
