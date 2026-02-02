using System.Net.Http.Headers;
using System.Text.Json;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public class PriceMonitorService : BackgroundService
{
    private const string ApiUrl = "https://prices.runescape.wiki/api/v1/osrs/latest";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InMemoryDataStore _dataStore;
    private readonly ILogger<PriceMonitorService> _logger;

    public PriceMonitorService(
        IHttpClientFactory httpClientFactory,
        InMemoryDataStore dataStore,
        ILogger<PriceMonitorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dataStore = dataStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to fetch OSRS price data.");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(5, _dataStore.Config.FetchIntervalSeconds));
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task FetchAndProcessAsync(CancellationToken stoppingToken)
    {
        var items = _dataStore.GetItems();
        if (items.Count == 0)
        {
            return;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OSRS-GE-Monitor", "1.0"));

        using var response = await client.GetAsync(ApiUrl, stoppingToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(stoppingToken);
        var payload = await JsonSerializer.DeserializeAsync<PriceApiResponse>(stream, cancellationToken: stoppingToken);
        if (payload?.Data is null)
        {
            return;
        }

        foreach (var item in items)
        {
            if (!payload.Data.TryGetValue(item.Id.ToString(), out var priceData))
            {
                continue;
            }

            var price = priceData.GetRepresentativePrice();
            if (price is null)
            {
                continue;
            }

            _dataStore.AddPricePoint(item.Id, new PricePoint(DateTimeOffset.UtcNow, price.Value));
            var (mean, stdDev, sampleSize) = _dataStore.GetRollingStats(item.Id);
            if (sampleSize < 2 || stdDev <= 0)
            {
                continue;
            }

            var dropThreshold = mean - (_dataStore.Config.StandardDeviationThreshold * stdDev);
            if (price.Value < dropThreshold && !_dataStore.HasActiveAlert(item.Id))
            {
                _dataStore.AddAlert(new Alert
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    TriggerPrice = price.Value,
                    Mean = mean,
                    StandardDeviation = stdDev
                });
                continue;
            }

            var recoveryThreshold = mean - (_dataStore.Config.RecoveryStandardDeviationThreshold * stdDev);
            if (price.Value >= recoveryThreshold)
            {
                _dataStore.TryRecoverAlert(item.Id, price.Value);
            }
        }
    }

    private sealed class PriceApiResponse
    {
        public Dictionary<string, PriceApiItem>? Data { get; set; }
    }

    private sealed class PriceApiItem
    {
        public double? High { get; set; }
        public double? Low { get; set; }

        public double? GetRepresentativePrice()
        {
            if (High.HasValue && Low.HasValue)
            {
                return (High.Value + Low.Value) / 2.0;
            }

            return High ?? Low;
        }
    }
}
