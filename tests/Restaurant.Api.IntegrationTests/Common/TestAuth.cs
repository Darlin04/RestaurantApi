using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Restaurant.Infrastructure.Identity;

namespace Restaurant.Api.IntegrationTests.Common;

public static class TestAuth
{
    public static async Task<HttpClient> CreateClientAsync(ApiFactory factory, string role)
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        const string password = "Test-Passw0rd!";

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario de prueba: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await users.AddToRoleAsync(user, role);
        }

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    public static HttpClient CreateAnonymousClient(ApiFactory factory) => factory.CreateClient();

    private sealed record LoginPayload(string AccessToken);
}
