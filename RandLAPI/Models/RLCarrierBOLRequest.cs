namespace RandLAPI.Models;

/// <summary>
/// Root request object for creating a Bill of Lading with R&L Carrier
/// This is the top-level structure that gets sent to R&L's API
/// </summary>
public class RLCarrierBOLRequest
{
    /// <summary>
    /// The actual Bill of Lading data
    /// R&L's API expects the BOL data to be nested under this property
    /// </summary>
    public BillOfLading BillOfLading { get; set; } = new();
}

/// <summary>
/// Represents a Bill of Lading (BOL) - the main shipping document
/// A BOL contains all the information needed to ship freight
/// </summary>
public class BillOfLading
{
    /// <summary>
    /// Date the BOL was created (format: MM/dd/yyyy)
    /// Usually set to today's date when creating the BOL
    /// </summary>
    public string BOLDate { get; set; } = string.Empty;
    
    /// <summary>
    /// Information about who is shipping the freight (the sender)
    /// This includes company name, address, and contact information
    /// </summary>
    public Shipper Shipper { get; set; } = new();
    
    /// <summary>
    /// Information about who will receive the freight (the recipient)
    /// This includes company name and delivery address
    /// </summary>
    public Consignee Consignee { get; set; } = new();
    
    /// <summary>
    /// List of items being shipped
    /// Each item includes weight, dimensions, freight class, and description
    /// </summary>
    public List<Item> Items { get; set; } = new();
}

/// <summary>
/// Represents the company shipping the freight (sender/origin)
/// Contains all the information needed to identify and contact the shipper
/// </summary>
public class Shipper
{
    /// <summary>
    /// Name of the shipping company
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary street address where freight is picked up from
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;
    
    /// <summary>
    /// City where the shipper is located
    /// </summary>
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// State or province abbreviation (e.g., "NV", "CA")
    /// </summary>
    public string StateOrProvince { get; set; } = string.Empty;
    
    /// <summary>
    /// ZIP or postal code for the shipper's address
    /// </summary>
    public string ZipOrPostalCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Country code (e.g., "USA", "CAN")
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Contact phone number for the shipper
    /// Used by the carrier if they need to contact the sender
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Represents the company receiving the freight (recipient/destination)
/// Contains the delivery address information
/// </summary>
public class Consignee
{
    /// <summary>
    /// Name of the receiving company
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary street address where freight will be delivered
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;
    
    /// <summary>
    /// City where the consignee is located
    /// </summary>
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// State or province abbreviation for delivery location
    /// </summary>
    public string StateOrProvince { get; set; } = string.Empty;
    
    /// <summary>
    /// ZIP or postal code for the delivery address
    /// </summary>
    public string ZipOrPostalCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Country code for the delivery location
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single item or group of items being shipped
/// Contains weight, dimensions, classification, and description
/// </summary>
public class Item
{
    /// <summary>
    /// Freight class (determines shipping rates)
    /// Common classes: 50, 55, 60, 65, 70, 77.5, 85, 92.5, 100, etc.
    /// Higher numbers = lower density = higher cost per pound
    /// </summary>
    public string Class { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of individual pieces/packages for this item
    /// For example: 3 pallets, 10 boxes, 1 crate
    /// </summary>
    public int Pieces { get; set; }
    
    /// <summary>
    /// Total weight of this item in pounds
    /// Used for calculating shipping costs and planning
    /// </summary>
    public int Weight { get; set; }
    
    /// <summary>
    /// Type of packaging used (e.g., "PLT" = Pallet, "BOX" = Box, "CRT" = Crate)
    /// Helps the carrier understand how to handle the freight
    /// </summary>
    public string PackageType { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what's being shipped
    /// Should be specific enough for customs/safety but not too detailed
    /// </summary>
    public string Description { get; set; } = string.Empty;
}