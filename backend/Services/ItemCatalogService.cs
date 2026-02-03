using System.Text.Json;

namespace OSRSGeMonitor.Api.Services;

public class ItemCatalogService
{
    private const string MappingUrl = "https://prices.runescape.wiki/api/v1/osrs/mapping";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ItemCatalogService> _logger;
    private readonly InMemoryDataStore _dataStore;
    private readonly object _lock = new();
    private DateTimeOffset _lastLoaded = DateTimeOffset.MinValue;
    private List<ItemCatalogEntry> _items = new();
    private string? _lastError;

    public ItemCatalogService(
        IHttpClientFactory httpClientFactory,
        ILogger<ItemCatalogService> logger,
        InMemoryDataStore dataStore)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dataStore = dataStore;
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

    public async Task<ItemCatalogEntry?> FindByNameAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        await EnsureLoadedAsync(cancellationToken);
        var trimmed = name.Trim();
        lock (_lock)
        {
            return _items.FirstOrDefault(item =>
                item.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<ItemCatalogEntry?> FindByNameFuzzyAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        await EnsureLoadedAsync(cancellationToken);
        var normalized = NormalizeName(name);
        if (normalized.Length == 0)
        {
            return null;
        }

        lock (_lock)
        {
            if (_items.Count == 0)
            {
                return null;
            }

            ItemCatalogEntry? best = null;
            var bestScore = -1;
            var bestLengthDelta = int.MaxValue;

            foreach (var item in _items)
            {
                var candidate = NormalizeName(item.Name);
                if (candidate.Length == 0)
                {
                    continue;
                }

                var score = ScoreMatch(normalized, candidate);
                if (score < 0)
                {
                    continue;
                }

                var lengthDelta = Math.Abs(candidate.Length - normalized.Length);
                if (score > bestScore || (score == bestScore && lengthDelta < bestLengthDelta))
                {
                    best = item;
                    bestScore = score;
                    bestLengthDelta = lengthDelta;
                }
            }

            return best;
        }
    }

    public CatalogStatus GetStatus()
    {
        lock (_lock)
        {
            return new CatalogStatus(_items.Count, _lastLoaded, _lastError);
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
            ApplyUserAgent(client, _dataStore.Config.UserAgent);

            using var response = await client.GetAsync(MappingUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var items = await JsonSerializer.DeserializeAsync<List<ItemCatalogEntry>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (items is null)
            {
                lock (_lock)
                {
                    _lastError = "Catalog payload was empty.";
                }
                return;
            }

            var sanitized = items
                .Where(item => item.Id > 0 && !string.IsNullOrWhiteSpace(item.Name))
                .ToList();
            var dropped = items.Count - sanitized.Count;
            if (dropped > 0)
            {
                var sample = items
                    .Where(item => item.Id <= 0 || string.IsNullOrWhiteSpace(item.Name))
                    .Take(5)
                    .Select(item => $"{item.Id}:{item.Name}")
                    .ToArray();
                _logger.LogWarning(
                    "Dropped {DroppedCount} invalid catalog entries (raw={RawCount}, kept={KeptCount}). Sample: {Sample}",
                    dropped,
                    items.Count,
                    sanitized.Count,
                    string.Join(", ", sample));
            }

            lock (_lock)
            {
                _items = sanitized;
                _lastLoaded = DateTimeOffset.UtcNow;
                _lastError = null;
            }
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _lastError = ex.Message;
            }
            _logger.LogError(ex, "Failed to load OSRS item catalog.");
        }
    }

    public sealed class ItemCatalogEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Examine { get; set; } = string.Empty;
        public bool Members { get; set; }
        public int? Lowalch { get; set; }
        public int? Highalch { get; set; }
        public int? Limit { get; set; }
        public int? Value { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public sealed record CatalogStatus(int Count, DateTimeOffset LastLoaded, string? LastError);

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

    private static string NormalizeName(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(trimmed.Length);
        var wasSpace = false;
        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!wasSpace)
                {
                    builder.Append(' ');
                    wasSpace = true;
                }
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
            wasSpace = false;
        }

        return builder.ToString();
    }

    private static int ScoreMatch(string query, string candidate)
    {
        if (candidate.Equals(query, StringComparison.Ordinal))
        {
            return 100;
        }

        if (candidate.StartsWith(query, StringComparison.Ordinal))
        {
            return 80;
        }

        if (candidate.Contains(query, StringComparison.Ordinal))
        {
            return 60;
        }

        return -1;
    }
}
