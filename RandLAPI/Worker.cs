using Microsoft.Extensions.Options;
using RandLAPI.Services;
using RandLAPI.Settings;

namespace RandLAPI;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPickTicketService _pickTicketService;
    private readonly IRLCarrierService _rlCarrierService;
    private readonly ServiceSettings _settings;

    public Worker(
        ILogger<Worker> logger, 
        IPickTicketService pickTicketService, 
        IRLCarrierService rlCarrierService,
        IOptions<ServiceSettings> settings)
    {
        _logger = logger;
        _pickTicketService = pickTicketService;
        _rlCarrierService = rlCarrierService;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("R&L Carrier BOL Integration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPickTicketsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("R&L Carrier BOL Integration Service stopped");
    }

    private async Task ProcessPickTicketsAsync()
    {
        _logger.LogDebug("Starting PickTicket processing cycle");

        var eligiblePickTickets = await _pickTicketService.GetEligiblePickTicketsAsync();
        
        if (!eligiblePickTickets.Any())
        {
            _logger.LogDebug("No eligible PickTickets found");
            return;
        }

        _logger.LogInformation("Processing {Count} eligible PickTickets", eligiblePickTickets.Count());

        foreach (var pickTicket in eligiblePickTickets)
        {
            try
            {
                var proNumber = await _rlCarrierService.CreateBillOfLadingAsync(pickTicket);
                
                if (!string.IsNullOrEmpty(proNumber))
                {
                    var updateSuccess = await _pickTicketService.UpdatePickTicketProNumberAsync(pickTicket.Id, proNumber);
                    
                    if (updateSuccess)
                    {
                        _logger.LogInformation("Successfully processed PickTicket {PickTicketNumber} with ProNumber {ProNumber}", 
                            pickTicket.PickTicketNumber, proNumber);
                    }
                    else
                    {
                        _logger.LogError("Failed to update PickTicket {PickTicketNumber} with ProNumber {ProNumber}", 
                            pickTicket.PickTicketNumber, proNumber);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to create BOL for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
                }

                await Task.Delay(100, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            }
        }

        _logger.LogDebug("Completed PickTicket processing cycle");
    }
}
