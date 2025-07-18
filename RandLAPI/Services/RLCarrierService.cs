using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RandLAPI.Models;
using RandLAPI.Settings;

namespace RandLAPI.Services;

/// <summary>
/// Interface that defines the contract for R&L Carrier operations
/// This allows us to use dependency injection and makes testing easier
/// </summary>
public interface IRLCarrierService
{
    /// <summary>
    /// Creates a Bill of Lading (BOL) with R&L Carrier for the given PickTicket
    /// Returns the ProNumber (tracking number) if successful, null if failed
    /// </summary>
    Task<string?> CreateBillOfLadingAsync(PickTicket pickTicket);
}

/// <summary>
/// Service class that handles all communication with the R&L Carrier API
/// This class is responsible for:
/// 1. Creating Bills of Lading (BOL) requests
/// 2. Sending BOL data to R&L Carrier
/// 3. Processing responses and extracting ProNumbers (tracking numbers)
/// </summary>
public class RLCarrierService : IRLCarrierService
{
    // Private fields to store our dependencies
    private readonly HttpClient _httpClient;                    // HTTP client for making API calls
    private readonly RLCarrierSettings _settings;              // Configuration settings for R&L Carrier API
    private readonly ILogger<RLCarrierService> _logger;        // Logger for this service

    /// <summary>
    /// Constructor - sets up the service with all its dependencies
    /// The HttpClient is automatically configured by the dependency injection system
    /// </summary>
    public RLCarrierService(HttpClient httpClient, IOptions<RLCarrierSettings> settings, ILogger<RLCarrierService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;      // Extract settings from the IOptions wrapper
        _logger = logger;
        
        // Configure the HTTP client with the base URL and API key for R&L Carrier
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("apiKey", _settings.ApiKey);
    }

    /// <summary>
    /// Creates a Bill of Lading (BOL) with R&L Carrier for the specified PickTicket
    /// This method builds the BOL request, sends it to R&L, and returns the tracking number
    /// </summary>
    /// <param name="pickTicket">The PickTicket to create a BOL for</param>
    /// <returns>The ProNumber (tracking number) if successful, null if failed</returns>
    public async Task<string?> CreateBillOfLadingAsync(PickTicket pickTicket)
    {
        try
        {
            // Step 1: Create the BOL request object with all required shipping information
            var bolRequest = CreateBOLRequest(pickTicket);
            
            // Step 2: Convert the request object to JSON format
            var json = JsonConvert.SerializeObject(bolRequest);
            
            // Step 3: Create HTTP content with the JSON data
            // We specify UTF-8 encoding and application/json content type
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending BOL request to R&L for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);

            // Step 4: Send the POST request to R&L Carrier's BOL API
            var response = await _httpClient.PostAsync(_settings.Endpoint, content);
            
            // Step 5: Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                // If not successful, log the error details and return null
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("R&L API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                return null;  // Indicate failure
            }

            // Step 6: Read and parse the response from R&L Carrier
            var responseContent = await response.Content.ReadAsStringAsync();
            var bolResponse = JsonConvert.DeserializeObject<RLCarrierBOLResponse>(responseContent);

            // Step 7: Validate the response and extract the ProNumber
            // R&L returns Code=0 for success, and we need a valid ProNumber
            if (bolResponse?.Code == 0 && !string.IsNullOrEmpty(bolResponse.ProNumber))
            {
                _logger.LogInformation("Successfully created BOL for PickTicket {PickTicketNumber}, ProNumber: {ProNumber}", 
                    pickTicket.PickTicketNumber, bolResponse.ProNumber);
                return bolResponse.ProNumber;  // Return the tracking number
            }

            // If we get here, the API call succeeded but we didn't get a valid ProNumber
            _logger.LogWarning("R&L API returned success but no ProNumber for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            return null;  // Indicate failure
        }
        catch (Exception ex)
        {
            // If any error occurs, log it and return null
            // This allows the calling code to handle the failure gracefully
            _logger.LogError(ex, "Error creating BOL for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            return null;  // Indicate failure
        }
    }

    /// <summary>
    /// Creates a BOL request object with all the required shipping information
    /// This method constructs the complete BOL data structure that R&L Carrier expects
    /// 
    /// NOTE: Currently uses hardcoded shipper/consignee data - in a real system,
    /// this would be populated from the PickTicket or configuration
    /// </summary>
    /// <param name="pickTicket">The PickTicket to create BOL data for (currently not used for data)</param>
    /// <returns>A complete BOL request object ready to send to R&L Carrier</returns>
    private static RLCarrierBOLRequest CreateBOLRequest(PickTicket pickTicket)
    {
        return new RLCarrierBOLRequest
        {
            BillOfLading = new BillOfLading
            {
                // Set the BOL date to today in MM/dd/yyyy format
                BOLDate = DateTime.Now.ToString("MM/dd/yyyy"),
                
                // Shipper information (where the shipment is coming from)
                // TODO: In a real system, this should be configurable or pulled from PickTicket data
                Shipper = new Shipper
                {
                    CompanyName = "P4 Software Inc.",
                    AddressLine1 = "3755 Breakthrough Way",
                    City = "Las Vegas",
                    StateOrProvince = "NV",
                    ZipOrPostalCode = "89135",
                    CountryCode = "USA",
                    PhoneNumber = "702-555-0101"
                },
                
                // Consignee information (where the shipment is going to)
                // TODO: In a real system, this should be pulled from PickTicket data
                Consignee = new Consignee
                {
                    CompanyName = "Evergreen Logistics",
                    AddressLine1 = "8400 NW 25th St",
                    City = "Doral",
                    StateOrProvince = "FL",
                    ZipOrPostalCode = "33198",
                    CountryCode = "USA"
                },
                
                // Items being shipped
                // TODO: In a real system, this should be pulled from PickTicket line items
                Items = new List<Item>
                {
                    new Item
                    {
                        Class = "70",                                           // Freight class (affects pricing)
                        Pieces = 8,                                             // Number of pieces/packages
                        Weight = 960,                                           // Total weight in pounds
                        PackageType = "PLT",                                    // Package type (PLT = Pallet)
                        Description = "Zebra Barcode Scanners and Mobile Computers"  // Description of contents
                    }
                }
            }
        };
    }
}