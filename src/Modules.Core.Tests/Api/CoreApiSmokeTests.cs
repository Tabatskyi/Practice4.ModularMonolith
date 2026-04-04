using Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Modules.Core.Tests.Api;

public class CoreApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public CoreApiSmokeTests(WebApplicationFactory<Program> factory)
    {
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
}