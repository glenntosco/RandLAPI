namespace RandLAPI.Settings;

/// <summary>
/// Configuration settings for R&L Carrier API integration
/// These settings control how our service connects to and interacts with R&L Carrier
/// Values come from appsettings.json under the "RLCarrierSettings" section
/// </summary>
public class RLCarrierSettings
{
    /// <summary>
    /// Base URL for the R&L Carrier API (e.g., "https://apisandbox.rlc.com" for testing)
    /// Production would use "https://api.rlc.com"
    /// This is the root URL where all R&L API calls will be made
    /// Should NOT include trailing slash
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API key for authenticating with R&L Carrier
    /// This is sent in the "apiKey" header with every request to R&L
    /// Should be kept secure and not logged
    /// Different from P4W API key format
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API endpoint path for creating Bills of Lading
    /// Default: "/BillOfLading" (R&L's standard BOL creation endpoint)
    /// Combined with BaseUrl to create the full API URL
    /// Should start with "/" but not end with one
    /// </summary>
    public string Endpoint { get; set; } = "/BillOfLading";
}