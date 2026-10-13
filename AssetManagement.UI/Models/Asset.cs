using System.ComponentModel.DataAnnotations;

namespace AssetManagement.UI.Models;

// An asset as the API sends it. Also used as the Add/Edit form.
public class Asset
{
    public int Id { get; set; }                 // 0 = new asset

    [Required, MaxLength(50)]
    public string AssetTag { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "";

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public DateTime? PurchaseDate { get; set; }

    public string? Status { get; set; }         // Available, Allocated, UnderRepair, Retired
}
