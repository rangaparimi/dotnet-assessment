using System.ComponentModel.DataAnnotations;

namespace AssetManagement.UI.Models;

// Copies of the API's response shapes (AssetManagement.Api/DTOs).
// The UI only talks to the API over HTTP, so it keeps its own copies
// instead of referencing the API project (which would pull in EF Core).

public enum AssetStatus
{
    Available,
    Allocated,
    [Display(Name = "Under Repair")]
    UnderRepair,
    Retired
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

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public bool IsActive { get; set; }
}

public class HeldAssetDto
{
    public int AllocationId { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime AllocatedDate { get; set; }
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
    public AssetStatus AssetStatus { get; set; }
}

// Delete endpoints answer { "message": "..." }
public class MessageDto
{
    public string Message { get; set; } = "";
}
