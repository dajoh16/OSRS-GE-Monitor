using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OSRSGeMonitor.Api.Models.Responses;
using Xunit;

namespace OSRSGeMonitor.Api.Tests;

public sealed class ManualPositionTests
{
    [Fact]
    public async Task AddManualPosition_AcceptsProvidedItemIdAndName()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        var payload = new ManualPositionPayload(
            ItemName: "Test Item",
            ItemId: 123,
            Quantity: 5,
            BuyPrice: 410);

        using var response = await client.PostAsJsonAsync("/api/positions/manual", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var position = await response.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(position);
        Assert.Equal(123, position!.ItemId);
        Assert.Equal("Test Item", position.ItemName);
        Assert.Equal(5, position.Quantity);
        Assert.Equal(410, position.BuyPrice);
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

    private sealed record ManualPositionPayload(
        string ItemName,
        int ItemId,
        int Quantity,
        double BuyPrice);
}
