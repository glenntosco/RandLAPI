using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RandLAPI.Models;
using RandLAPI.Settings;

namespace RandLAPI.Services;

/// <summary>
/// Interface that defines the contract for PickTicket operations
/// This allows us to use dependency injection and makes testing easier
/// </summary>
public interface IPickTicketService
{
    /// <summary>
    /// Gets all PickTickets that are eligible for BOL creation
    /// </summary>
    Task<IEnumerable<PickTicket>> GetEligiblePickTicketsAsync();
    
    /// <summary>
    /// Updates a PickTicket with the ProNumber (tracking number) received from R&L Carrier
    /// </summary>
    Task<bool> UpdatePickTicketProNumberAsync(Guid id, string proNumber);
}

/// <summary>
/// Service class that handles all communication with the P4 Warehouse API
/// This class is responsible for:
/// 1. Fetching PickTickets that need BOL creation
/// 2. Updating PickTickets with ProNumbers after successful BOL creation
/// </summary>
public class PickTicketService : IPickTicketService
{
    // Private fields to store our dependencies
    private readonly HttpClient _httpClient;                // HTTP client for making API calls
    private readonly ServiceSettings _settings;             // Configuration settings
    private readonly ILogger<PickTicketService> _logger;    // Logger for this service

    /// <summary>
    /// Constructor - sets up the service with all its dependencies
    /// The HttpClient is automatically configured by the dependency injection system
    /// </summary>
    public PickTicketService(HttpClient httpClient, IOptions<ServiceSettings> settings, ILogger<PickTicketService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;      // Extract settings from the IOptions wrapper
        _logger = logger;
        
        // Configure the HTTP client with the base URL and API key for P4 Warehouse
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("ApiKey", _settings.ApiKey);
    }

    /// <summary>
    /// Fetches all PickTickets that are eligible for BOL creation from P4 Warehouse
    /// Uses OData queries to filter the results on the server side
    /// </summary>
    public async Task<IEnumerable<PickTicket>> GetEligiblePickTicketsAsync()
    {
        try
        {
            // Build the OData filter criteria
            // We want PickTickets that:
            // 1. Have R&L CARRIERS as the carrier
            // 2. Are in ReadyToPick or Waved state (ready for shipping)
            // 3. Don't already have a ProNumber (haven't been processed yet)
            var filterValue = "Carrier eq 'R&L CARRIERS' and (PickTicketState eq 'ReadyToPick' or PickTicketState eq 'Waved') and (ProNumber eq null or length(ProNumber) eq 0)";
            
            // URL encode the filter to handle special characters safely
            var encodedFilter = "$filter=" + Uri.EscapeDataString(filterValue);
            
            // Limit the number of records returned to prevent overwhelming the system
            var top = "$top=" + _settings.MaxRecordsPerCheck;
            
            // Only select the fields we actually need to reduce data transfer
            var select = "$select=Id,PickTicketNumber,ProNumber,Carrier,PickTicketState";
            
            // Combine all the OData query parameters into the final URL
            var requestUri = $"odata/PickTicket?{select}&{encodedFilter}&{top}";

            _logger.LogDebug("Fetching eligible PickTickets from: {RequestUri}", requestUri);

            // Make the HTTP GET request to P4 Warehouse
            var response = await _httpClient.GetAsync(requestUri);
            
            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                // If not successful, log the error details and throw an exception
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("P4W API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();  // This will throw an exception
            }

            // Read the JSON response from the API
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            // Deserialize the JSON into our ODataResponse object
            // OData wraps results in a "value" property, so we need a wrapper class
            var oDataResponse = JsonConvert.DeserializeObject<ODataResponse<PickTicket>>(jsonContent);

            // Log how many records we retrieved
            _logger.LogInformation("Retrieved {Count} eligible PickTickets", oDataResponse?.Value?.Count() ?? 0);
            
            // Return the actual PickTickets, or an empty collection if something went wrong
            return oDataResponse?.Value ?? Enumerable.Empty<PickTicket>();
        }
        catch (Exception ex)
        {
            // If any error occurs, log it and return an empty collection
            // This prevents the entire service from crashing due to API issues
            _logger.LogError(ex, "Error fetching eligible PickTickets");
            return Enumerable.Empty<PickTicket>();
        }
    }

    /// <summary>
    /// Updates a specific PickTicket with the ProNumber received from R&L Carrier
    /// This saves the tracking number back to P4 Warehouse so it can be used for shipment tracking
    /// </summary>
    /// <param name="id">The unique ID of the PickTicket to update</param>
    /// <param name="proNumber">The tracking number received from R&L Carrier</param>
    /// <returns>True if the update was successful, false otherwise</returns>
    public async Task<bool> UpdatePickTicketProNumberAsync(Guid id, string proNumber)
    {
        try
        {
            // Create the update request object with the PickTicket ID and new ProNumber
            var updateRequest = new PickTicketUpdateRequest
            {
                Id = id,
                ProNumber = proNumber
            };

            // Convert the request object to JSON format
            var json = JsonConvert.SerializeObject(updateRequest);
            
            // Create HTTP content with the JSON data
            // We specify UTF-8 encoding and application/json content type
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Updating PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);

            // Send the POST request to P4 Warehouse's CreateOrUpdate API endpoint
            var response = await _httpClient.PostAsync("api/PickTicketApi/CreateOrUpdate", content);
            
            // Throw an exception if the request wasn't successful
            // This will be caught by the catch block below
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);
            return true;  // Success!
        }
        catch (Exception ex)
        {
            // If any error occurs, log it and return false
            // The calling code can then decide how to handle the failure
            _logger.LogError(ex, "Error updating PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);
            return false;  // Failure
        }
    }
}

/// <summary>
/// Helper class to deserialize OData API responses
/// OData APIs wrap their results in a "value" property, so we need this wrapper
/// The generic type T allows us to use this for any type of data (PickTicket, etc.)
/// </summary>
public class ODataResponse<T>
{
    /// <summary>
    /// The actual data returned by the OData API
    /// This will be an array/collection of the requested objects
    /// </summary>
    public IEnumerable<T> Value { get; set; } = Enumerable.Empty<T>();
}