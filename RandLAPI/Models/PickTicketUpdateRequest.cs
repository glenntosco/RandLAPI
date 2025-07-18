namespace RandLAPI.Models;

/// <summary>
/// Request model for updating a PickTicket in P4 Warehouse
/// This is sent to P4W's CreateOrUpdate API to save the ProNumber after BOL creation
/// </summary>
public class PickTicketUpdateRequest
{
    /// <summary>
    /// The unique ID of the PickTicket to update
    /// This must match the Id from the original PickTicket
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The tracking number (ProNumber) received from R&L Carrier
    /// This gets saved to the PickTicket so users can track their shipment
    /// </summary>
    public string ProNumber { get; set; } = string.Empty;
}