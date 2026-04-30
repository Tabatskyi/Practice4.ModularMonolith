using Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Modules.Core.Tests.Api;

public class CoreApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public CoreApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("Migrations__ApplyOnStartup", "false");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__CoreDb",
            "Host=localhost;Port=5432;Database=practice4_modular_monolith;Username=postgres;Password=postgres");

        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkAndHealthyStatus()
    {
        var response = await _httpClient.GetAsync("/health");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("Healthy", payload);
    }

    [Fact]
    public async Task GetHealth_GeneratesCorrelationIdHeader_WhenRequestHeaderIsMissing()
    {
        var response = await _httpClient.GetAsync("/health");

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        var correlationId = Assert.Single(values);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task GetHealth_ReturnsExistingCorrelationIdHeader_WhenProvided()
    {
        var expectedCorrelationId = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", expectedCorrelationId);

        var response = await _httpClient.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-Id", out var values));
        var correlationId = Assert.Single(values);
        Assert.Equal(expectedCorrelationId, correlationId);
    }
}