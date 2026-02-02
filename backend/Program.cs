using OSRSGeMonitor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<InMemoryDataStore>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<PriceMonitorService>();

var app = builder.Build();

app.MapControllers();

app.Run();
