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
        var filter = string.IsNullOrWhiteSpace(status) ? "active" : status;
        return Ok(_dataStore.GetAlerts(filter));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Alert> GetAlert(Guid id)
    {
        var alert = _dataStore.GetAlert(id);
        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid id, AcknowledgeAlertRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        return _dataStore.AcknowledgeAlert(id, request.Quantity, out _) ? NoContent() : NotFound();
    }

    public sealed class AcknowledgeAlertRequest
    {
        public int Quantity { get; set; }
    }
}
