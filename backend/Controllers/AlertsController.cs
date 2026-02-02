using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public AlertsController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Alert>> GetAlerts([FromQuery] string? status)
    {
        return Ok(_dataStore.GetAlerts(status));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Alert> GetAlert(Guid id)
    {
        var alert = _dataStore.GetAlert(id);
        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid id)
    {
        return _dataStore.AcknowledgeAlert(id) ? NoContent() : NotFound();
    }
}
