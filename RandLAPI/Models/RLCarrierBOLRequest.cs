namespace RandLAPI.Models;

public class RLCarrierBOLRequest
{
    public BillOfLading BillOfLading { get; set; } = new();
}

public class BillOfLading
{
    public string BOLDate { get; set; } = string.Empty;
    public Shipper Shipper { get; set; } = new();
    public Consignee Consignee { get; set; } = new();
    public List<Item> Items { get; set; } = new();
}

public class Shipper
{
    public string CompanyName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string ZipOrPostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class Consignee
{
    public string CompanyName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string ZipOrPostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class Item
{
    public string Class { get; set; } = string.Empty;
    public int Pieces { get; set; }
    public int Weight { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}