using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly OsrsTimeSeriesService _timeSeriesService;

    public PricesController(OsrsTimeSeriesService timeSeriesService)
    {
        _timeSeriesService = timeSeriesService;
    }

    [HttpGet("{itemId:int}/history")]
    public async Task<ActionResult<IReadOnlyCollection<PriceHistoryPoint>>> GetHistory(
        int itemId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] int? maxPoints,
        CancellationToken cancellationToken)
    {
        if (maxPoints.HasValue && maxPoints.Value <= 0)
        {
            return BadRequest("maxPoints must be greater than zero.");
        }

        if (!TryParseTimestamp(from, out var fromTimestamp, out var fromError))
        {
            return BadRequest(fromError);
        }

        if (!TryParseTimestamp(to, out var toTimestamp, out var toError))
        {
            return BadRequest(toError);
        }

        var resolvedTo = toTimestamp ?? DateTimeOffset.UtcNow;
        var resolvedFrom = fromTimestamp ?? resolvedTo.AddDays(-30);

        if (resolvedFrom > resolvedTo)
        {
            return BadRequest("'from' must be earlier than 'to'.");
        }

        var range = resolvedTo - resolvedFrom;
        var timestep = SelectTimestep(range);

        var history = await _timeSeriesService.GetTimeSeriesAsync(itemId, timestep, cancellationToken);
        var filtered = history
            .Where(point => point.Timestamp >= resolvedFrom && point.Timestamp <= resolvedTo)
            .Select(point => new PriceHistoryPoint(point.Timestamp, point.Price))
            .ToList();

        if (maxPoints.HasValue && maxPoints.Value > 0 && filtered.Count > maxPoints.Value)
        {
            filtered = Downsample(filtered, maxPoints.Value);
        }

        return Ok(filtered);
    }

    [HttpGet("{itemId:int}/latest")]
    public async Task<ActionResult<PriceHistoryPoint>> GetLatest(
        int itemId,
        CancellationToken cancellationToken)
    {
        var history = await _timeSeriesService.GetTimeSeriesAsync(itemId, TimeSeriesTimestep.FiveMinutes, cancellationToken);
        var latest = history.OrderBy(point => point.Timestamp).LastOrDefault();
        if (latest is null)
        {
            return NotFound();
        }

        return Ok(new PriceHistoryPoint(latest.Timestamp, latest.Price));
    }

    private static bool TryParseTimestamp(
        string? input,
        out DateTimeOffset? value,
        out string error)
    {
        value = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Invalid timestamp '{input}'. Use ISO-8601 format.";
        return false;
    }

    public sealed record PriceHistoryPoint(DateTimeOffset Timestamp, double Price);

    private static TimeSeriesTimestep SelectTimestep(TimeSpan range)
    {
        if (range <= TimeSpan.FromHours(36))
        {
            return TimeSeriesTimestep.FiveMinutes;
        }

        if (range <= TimeSpan.FromDays(14))
        {
            return TimeSeriesTimestep.OneHour;
        }

        if (range <= TimeSpan.FromDays(90))
        {
            return TimeSeriesTimestep.SixHours;
        }

        return TimeSeriesTimestep.OneDay;
    }

    private static List<PriceHistoryPoint> Downsample(
        IReadOnlyList<PriceHistoryPoint> points,
        int maxPoints)
    {
        if (points.Count <= maxPoints)
        {
            return points.ToList();
        }

        var step = (int)Math.Ceiling(points.Count / (double)maxPoints);
        var sampled = new List<PriceHistoryPoint>();
        for (var index = 0; index < points.Count; index += step)
        {
            sampled.Add(points[index]);
        }

        var last = points[^1];
        if (sampled.Count == 0 || sampled[^1].Timestamp != last.Timestamp)
        {
            sampled.Add(last);
        }

        return sampled;
    }
}
