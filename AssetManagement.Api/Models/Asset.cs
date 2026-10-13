using AssetManagement.Api.Models.Enums;

namespace AssetManagement.Api.Models;

public class Asset
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";     // unique, e.g. "LAP-0012"
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";     // Laptop, Monitor, Phone...
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}
