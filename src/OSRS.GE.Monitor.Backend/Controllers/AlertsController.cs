using Microsoft.AspNetCore.Mvc;
using OSRS.GE.Monitor.Backend.ApiModels;
using OSRS.GE.Monitor.Backend.Data;

namespace OSRS.GE.Monitor.Backend.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController : ControllerBase
{
    private readonly ITimeSeriesRepository _repository;

    public AlertsController(ITimeSeriesRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{itemId:long}/open")]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetOpenAlertsAsync(long itemId, CancellationToken cancellationToken)
    {
        var alerts = await _repository.GetOpenAlertsAsync(itemId, cancellationToken);
        var response = alerts
            .Select(alert => new AlertResponse(alert.Id, alert.ItemId, alert.Timestamp, alert.Deviation, alert.Status))
            .ToList();

        return response;
    }
}
