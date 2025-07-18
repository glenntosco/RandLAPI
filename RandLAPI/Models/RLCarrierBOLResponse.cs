namespace RandLAPI.Models;

/// <summary>
/// Response object returned by R&L Carrier after creating a Bill of Lading
/// Contains the tracking number and status code for the BOL creation request
/// </summary>
public class RLCarrierBOLResponse
{
    /// <summary>
    /// The tracking number (ProNumber) assigned by R&L Carrier
    /// This is what customers use to track their shipment
    /// Will be empty/null if the BOL creation failed
    /// </summary>
    public string ProNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Status code indicating the result of the BOL creation
    /// 0 = Success (BOL created successfully)
    /// Other values = Error (specific meaning depends on R&L's API documentation)
    /// </summary>
    public int Code { get; set; }
}