using Restaurant.Api.IntegrationTests.Common;

namespace Restaurant.Api.IntegrationTests.Platform;

[TestFixture]
public sealed class HealthTests
{
    [Test]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Arrange
        var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(body, Does.Contain("Healthy"));
    }
}
