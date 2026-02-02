using Microsoft.AspNetCore.Mvc;
using OSRS.GE.Monitor.Backend.ApiModels;
using OSRS.GE.Monitor.Backend.Data;
using OSRS.GE.Monitor.Backend.Models;

namespace OSRS.GE.Monitor.Backend.Controllers;

[ApiController]
[Route("api/prices")]
public sealed class PriceHistoryController : ControllerBase
{
    private readonly ITimeSeriesRepository _repository;

    public PriceHistoryController(ITimeSeriesRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> InsertPriceAsync([FromBody] PriceHistoryRequest request, CancellationToken cancellationToken)
    {
        var entry = new PriceHistoryEntry(request.ItemId, request.Timestamp, request.BuyPrice, request.SellPrice);
        await _repository.InsertPriceHistoryAsync(entry, cancellationToken);
        return Accepted();
    }

    [HttpGet("{itemId:long}/latest")]
    public async Task<ActionResult<PriceHistoryResponse>> GetLatestAsync(long itemId, CancellationToken cancellationToken)
    {
        var latest = await _repository.GetLatestPriceAsync(itemId, cancellationToken);
        if (latest is null)
        {
            return NotFound();
        }

        return new PriceHistoryResponse(latest.ItemId, latest.Timestamp, latest.BuyPrice, latest.SellPrice);
    }

    [HttpGet("{itemId:long}")]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryResponse>>> GetHistoryAsync(long itemId, [FromQuery] DateTimeOffset since, CancellationToken cancellationToken)
    {
        var history = await _repository.GetPriceHistoryAsync(itemId, since, cancellationToken);
        var response = history
            .Select(entry => new PriceHistoryResponse(entry.ItemId, entry.Timestamp, entry.BuyPrice, entry.SellPrice))
            .ToList();

        return response;
    }
}
