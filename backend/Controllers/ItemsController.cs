using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public ItemsController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<MonitoredItem>> GetItems()
    {
        return Ok(_dataStore.GetItems());
    }

    [HttpPost]
    public ActionResult<MonitoredItem> AddItem(CreateMonitoredItemRequest request)
    {
        var item = _dataStore.AddItem(request);
        return CreatedAtAction(nameof(GetItems), new { id = item.Id }, item);
    }

    [HttpDelete("{id:int}")]
    public IActionResult RemoveItem(int id)
    {
        return _dataStore.RemoveItem(id) ? NoContent() : NotFound();
    }
}
