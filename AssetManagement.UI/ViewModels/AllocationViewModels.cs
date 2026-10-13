using System.ComponentModel.DataAnnotations;
using AssetManagement.UI.Models;

namespace AssetManagement.UI.ViewModels;

// Everything the Allocation page shows
public class AllocationsViewModel
{
    public List<AssetDto> AvailableAssets { get; set; } = [];
    public List<EmployeeDto> ActiveEmployees { get; set; } = [];
    public List<AllocationDto> Allocations { get; set; } = [];
    public AllocationFormModel Form { get; set; } = new();

    // List filter: "active", "returned" or "all"
    public string Show { get; set; } = "active";
}

// The Allocate form. Also sent as the JSON body to POST api/allocations.
public class AllocationFormModel
{
    [Display(Name = "Asset"), Required(ErrorMessage = "Choose an asset.")]
    public int? AssetId { get; set; }

    [Display(Name = "Employee"), Required(ErrorMessage = "Choose an employee.")]
    public int? EmployeeId { get; set; }

    [MaxLength(250)]
    public string? Remarks { get; set; }
}
