using OSRSGeMonitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<SqliteWatchlistStore>();
builder.Services.AddSingleton<SqliteTimeSeriesCacheStore>();
builder.Services.AddSingleton<SqliteConfigStore>();
builder.Services.AddSingleton<SqlitePositionStore>();
builder.Services.AddSingleton<InMemoryDataStore>();
builder.Services.AddSingleton<ItemCatalogService>();
builder.Services.AddSingleton<OsrsTimeSeriesService>();
builder.Services.AddSingleton<DiscordNotificationQueue>();
builder.Services.AddSingleton<DiscordNotificationService>();
builder.Services.AddHostedService<PriceMonitorService>();
builder.Services.AddHostedService<DiscordNotificationWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("DevCors");
app.MapControllers();

await app.Services.GetRequiredService<SqliteConfigStore>().InitializeAsync();
await app.Services.GetRequiredService<SqlitePositionStore>().InitializeAsync();
var dataStore = app.Services.GetRequiredService<InMemoryDataStore>();
await dataStore.LoadConfigAsync();
await dataStore.LoadPositionsAsync();

await app.RunAsync();

public partial class Program { }
