using System.Net.Http.Headers;
using System.Text.Json;

namespace OSRSGeMonitor.Api.Services;

public class ItemCatalogService
{
    private const string MappingUrl = "https://prices.runescape.wiki/api/v1/osrs/mapping";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ItemCatalogService> _logger;
    private readonly object _lock = new();
    private DateTimeOffset _lastLoaded = DateTimeOffset.MinValue;
    private List<ItemCatalogEntry> _items = new();

    public ItemCatalogService(IHttpClientFactory httpClientFactory, ILogger<ItemCatalogService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<ItemCatalogEntry>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ItemCatalogEntry>();
        }

        await EnsureLoadedAsync(cancellationToken);
        lock (_lock)
        {
            return _items
                .Where(item => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Name)
                .Take(25)
                .ToArray();
        }
    }

    public async Task<ItemCatalogEntry?> FindByIdAsync(int itemId, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);
        lock (_lock)
        {
            return _items.FirstOrDefault(item => item.Id == itemId);
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow - _lastLoaded < CacheDuration && _items.Count > 0)
        {
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OSRS-GE-Monitor", "1.0"));

            using var response = await client.GetAsync(MappingUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var items = await JsonSerializer.DeserializeAsync<List<ItemCatalogEntry>>(stream, cancellationToken: cancellationToken);

            if (items is null)
            {
                return;
            }

            lock (_lock)
            {
                _items = items;
                _lastLoaded = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OSRS item catalog.");
        }
    }

    public sealed class ItemCatalogEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
