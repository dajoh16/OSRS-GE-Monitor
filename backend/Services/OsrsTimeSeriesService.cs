using System.Collections.Concurrent;
using System.Text.Json;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public enum TimeSeriesTimestep
{
    FiveMinutes,
    OneHour,
    SixHours,
    OneDay
}

public class OsrsTimeSeriesService
{
    private const string ApiUrl = "https://prices.runescape.wiki/api/v1/osrs/timeseries";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InMemoryDataStore _dataStore;
    private readonly SqliteTimeSeriesCacheStore _cacheStore;
    private readonly ILogger<OsrsTimeSeriesService> _logger;
    private readonly ConcurrentDictionary<(int ItemId, TimeSeriesTimestep Timestep), CacheEntry> _cache = new();

    public OsrsTimeSeriesService(
        IHttpClientFactory httpClientFactory,
        InMemoryDataStore dataStore,
        SqliteTimeSeriesCacheStore cacheStore,
        ILogger<OsrsTimeSeriesService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dataStore = dataStore;
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PricePoint>> GetTimeSeriesAsync(
        int itemId,
        TimeSeriesTimestep timestep,
        CancellationToken cancellationToken)
    {
        var key = (itemId, timestep);
        var now = DateTimeOffset.UtcNow;
        var cacheTtl = GetCacheTtl(timestep);
        if (_cache.TryGetValue(key, out var entry) && now - entry.FetchedAt < cacheTtl)
        {
            return entry.Points;
        }

        var persisted = _cacheStore.Get(itemId, ToQueryValue(timestep));
        if (persisted is not null && now - persisted.FetchedAt < cacheTtl)
        {
            var cached = new CacheEntry(persisted.FetchedAt, persisted.Points);
            _cache[key] = cached;
            return cached.Points;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            ApplyUserAgent(client, _dataStore.Config.UserAgent);

            var url = $"{ApiUrl}?timestep={ToQueryValue(timestep)}&id={itemId}";
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<TimeSeriesResponse>(stream, JsonOptions, cancellationToken);
            if (payload?.Data is null)
            {
                return Array.Empty<PricePoint>();
            }

            var points = payload.Data
                .Select(point =>
                {
                    var price = GetRepresentativePrice(point.AvgHighPrice, point.AvgLowPrice);
                    if (!price.HasValue)
                    {
                        return null;
                    }

                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(point.Timestamp);
                    return new PricePoint(timestamp, price.Value);
                })
                .Where(point => point is not null)
                .Select(point => point!)
                .ToList();

            _cache[key] = new CacheEntry(now, points);
            _cacheStore.Upsert(itemId, ToQueryValue(timestep), now, points);
            return points;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OSRS time-series for item {ItemId}.", itemId);
            return Array.Empty<PricePoint>();
        }
    }

    public async Task<(double Mean, double StandardDeviation, int SampleSize)> GetRollingStatsAsync(
        int itemId,
        TimeSeriesTimestep timestep,
        int windowSize,
        CancellationToken cancellationToken)
    {
        var points = await GetTimeSeriesAsync(itemId, timestep, cancellationToken);
        if (points.Count == 0)
        {
            return (0, 0, 0);
        }

        var max = Math.Max(windowSize, 1);
        var recent = points
            .OrderBy(point => point.Timestamp)
            .TakeLast(max)
            .Select(point => point.Price)
            .ToArray();

        if (recent.Length == 0)
        {
            return (0, 0, 0);
        }

        var mean = recent.Average();
        var variance = recent.Sum(price => Math.Pow(price - mean, 2)) / recent.Length;
        var stdDev = Math.Sqrt(variance);
        return (mean, stdDev, recent.Length);
    }

    private static string ToQueryValue(TimeSeriesTimestep timestep)
    {
        return timestep switch
        {
            TimeSeriesTimestep.FiveMinutes => "5m",
            TimeSeriesTimestep.OneHour => "1h",
            TimeSeriesTimestep.SixHours => "6h",
            _ => "24h"
        };
    }

    private static TimeSpan GetCacheTtl(TimeSeriesTimestep timestep)
    {
        return timestep switch
        {
            TimeSeriesTimestep.FiveMinutes => TimeSpan.FromMinutes(3),
            TimeSeriesTimestep.OneHour => TimeSpan.FromMinutes(30),
            TimeSeriesTimestep.SixHours => TimeSpan.FromHours(2),
            _ => TimeSpan.FromHours(6)
        };
    }

    private static double? GetRepresentativePrice(double? high, double? low)
    {
        if (high.HasValue && low.HasValue)
        {
            return (high.Value + low.Value) / 2.0;
        }

        return high ?? low;
    }

    private static void ApplyUserAgent(HttpClient client, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return;
        }

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.Remove("User-Agent");

        try
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }
        catch (FormatException)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        }
    }

    private sealed record CacheEntry(DateTimeOffset FetchedAt, List<PricePoint> Points);

    private sealed class TimeSeriesResponse
    {
        public List<TimeSeriesPoint>? Data { get; set; }
        public int ItemId { get; set; }
    }

    private sealed class TimeSeriesPoint
    {
        public long Timestamp { get; set; }
        public double? AvgHighPrice { get; set; }
        public double? AvgLowPrice { get; set; }
        public double? HighPriceVolume { get; set; }
        public double? LowPriceVolume { get; set; }
    }
}
