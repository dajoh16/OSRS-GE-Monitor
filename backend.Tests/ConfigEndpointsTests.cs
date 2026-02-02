using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OSRSGeMonitor.Api.Models;
using Xunit;

namespace OSRSGeMonitor.Api.Tests;

public sealed class ConfigEndpointsTests
{
    [Fact]
    public async Task UpdateConfig_Persists_DiscordWebhookSettings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        var payload = new ConfigUpdatePayload(
            DiscordNotificationsEnabled: true,
            DiscordWebhookUrl: "https://discord.com/api/webhooks/1234567890/testtoken");

        await using (var factory = CreateFactory(tempRoot))
        {
            using var client = factory.CreateClient();
            var response = await client.PutAsJsonAsync("/api/config", payload);
            response.EnsureSuccessStatusCode();
        }

        await using (var factory = CreateFactory(tempRoot))
        {
            using var client = factory.CreateClient();
            var config = await client.GetFromJsonAsync<GlobalConfig>("/api/config");
            Assert.NotNull(config);
            Assert.True(config!.DiscordNotificationsEnabled);
            Assert.Equal(payload.DiscordWebhookUrl, config.DiscordWebhookUrl);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(string contentRoot)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot);
            });
    }

    private sealed record ConfigUpdatePayload(
        bool DiscordNotificationsEnabled,
        string DiscordWebhookUrl);
}
