using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/positions")]
public class PositionsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public PositionsController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Position>> GetPositions()
    {
        return Ok(_dataStore.GetPositions());
    }

    [HttpPost]
    public ActionResult<Position> AddPosition(CreatePositionRequest request)
    {
        var position = _dataStore.AddPosition(request);
        return CreatedAtAction(nameof(GetPositions), new { id = position.Id }, position);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid id)
    {
        return _dataStore.AcknowledgePosition(id) ? NoContent() : NotFound();
    }
}
