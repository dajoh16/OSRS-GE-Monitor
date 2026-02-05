using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Services;
using Xunit;

namespace OSRSGeMonitor.Api.Tests;

public sealed class WatchlistDiscordReportTests
{
    [Fact]
    public async Task DiscordReport_ReturnsBadRequest_WhenDiscordDisabled()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/watchlist/1/discord-report", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscordReport_ReturnsAccepted_WhenConfiguredAndMarketDataReady()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        var configPayload = new ConfigUpdatePayload(
            DiscordNotificationsEnabled: true,
            DiscordWebhookUrl: "https://discord.com/api/webhooks/1234567890/testtoken");
        using var configResponse = await client.PutAsJsonAsync("/api/config", configPayload);
        configResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var dataStore = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
            dataStore.AddItem(new CreateMonitoredItemRequest
            {
                Id = 4151,
                Name = "Abyssal whip"
            });
            dataStore.UpdateLatestPrice(
                4151,
                new InMemoryDataStore.LatestPriceSnapshot(
                    4151,
                    100_000,
                    95_000,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow));
        }

        using var response = await client.PostAsync("/api/watchlist/4151/discord-report", null);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(string contentRoot)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                });
            });
    }

    private sealed record ConfigUpdatePayload(
        bool DiscordNotificationsEnabled,
        string DiscordWebhookUrl);
}
