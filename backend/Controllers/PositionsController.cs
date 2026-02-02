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

    [HttpGet("{id:guid}")]
    public ActionResult<Position> GetPosition(Guid id)
    {
        var position = _dataStore.GetPosition(id);
        return position is null ? NotFound() : Ok(position);
    }

    [HttpPost]
    public ActionResult<Position> AddPosition(CreatePositionRequest request)
    {
        var position = _dataStore.AddPosition(request);
        return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, position);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid id)
    {
        return _dataStore.AcknowledgePosition(id) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult RemovePosition(Guid id)
    {
        return _dataStore.RemovePosition(id) ? NoContent() : NotFound();
    }
}
