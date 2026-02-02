namespace OSRSGeMonitor.Api.Models.Requests;

public sealed record UpdateConfigRequest
{
    public double? StandardDeviationThreshold { get; init; }
    public double? ProfitTargetPercent { get; init; }
    public double? RecoveryStandardDeviationThreshold { get; init; }
    public int? RollingWindowSize { get; init; }
    public int? FetchIntervalSeconds { get; init; }
    public string? UserAgent { get; init; }
    public bool? DiscordNotificationsEnabled { get; init; }
    public string? DiscordWebhookUrl { get; init; }
}
