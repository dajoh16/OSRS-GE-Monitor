using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;
    private readonly ItemCatalogService _catalogService;

    public WatchlistController(InMemoryDataStore dataStore, ItemCatalogService catalogService)
    {
        _dataStore = dataStore;
        _catalogService = catalogService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<MonitoredItem>> GetWatchlist()
    {
        return Ok(_dataStore.GetItems());
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

        return CreatedAtAction(nameof(GetWatchlist), new { id = monitored.Id }, monitored);
    }

    [HttpDelete("{id:int}")]
    public IActionResult RemoveItem(int id)
    {
        return _dataStore.RemoveItem(id) ? NoContent() : NotFound();
    }

    public sealed class AddToWatchlistRequest
    {
        public int ItemId { get; set; }
    }
}
