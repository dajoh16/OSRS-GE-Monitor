using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OSRS.GE.Monitor.Backend.Data;

public static class TimeSeriesRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddTimeSeriesRepository(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["TimeSeriesRepository:Provider"] ?? "InMemory";
        var connectionString = configuration["TimeSeriesRepository:ConnectionString"];

        services.AddSingleton(new TimeSeriesRepositoryOptions
        {
            Provider = provider,
            ConnectionString = connectionString
        });

        return provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            ? services.AddSingleton<ITimeSeriesRepository>(_ => new PostgresTimeSeriesRepository(
                connectionString ?? throw new InvalidOperationException("Postgres connection string is required.")))
            : services.AddSingleton<ITimeSeriesRepository, InMemoryTimeSeriesRepository>();
    }
}
