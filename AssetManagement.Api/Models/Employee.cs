namespace AssetManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = "";  // unique
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";         // unique
    public string Department { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}
