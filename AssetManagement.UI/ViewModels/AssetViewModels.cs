using System.ComponentModel.DataAnnotations;
using AssetManagement.UI.Models;

namespace AssetManagement.UI.ViewModels;

// Everything the Assets page shows
public class AssetsViewModel
{
    public List<AssetDto> Assets { get; set; } = [];
    public AssetFormModel Form { get; set; } = new();
    public bool IsEdit => Form.Id.HasValue;
}

// The Add/Edit asset form. Also sent as the JSON body to POST/PUT api/assets.
public class AssetFormModel
{
    public int? Id { get; set; }

    [Display(Name = "Asset Tag"), Required, MaxLength(50)]
    public string AssetTag { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "";

    [Display(Name = "Serial Number"), MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Display(Name = "Purchase Date"), Required]
    public DateTime? PurchaseDate { get; set; }

    // Only used when editing; null keeps the current status
    public AssetStatus? Status { get; set; }

    public static AssetFormModel FromDto(AssetDto a) => new()
    {
        Id = a.Id,
        AssetTag = a.AssetTag,
        Name = a.Name,
        Category = a.Category,
        SerialNumber = a.SerialNumber,
        PurchaseDate = a.PurchaseDate,
        Status = a.Status
    };
}
