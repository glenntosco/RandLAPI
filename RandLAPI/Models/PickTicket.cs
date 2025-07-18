namespace RandLAPI.Models;

/// <summary>
/// Represents a PickTicket from P4 Warehouse
/// A PickTicket is a warehouse document that lists items to be picked and shipped
/// This model contains the basic information we need for BOL creation
/// </summary>
public class PickTicket
{
    /// <summary>
    /// Unique identifier for this PickTicket in P4 Warehouse
    /// This is used to update the PickTicket after BOL creation
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Human-readable PickTicket number (e.g., "PT-12345")
    /// Used for logging and identification purposes
    /// </summary>
    public string PickTicketNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Tracking number (ProNumber) from the carrier
    /// This will be null/empty until a BOL is created successfully
    /// Once populated, this PickTicket should not be processed again
    /// </summary>
    public string? ProNumber { get; set; }
    
    /// <summary>
    /// Name of the shipping carrier (e.g., "R&L CARRIERS")
    /// We filter PickTickets to only process ones with specific carriers
    /// </summary>
    public string Carrier { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the PickTicket (e.g., "ReadyToPick", "Waved", "Closed")
    /// We only process PickTickets in certain states (ReadyToPick, Waved)
    /// </summary>
    public string PickTicketState { get; set; } = string.Empty;
}