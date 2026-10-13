using System.ComponentModel.DataAnnotations;

namespace AssetManagement.UI.Models;

// A returned asset as the API sends it. Also used as the Record Return form
// (the form only fills AllocationId, ReturnCondition and Remarks).
public class AssetReturn
{
    [Required(ErrorMessage = "Choose an allocation.")]
    public int? AllocationId { get; set; }

    public string AssetTag { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public DateTime AllocatedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }

    [Required]
    public string ReturnCondition { get; set; } = "Good";      // Good or Damaged

    [MaxLength(200)]
    public string? Remarks { get; set; }

    public string? AssetStatus { get; set; }                    // asset status after the return
}
