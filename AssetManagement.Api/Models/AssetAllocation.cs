namespace AssetManagement.Api.Models;

public class AssetAllocation
{
    public int Id { get; set; } //hidden primary key, var used for internal tracking of the allocation record
    public int AssetId { get; set; }    //unique
    public Asset Asset { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime AllocatedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }     // null = still with employee
    public string? ReturnCondition { get; set; }    // Good / Damaged
    public string? Remarks { get; set; }
}
