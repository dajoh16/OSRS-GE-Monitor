using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Models.Responses;
using Xunit;

namespace OSRSGeMonitor.Api.Tests;

public sealed class PositionProfitTests
{
    [Fact]
    public async Task SellPosition_ComputesProfit_WithGeTaxRules()
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

        var sell = new SellPositionRequest(1000, null);
        var soldResponse = await client.PostAsJsonAsync($"/api/positions/{position!.Id}/sell", sell);
        soldResponse.EnsureSuccessStatusCode();
        var sold = await soldResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(sold);

        var expectedTaxPerItem = Math.Floor(1000 * 0.02);
        var expectedTax = expectedTaxPerItem * 2;
        var expectedProfit = (1000 * 2) - (900 * 2) - expectedTax;

        Assert.Equal(expectedTax, sold!.TaxPaid);
        Assert.Equal(expectedProfit, sold.Profit);
        Assert.True(sold.IsSold);

        var update = new UpdateBuyPriceRequest(800);
        var updateResponse = await client.PostAsJsonAsync($"/api/positions/{position.Id}/buy-price", update);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(updated);
        var expectedProfitAfterUpdate = (1000 * 2) - (800 * 2) - expectedTax;
        Assert.Equal(expectedProfitAfterUpdate, updated!.Profit);
    }

    [Fact]
    public async Task SellPosition_PartialSell_SplitsPositionAndComputesProfit()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"osrs-ge-monitor-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);

        await using var factory = CreateFactory(tempRoot);
        using var client = factory.CreateClient();

        var create = new CreatePositionRequest(
            ItemId: 1515,
            ItemName: "Yew logs",
            Quantity: 10,
            BuyPrice: 900,
            BoughtAt: null);

        var created = await client.PostAsJsonAsync("/api/positions", create);
        created.EnsureSuccessStatusCode();
        var position = await created.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(position);

        var sell = new SellPositionRequest(1000, 4);
        var soldResponse = await client.PostAsJsonAsync($"/api/positions/{position!.Id}/sell", sell);
        soldResponse.EnsureSuccessStatusCode();
        var sold = await soldResponse.Content.ReadFromJsonAsync<PositionDto>();
        Assert.NotNull(sold);

        var expectedTaxPerItem = Math.Floor(1000 * 0.02);
        var expectedTax = expectedTaxPerItem * 4;
        var expectedProfit = (1000 * 4) - (900 * 4) - expectedTax;

        Assert.Equal(4, sold!.Quantity);
        Assert.Equal(expectedTax, sold.TaxPaid);
        Assert.Equal(expectedProfit, sold.Profit);
        Assert.True(sold.IsSold);

        var positions = await client.GetFromJsonAsync<PositionDto[]>("/api/positions");
        Assert.NotNull(positions);

        var remaining = positions!.Single(entry => !entry.IsSold);
        Assert.Equal(6, remaining.Quantity);
        Assert.False(remaining.IsSold);
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
