using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Responses;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;
    private readonly ItemCatalogService _catalogService;

    public NotificationsController(InMemoryDataStore dataStore, ItemCatalogService catalogService)
    {
        _dataStore = dataStore;
        _catalogService = catalogService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Notification>> GetNotifications()
    {
        return Ok(_dataStore.GetNotifications());
    }

    [HttpDelete]
    public IActionResult ClearNotifications()
    {
        _dataStore.ClearNotifications();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult RemoveNotification(Guid id)
    {
        return _dataStore.RemoveNotification(id) ? NoContent() : NotFound();
    }

    [HttpGet("suppressed")]
    public async Task<ActionResult<IReadOnlyCollection<SuppressedItemDto>>> GetSuppressed(
        CancellationToken cancellationToken)
    {
        var ids = _dataStore.GetSuppressedDropItems();
        if (ids.Count == 0)
        {
            return Ok(Array.Empty<SuppressedItemDto>());
        }

        var results = new List<SuppressedItemDto>(ids.Count);
        foreach (var id in ids)
        {
            var match = await _catalogService.FindByIdAsync(id, cancellationToken);
            var name = match?.Name ?? $"Item #{id}";
            results.Add(new SuppressedItemDto(id, name));
        }

        return Ok(results);
    }

    [HttpDelete("suppressed/{itemId:int}")]
    public IActionResult RemoveSuppressed(int itemId)
    {
        _dataStore.ClearDropSuppression(itemId);
        return NoContent();
    }
}
