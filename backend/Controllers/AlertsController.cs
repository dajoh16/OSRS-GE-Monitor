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
    public async Task<IActionResult> Acknowledge(Guid id, AcknowledgeAlertRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        var position = await _dataStore.AcknowledgeAlertAsync(id, request.Quantity, cancellationToken);
        return position is null ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult RemoveAlert(Guid id)
    {
        return _dataStore.RemoveAlert(id) ? NoContent() : NotFound();
    }

    public sealed record AcknowledgeAlertRequest(int Quantity);
}
