using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Models.Responses;
using Xunit;

namespace OSRSGeMonitor.Api.Tests;

public sealed class PositionIncreaseTests
{
    [Fact]
    public async Task IncreaseQuantity_UpdatesOpenPosition_WhenBuyPriceMatches()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        var create = new CreatePositionRequest(
            ItemId: 1515,
            ItemName: "Yew logs",
            Quantity: 2,
            BuyPrice: 900,
            BoughtAt: null);

        var created = await client.PostAsJsonAsync("/api/positions", create);
        created.EnsureSuccessStatusCode();
        var position = await created.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(position);

        var increase = new IncreasePositionQuantityRequest(BuyPrice: 900, Quantity: 3);
        var increaseResponse = await client.PostAsJsonAsync($"/api/positions/{position!.Id}/increase", increase);
        increaseResponse.EnsureSuccessStatusCode();
        var updated = await increaseResponse.Content.ReadFromJsonAsync<PositionDto>();

        Assert.NotNull(updated);
        Assert.Equal(5, updated!.Quantity);
        Assert.Equal(900, updated.BuyPrice);
    }

    [Fact]
    public async Task IncreaseQuantity_Rejects_WhenBuyPriceMismatch()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        var create = new CreatePositionRequest(
            ItemId: 1515,
            ItemName: "Yew logs",
            Quantity: 2,
            BuyPrice: 900,
            BoughtAt: null);

        var created = await client.PostAsJsonAsync("/api/positions", create);
        created.EnsureSuccessStatusCode();
        var position = await created.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(position);

        var increase = new IncreasePositionQuantityRequest(BuyPrice: 800, Quantity: 1);
        var response = await client.PostAsJsonAsync($"/api/positions/{position!.Id}/increase", increase);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
