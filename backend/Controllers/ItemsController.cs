using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models.Responses;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly ItemCatalogService _catalogService;
    private readonly OsrsTimeSeriesService _timeSeriesService;
    private readonly InMemoryDataStore _dataStore;

    public ItemsController(
        ItemCatalogService catalogService,
        OsrsTimeSeriesService timeSeriesService,
        InMemoryDataStore dataStore)
    {
        _catalogService = catalogService;
        _timeSeriesService = timeSeriesService;
        _dataStore = dataStore;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ItemCatalogService.ItemCatalogEntry>>> SearchItems(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        var results = await _catalogService.SearchAsync(query ?? string.Empty, cancellationToken);
        return Ok(results);
    }

    [HttpGet("status")]
    public ActionResult<ItemCatalogService.CatalogStatus> GetStatus()
    {
        return Ok(_catalogService.GetStatus());
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<ItemDetailsDto>> GetDetails(int id, CancellationToken cancellationToken)
    {
        var item = await _catalogService.FindByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        var trend = await BuildTrendAsync(id, cancellationToken);
        var latest = _dataStore.GetLatestPrice(id);
        var latestDto = latest is null
            ? null
            : new LatestPriceDto(latest.ItemId, latest.High, latest.Low, latest.HighTime, latest.LowTime);

        return Ok(new ItemDetailsDto(
            item.Id,
            item.Name,
            item.Examine,
            item.Members,
            item.Limit,
            item.Lowalch,
            item.Highalch,
            item.Value,
            item.Icon,
            trend,
            latestDto));
    }

    private async Task<ItemTrendDto?> BuildTrendAsync(int itemId, CancellationToken cancellationToken)
    {
        var points = await _timeSeriesService.GetTimeSeriesAsync(itemId, TimeSeriesTimestep.OneHour, cancellationToken);
        if (points.Count < 2)
        {
            return null;
        }

        var ordered = points.OrderBy(point => point.Timestamp).TakeLast(2).ToArray();
        var previous = ordered[0].Price;
        var latest = ordered[1].Price;
        if (previous <= 0)
        {
            return null;
        }

        var percent = (latest - previous) / previous * 100.0;
        var direction = Math.Abs(percent) < 0.1
            ? "flat"
            : percent > 0 ? "up" : "down";

        return new ItemTrendDto("1h", direction, Math.Round(percent, 2), latest, previous);
    }
}
