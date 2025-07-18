namespace RandLAPI.Settings;

/// <summary>
/// Configuration settings for P4 Warehouse API integration
/// These settings control how our service connects to and interacts with P4W
/// Values come from appsettings.json under the "ServiceSettings" section
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Base URL for the P4 Warehouse API (e.g., "https://nadc218demo.p4warehouse.com/")
    /// This is the root URL where all P4W API calls will be made
    /// Should include the trailing slash
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API key for authenticating with P4 Warehouse
    /// This is sent in the "ApiKey" header with every request to P4W
    /// Should be kept secure and not logged
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum number of PickTickets to retrieve in a single API call
    /// Prevents overwhelming the system with too much data at once
    /// Default: 100 (good balance between efficiency and performance)
    /// </summary>
    public int MaxRecordsPerCheck { get; set; } = 100;
    
    /// <summary>
    /// How often (in seconds) to check for new PickTickets
    /// Controls the frequency of our processing cycles
    /// Default: 30 seconds (good for responsive processing without overloading APIs)
    /// Production recommendation: 60-300 seconds depending on volume
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 30;
}