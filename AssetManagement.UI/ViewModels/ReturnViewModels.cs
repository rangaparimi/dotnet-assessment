using System.ComponentModel.DataAnnotations;
using AssetManagement.UI.Models;

namespace AssetManagement.UI.ViewModels;

// Everything the Returns page shows
public class ReturnsViewModel
{
    public List<AllocationDto> OpenAllocations { get; set; } = [];
    public List<ReturnDto> Returns { get; set; } = [];
    public ReturnFormModel Form { get; set; } = new();
}

// The Record Return form. Also sent as the JSON body to POST api/returns.
public class ReturnFormModel
{
    [Display(Name = "Open allocation"), Required(ErrorMessage = "Choose an allocation.")]
    public int? AllocationId { get; set; }

    [Display(Name = "Condition"), Required]
    [RegularExpression("^(Good|Damaged)$", ErrorMessage = "Condition must be Good or Damaged.")]
    public string ReturnCondition { get; set; } = "Good";

    [MaxLength(200)]
    public string? Remarks { get; set; }
}
