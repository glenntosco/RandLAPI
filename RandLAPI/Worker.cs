using Microsoft.Extensions.Options;
using RandLAPI.Services;
using RandLAPI.Settings;

namespace RandLAPI;

/// <summary>
/// The main background worker service that continuously processes PickTickets
/// This class inherits from BackgroundService, which provides the framework for long-running tasks
/// It runs continuously until the application is stopped
/// </summary>
public class Worker : BackgroundService
{
    // Private readonly fields to store our dependencies
    // These are injected through the constructor (Dependency Injection pattern)
    private readonly ILogger<Worker> _logger;                    // For logging information, warnings, and errors
    private readonly IPickTicketService _pickTicketService;      // Service to interact with P4 Warehouse API
    private readonly IRLCarrierService _rlCarrierService;        // Service to interact with R&L Carrier API
    private readonly ServiceSettings _settings;                 // Configuration settings for our service

    /// <summary>
    /// Constructor - called when the service is created
    /// All parameters are automatically provided by the dependency injection container
    /// </summary>
    public Worker(
        ILogger<Worker> logger, 
        IPickTicketService pickTicketService, 
        IRLCarrierService rlCarrierService,
        IOptions<ServiceSettings> settings)     // IOptions wrapper allows us to access configuration
    {
        // Store the injected dependencies in our private fields
        _logger = logger;
        _pickTicketService = pickTicketService;
        _rlCarrierService = rlCarrierService;
        _settings = settings.Value;              // Extract the actual settings from the IOptions wrapper
    }

    /// <summary>
    /// Main execution method - this runs when the background service starts
    /// The method continues running until the application is stopped
    /// </summary>
    /// <param name="stoppingToken">Token that signals when the service should stop</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Log that our service has started
        _logger.LogInformation("R&L Carrier BOL Integration Service started");

        // Main processing loop - continues until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process all eligible PickTickets in this cycle
                await ProcessPickTicketsAsync();
            }
            catch (Exception ex)
            {
                // If any unhandled error occurs, log it but don't crash the service
                // This ensures the service keeps running even if there are temporary issues
                _logger.LogError(ex, "Unhandled error in processing cycle");
            }

            // Wait for the configured interval before starting the next processing cycle
            // This prevents the service from running continuously and overwhelming the APIs
            await Task.Delay(TimeSpan.FromSeconds(_settings.CheckIntervalSeconds), stoppingToken);
        }

        // Log that our service has stopped (this happens when the application shuts down)
        _logger.LogInformation("R&L Carrier BOL Integration Service stopped");
    }

    /// <summary>
    /// Private method that handles the core business logic for processing PickTickets
    /// This method is called once per processing cycle
    /// </summary>
    private async Task ProcessPickTicketsAsync()
    {
        // Log the start of this processing cycle (Debug level - only shown when debugging)
        _logger.LogDebug("Starting PickTicket processing cycle");

        // Step 1: Get all PickTickets that need BOL creation from P4 Warehouse
        var eligiblePickTickets = await _pickTicketService.GetEligiblePickTicketsAsync();
        
        // If no PickTickets were found, log it and exit early
        if (!eligiblePickTickets.Any())
        {
            _logger.LogDebug("No eligible PickTickets found");
            return;  // Exit the method early - nothing to process
        }

        // Log how many PickTickets we found to process
        _logger.LogInformation("Processing {Count} eligible PickTickets", eligiblePickTickets.Count());

        // Step 2: Process each PickTicket individually
        foreach (var pickTicket in eligiblePickTickets)
        {
            try
            {
                // Step 2a: Create a BOL (Bill of Lading) with R&L Carrier for this PickTicket
                var proNumber = await _rlCarrierService.CreateBillOfLadingAsync(pickTicket);
                
                // Step 2b: Check if we successfully got a ProNumber (tracking number) back
                if (!string.IsNullOrEmpty(proNumber))
                {
                    // Step 2c: Update the PickTicket in P4 Warehouse with the new ProNumber
                    var updateSuccess = await _pickTicketService.UpdatePickTicketProNumberAsync(pickTicket.Id, proNumber);
                    
                    // Log the result of the update operation
                    if (updateSuccess)
                    {
                        // Success! Both BOL creation and P4W update worked
                        _logger.LogInformation("Successfully processed PickTicket {PickTicketNumber} with ProNumber {ProNumber}", 
                            pickTicket.PickTicketNumber, proNumber);
                    }
                    else
                    {
                        // BOL was created but P4W update failed - this needs attention
                        _logger.LogError("Failed to update PickTicket {PickTicketNumber} with ProNumber {ProNumber}", 
                            pickTicket.PickTicketNumber, proNumber);
                    }
                }
                else
                {
                    // BOL creation failed - log a warning and continue with the next PickTicket
                    _logger.LogWarning("Failed to create BOL for PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
                }

                // Wait 100 milliseconds before processing the next PickTicket
                // This prevents overwhelming the APIs with too many requests at once
                await Task.Delay(100, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // If processing this specific PickTicket fails, log the error and continue with the next one
                // This ensures one bad PickTicket doesn't stop processing of all others
                _logger.LogError(ex, "Error processing PickTicket {PickTicketNumber}", pickTicket.PickTicketNumber);
            }
        }

        // Log that we've completed this processing cycle
        _logger.LogDebug("Completed PickTicket processing cycle");
    }
}
