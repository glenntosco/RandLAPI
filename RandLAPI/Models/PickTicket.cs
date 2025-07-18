namespace RandLAPI.Models;

public class PickTicket
{
    public Guid Id { get; set; }
    public string PickTicketNumber { get; set; } = string.Empty;
    public string? ProNumber { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string PickTicketState { get; set; } = string.Empty;
}