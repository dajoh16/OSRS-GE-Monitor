namespace OSRSGeMonitor.Api.Models;

public class GlobalConfig
{
    public double StandardDeviationThreshold { get; set; } = 2.0;
    public double RecoveryStandardDeviationThreshold { get; set; } = 0.75;
    public int RollingWindowSize { get; set; } = 30;
    public int FetchIntervalSeconds { get; set; } = 60;
}
