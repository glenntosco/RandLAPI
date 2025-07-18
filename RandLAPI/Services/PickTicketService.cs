using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RandLAPI.Models;
using RandLAPI.Settings;

namespace RandLAPI.Services;

public interface IPickTicketService
{
    Task<IEnumerable<PickTicket>> GetEligiblePickTicketsAsync();
    Task<bool> UpdatePickTicketProNumberAsync(Guid id, string proNumber);
}

public class PickTicketService : IPickTicketService
{
    private readonly HttpClient _httpClient;
    private readonly ServiceSettings _settings;
    private readonly ILogger<PickTicketService> _logger;

    public PickTicketService(HttpClient httpClient, IOptions<ServiceSettings> settings, ILogger<PickTicketService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("ApiKey", _settings.ApiKey);
    }

    public async Task<IEnumerable<PickTicket>> GetEligiblePickTicketsAsync()
    {
        try
        {
            var filterValue = "Carrier eq 'R&L CARRIERS' and (PickTicketState eq 'ReadyToPick' or PickTicketState eq 'Waved') and (ProNumber eq null or length(ProNumber) eq 0)";
            var encodedFilter = "$filter=" + Uri.EscapeDataString(filterValue);
            var top = "$top=" + _settings.MaxRecordsPerCheck;
            var select = "$select=Id,PickTicketNumber,ProNumber,Carrier,PickTicketState";
            var requestUri = $"odata/PickTicket?{select}&{encodedFilter}&{top}";

            _logger.LogDebug("Fetching eligible PickTickets from: {RequestUri}", requestUri);

            var response = await _httpClient.GetAsync(requestUri);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("P4W API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var oDataResponse = JsonConvert.DeserializeObject<ODataResponse<PickTicket>>(jsonContent);

            _logger.LogInformation("Retrieved {Count} eligible PickTickets", oDataResponse?.Value?.Count() ?? 0);
            return oDataResponse?.Value ?? Enumerable.Empty<PickTicket>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching eligible PickTickets");
            return Enumerable.Empty<PickTicket>();
        }
    }

    public async Task<bool> UpdatePickTicketProNumberAsync(Guid id, string proNumber)
    {
        try
        {
            var updateRequest = new PickTicketUpdateRequest
            {
                Id = id,
                ProNumber = proNumber
            };

            var json = JsonConvert.SerializeObject(updateRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Updating PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);

            var response = await _httpClient.PostAsync("api/PickTicketApi/CreateOrUpdate", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PickTicket {Id} with ProNumber {ProNumber}", id, proNumber);
            return false;
        }
    }
}

public class ODataResponse<T>
{
    public IEnumerable<T> Value { get; set; } = Enumerable.Empty<T>();
}