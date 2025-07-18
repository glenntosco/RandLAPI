using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RandLAPI.Models;
using RandLAPI.Settings;

namespace RandLAPI.Services;

public interface IRLCarrierService
{
    Task<string?> CreateBillOfLadingAsync(PickTicket pickTicket);
}

public class RLCarrierService : IRLCarrierService
{
    private readonly HttpClient _httpClient;
    private readonly RLCarrierSettings _settings;
    private readonly ILogger<RLCarrierService> _logger;

    public RLCarrierService(HttpClient httpClient, IOptions<RLCarrierSettings> settings, ILogger<RLCarrierService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("apiKey", _settings.ApiKey);
    }

    public async Task<string?> CreateBillOfLadingAsync(PickTicket pickTicket)
    {
        try
        {
            var bolRequest = CreateBOLRequest(pickTicket);
            var json = JsonConvert.SerializeObject(bolRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending BOL request to R&L for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);

            var response = await _httpClient.PostAsync(_settings.Endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("R&L API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var bolResponse = JsonConvert.DeserializeObject<RLCarrierBOLResponse>(responseContent);

            if (bolResponse?.Code == 0 && !string.IsNullOrEmpty(bolResponse.ProNumber))
            {
                _logger.LogInformation("Successfully created BOL for PickTicket {PickTicketNumber}, ProNumber: {ProNumber}", 
                    pickTicket.PickTicketNumber, bolResponse.ProNumber);
                return bolResponse.ProNumber;
            }

            _logger.LogWarning("R&L API returned success but no ProNumber for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating BOL for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            return null;
        }
    }

    private static RLCarrierBOLRequest CreateBOLRequest(PickTicket pickTicket)
    {
        return new RLCarrierBOLRequest
        {
            BillOfLading = new BillOfLading
            {
                BOLDate = DateTime.Now.ToString("MM/dd/yyyy"),
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
                Consignee = new Consignee
                {
                    CompanyName = "Evergreen Logistics",
                    AddressLine1 = "8400 NW 25th St",
                    City = "Doral",
                    StateOrProvince = "FL",
                    ZipOrPostalCode = "33198",
                    CountryCode = "USA"
                },
                Items = new List<Item>
                {
                    new Item
                    {
                        Class = "70",
                        Pieces = 8,
                        Weight = 960,
                        PackageType = "PLT",
                        Description = "Zebra Barcode Scanners and Mobile Computers"
                    }
                }
            }
        };
    }
}