using System.ComponentModel.DataAnnotations;
using AssetManagement.Api.Models.Enums;

namespace AssetManagement.Api.DTOs;

public class CreateReturnDto
{
    [Required, Range(1, int.MaxValue)]
    public int AllocationId { get; set; }

    [Required]
    [RegularExpression("^(Good|Damaged)$", ErrorMessage = "ReturnCondition must be 'Good' or 'Damaged'.")]
    public string ReturnCondition { get; set; } = "";

    [MaxLength(200)]
    public string? Remarks { get; set; }
}

public class ReturnDto
{
    public int AllocationId { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public string AssetName { get; set; } = "";
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime AllocatedDate { get; set; }
    public DateTime ReturnedDate { get; set; }
    public string ReturnCondition { get; set; } = "";
    public string? Remarks { get; set; }
    public AssetStatus AssetStatus { get; set; }   // asset status after the return
}
