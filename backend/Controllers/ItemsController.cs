using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly ItemCatalogService _catalogService;

    public ItemsController(ItemCatalogService catalogService)
    {
        _catalogService = catalogService;
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
}
