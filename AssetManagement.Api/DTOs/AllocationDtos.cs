using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Api.DTOs;

public class CreateAllocationDto
{
    [Required, Range(1, int.MaxValue)]
    public int AssetId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [MaxLength(250)]
    public string? Remarks { get; set; }
}

public class AllocationDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public string AssetName { get; set; } = "";
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime AllocatedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public string? ReturnCondition { get; set; }
    public string? Remarks { get; set; }
}
