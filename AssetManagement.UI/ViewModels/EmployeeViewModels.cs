using System.ComponentModel.DataAnnotations;
using AssetManagement.UI.Models;

namespace AssetManagement.UI.ViewModels;

// Everything the Employees page shows
public class EmployeesViewModel
{
    public List<EmployeeDto> Employees { get; set; } = [];
    public EmployeeFormModel Form { get; set; } = new();
    public bool IsEdit => Form.Id.HasValue;

    // Filled when the user clicks "Assets" on an employee row
    public EmployeeDto? HeldBy { get; set; }
    public List<HeldAssetDto> HeldAssets { get; set; } = [];
}

// The Add/Edit employee form. Also sent as the JSON body to POST/PUT api/employees.
public class EmployeeFormModel
{
    public int? Id { get; set; }

    [Display(Name = "Employee Code"), Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Display(Name = "Full Name"), Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = "";

    [Required, MaxLength(50)]
    public string Department { get; set; } = "";

    // Only shown when editing; new employees are always active
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public static EmployeeFormModel FromDto(EmployeeDto e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        Email = e.Email,
        Department = e.Department,
        IsActive = e.IsActive
    };
}
