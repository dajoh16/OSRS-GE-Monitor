namespace OSRSGeMonitor.Api.Models.Requests;

public class UpdateConfigRequest
{
    public double? StandardDeviationThreshold { get; set; }
    public double? ProfitTargetPercent { get; set; }
    public double? RecoveryStandardDeviationThreshold { get; set; }
    public int? RollingWindowSize { get; set; }
    public int? FetchIntervalSeconds { get; set; }
    public string? UserAgent { get; set; }
}
