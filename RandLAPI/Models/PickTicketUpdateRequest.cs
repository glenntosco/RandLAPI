namespace RandLAPI.Models;

public class PickTicketUpdateRequest
{
    public Guid Id { get; set; }
    public string ProNumber { get; set; } = string.Empty;
}