using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Responses;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;
    private readonly ItemCatalogService _catalogService;
    private readonly DiscordNotificationService _discordNotificationService;

    public WatchlistController(
        InMemoryDataStore dataStore,
        ItemCatalogService catalogService,
        DiscordNotificationService discordNotificationService)
    {
        _dataStore = dataStore;
        _catalogService = catalogService;
        _discordNotificationService = discordNotificationService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<MonitoredItem>> GetWatchlist()
    {
        return Ok(_dataStore.GetItems());
    }

    [HttpGet("{id:int}")]
    public ActionResult<MonitoredItem> GetWatchlistItem(int id)
    {
        var item = _dataStore.GetItem(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MonitoredItem>> AddItem(AddToWatchlistRequest request, CancellationToken cancellationToken)
    {
        var item = await _catalogService.FindByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
        {
            return NotFound($"Item {request.ItemId} not found in catalog.");
        }

        var monitored = _dataStore.AddItem(new Models.Requests.CreateMonitoredItemRequest
        {
            Id = item.Id,
            Name = item.Name
        });

        return CreatedAtAction(nameof(GetWatchlistItem), new { id = monitored.Id }, monitored);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkAddWatchlistResponse>> AddItemsBulk(
        BulkAddWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Names is null || request.Names.Count == 0)
        {
            return BadRequest("At least one item name is required.");
        }

        var added = new List<MonitoredItem>();
        var notFound = new List<string>();
        var duplicates = new List<string>();
        var matched = new List<BulkMatchResult>();
        var existingIds = _dataStore.GetItems().Select(item => item.Id).ToHashSet();

        foreach (var name in request.Names.Where(entry => !string.IsNullOrWhiteSpace(entry)))
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var catalogItem = await _catalogService.FindByNameFuzzyAsync(trimmed, cancellationToken);
            if (catalogItem is null)
            {
                notFound.Add(trimmed);
                continue;
            }

            if (existingIds.Contains(catalogItem.Id))
            {
                duplicates.Add(trimmed);
                continue;
            }

            var monitored = _dataStore.AddItem(new Models.Requests.CreateMonitoredItemRequest
            {
                Id = catalogItem.Id,
                Name = catalogItem.Name
            });
            existingIds.Add(monitored.Id);
            added.Add(monitored);
            matched.Add(new BulkMatchResult(trimmed, catalogItem.Name, catalogItem.Id));
        }

        var response = new BulkAddWatchlistResponse(added, notFound, duplicates, matched);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public IActionResult RemoveItem(int id)
    {
        return _dataStore.RemoveItem(id) ? NoContent() : NotFound();
    }

    [HttpGet("market")]
    public async Task<ActionResult<IReadOnlyCollection<WatchlistMarketDto>>> GetMarketLatest(
        CancellationToken cancellationToken)
    {
        var items = _dataStore.GetItems().ToArray();
        var ids = items.Select(item => item.Id).ToArray();
        var snapshots = _dataStore.GetLatestPrices(ids);
        var catalog = new Dictionary<int, ItemCatalogService.ItemCatalogEntry>();
        foreach (var item in items)
        {
            var match = await _catalogService.FindByIdAsync(item.Id, cancellationToken);
            if (match is not null)
            {
                catalog[item.Id] = match;
            }
        }
        var results = snapshots
            .Select(snapshot =>
            {
                catalog.TryGetValue(snapshot.ItemId, out var entry);
                return new WatchlistMarketDto(
                    snapshot.ItemId,
                    snapshot.High,
                    snapshot.Low,
                    snapshot.HighTime,
                    snapshot.LowTime,
                    entry?.Limit);
            })
            .OrderBy(entry => entry.ItemId)
            .ToArray();
        return Ok(results);
    }

    [HttpPost("{id:int}/discord-report")]
    public async Task<IActionResult> SendDiscordReport(int id, CancellationToken cancellationToken)
    {
        var config = _dataStore.Config;
        if (!config.DiscordNotificationsEnabled || string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
        {
            return BadRequest("Discord notifications are disabled or webhook URL is missing.");
        }

        var item = _dataStore.GetItem(id);
        if (item is null)
        {
            return NotFound();
        }

        var snapshot = _dataStore.GetLatestPrice(id);
        if (snapshot?.High is null || snapshot.Low is null)
        {
            return Conflict("Market data not available for this item yet.");
        }

        var high = snapshot.High.Value;
        var low = snapshot.Low.Value;
        var spread = high - low;
        var tax = CalculateTaxPerItem(high);
        var afterTax = high - low - tax;

        await _discordNotificationService.EnqueueReportAsync(
            item.Name,
            high,
            low,
            spread,
            afterTax,
            cancellationToken);

        return Accepted();
    }

    public sealed class AddToWatchlistRequest
    {
        public int ItemId { get; set; }
    }

    public sealed class BulkAddWatchlistRequest
    {
        public List<string> Names { get; set; } = new();
    }

    public sealed record BulkMatchResult(string InputName, string MatchedName, int ItemId);

    public sealed record BulkAddWatchlistResponse(
        IReadOnlyCollection<MonitoredItem> Added,
        IReadOnlyCollection<string> NotFound,
        IReadOnlyCollection<string> Duplicates,
        IReadOnlyCollection<BulkMatchResult> Matched);

    private static double CalculateTaxPerItem(double sellPrice)
    {
        if (sellPrice < 100)
        {
            return 0;
        }

        var tax = Math.Floor(sellPrice * 0.02);
        return Math.Min(tax, 5_000_000);
    }
}
