namespace OSRSGeMonitor.Api.Models;

public class GlobalConfig
{
    public double StandardDeviationThreshold { get; set; } = 3.5;
    public double ProfitTargetPercent { get; set; } = 0.02;
    public double RecoveryStandardDeviationThreshold { get; set; } = 0.75;
    public int RollingWindowSize { get; set; } = 30;
    public int FetchIntervalSeconds { get; set; } = 60;
    public string UserAgent { get; set; } = "OSRS-GE-Monitor/1.0 (contact: Discord danmarkdan#6784; GitHub dajoh16)";
    public bool DiscordNotificationsEnabled { get; set; }
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public int AlertGraceMinutes { get; set; } = 10;
}
