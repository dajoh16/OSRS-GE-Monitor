using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Models.Responses;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/positions")]
public class PositionsController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;
    private readonly ItemCatalogService _catalogService;

    public PositionsController(InMemoryDataStore dataStore, ItemCatalogService catalogService)
    {
        _dataStore = dataStore;
        _catalogService = catalogService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<PositionDto>> GetPositions()
    {
        return Ok(_dataStore.GetPositions().Select(ToDto).ToArray());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PositionDto> GetPosition(Guid id)
    {
        var position = _dataStore.GetPosition(id);
        return position is null ? NotFound() : Ok(ToDto(position));
    }

    [HttpPost]
    public async Task<ActionResult<PositionDto>> AddPosition(CreatePositionRequest request, CancellationToken cancellationToken)
    {
        var position = await _dataStore.AddPositionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, ToDto(position));
    }

    [HttpPost("manual")]
    public async Task<ActionResult<PositionDto>> AddManualPosition(
        ManualPositionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        if (request.BuyPrice <= 0)
        {
            return BadRequest("Buy price must be greater than zero.");
        }

        if (request.ItemId is null && string.IsNullOrWhiteSpace(request.ItemName))
        {
            return BadRequest("Item name is required.");
        }

        ItemCatalogService.ItemCatalogEntry? match = null;
        if (request.ItemId.HasValue && !string.IsNullOrWhiteSpace(request.ItemName))
        {
            match = new ItemCatalogService.ItemCatalogEntry
            {
                Id = request.ItemId.Value,
                Name = request.ItemName.Trim()
            };
        }
        else if (request.ItemId.HasValue)
        {
            match = await _catalogService.FindByIdAsync(request.ItemId.Value, cancellationToken);
        }

        if (match is null && !string.IsNullOrWhiteSpace(request.ItemName))
        {
            match = await _catalogService.FindByNameAsync(request.ItemName, cancellationToken)
                ?? await _catalogService.FindByNameFuzzyAsync(request.ItemName, cancellationToken);
        }

        if (match is null)
        {
            return BadRequest("No catalog item found matching that name.");
        }

        var position = await _dataStore.AddPositionAsync(new CreatePositionRequest(
            match.Id,
            match.Name,
            request.Quantity,
            request.BuyPrice,
            request.BoughtAt), cancellationToken);

        return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, ToDto(position));
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        return await _dataStore.AcknowledgePositionAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RemovePosition(Guid id, CancellationToken cancellationToken)
    {
        return await _dataStore.RemovePositionAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/sell")]
    public async Task<ActionResult<PositionDto>> SellPosition(
        Guid id,
        SellPositionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SellPrice <= 0)
        {
            return BadRequest("Sell price must be greater than zero.");
        }

        var position = await _dataStore.SellPositionAsync(id, request.SellPrice, cancellationToken);
        return position is null ? NotFound() : Ok(ToDto(position));
    }

    [HttpPost("{id:guid}/buy-price")]
    public async Task<ActionResult<PositionDto>> UpdateBuyPrice(
        Guid id,
        UpdateBuyPriceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BuyPrice <= 0)
        {
            return BadRequest("Buy price must be greater than zero.");
        }

        var position = await _dataStore.UpdateBuyPriceAsync(id, request.BuyPrice, cancellationToken);
        return position is null ? NotFound() : Ok(ToDto(position));
    }

    [HttpGet("summary")]
    public ActionResult<PositionSummaryDto> GetSummary()
    {
        return Ok(_dataStore.GetPositionSummary());
    }

    [HttpGet("history")]
    public ActionResult<IReadOnlyCollection<ProfitPointDto>> GetHistory([FromQuery] int? itemId)
    {
        return Ok(_dataStore.GetProfitHistory(itemId));
    }

    private static PositionDto ToDto(Position position)
    {
        return new PositionDto(
            position.Id,
            position.ItemId,
            position.ItemName,
            position.Quantity,
            position.BuyPrice,
            position.BoughtAt,
            position.AcknowledgedAt,
            position.RecoveredAt,
            position.RecoveryPrice,
            position.SellPrice,
            position.SoldAt,
            position.TaxRateApplied,
            position.TaxPaid,
            position.Profit,
            position.IsSold);
    }
}
