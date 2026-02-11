namespace OSRSTools.Core.Configuration;

public class CacheSettings
{
    public TimeSpan MappingDuration { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan PriceDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan VolumeDuration { get; set; } = TimeSpan.FromHours(1);
}
