using OSRSGeMonitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
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
builder.Services.AddSingleton<InMemoryDataStore>();
builder.Services.AddSingleton<ItemCatalogService>();
builder.Services.AddSingleton<OsrsTimeSeriesService>();
builder.Services.AddHostedService<PriceMonitorService>();

var app = builder.Build();

app.UseCors("DevCors");
app.MapControllers();

app.Run();
