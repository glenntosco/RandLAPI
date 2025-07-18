namespace RandLAPI.Settings;

public class ServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int MaxRecordsPerCheck { get; set; } = 100;
    public int CheckIntervalSeconds { get; set; } = 30;
}