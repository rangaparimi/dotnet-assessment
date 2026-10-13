using System.ComponentModel.DataAnnotations;
using AssetManagement.Api.Models.Enums;

namespace AssetManagement.Api.DTOs;

public class CreateAssetDto
{
    [Required, MaxLength(50)]
    public string AssetTag { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "";

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public DateTime PurchaseDate { get; set; }
}

public class UpdateAssetDto
{
    [Required, MaxLength(50)]
    public string AssetTag { get; set; } = "";

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "";

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    public DateTime PurchaseDate { get; set; }

    // Optional. Leave empty to keep the current status.
    // "Allocated" can only be set through the Allocation module.
    public AssetStatus? Status { get; set; }
}

public class AssetDto
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public AssetStatus Status { get; set; }
}
