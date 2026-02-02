using OSRSGeMonitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SqlitePriceHistoryStore>();
builder.Services.AddSingleton<InMemoryDataStore>();
builder.Services.AddSingleton<ItemCatalogService>();
builder.Services.AddHostedService<PriceMonitorService>();

var app = builder.Build();

app.MapControllers();

app.Run();
