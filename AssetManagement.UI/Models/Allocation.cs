using System.ComponentModel.DataAnnotations;

namespace AssetManagement.UI.Models;

// An allocation as the API sends it. Also used as the Allocate form
// (the form only fills AssetId, EmployeeId and Remarks).
public class Allocation
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Choose an asset.")]
    public int? AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public string AssetName { get; set; } = "";

    [Required(ErrorMessage = "Choose an employee.")]
    public int? EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";

    public DateTime AllocatedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }     // null = still with the employee
    public string? ReturnCondition { get; set; }

    [MaxLength(250)]
    public string? Remarks { get; set; }
}
