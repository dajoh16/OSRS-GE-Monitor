using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public NotificationsController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
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
}
