using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Api.DTOs;

public class CreateEmployeeDto
{
    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = "";

    [Required, MaxLength(50)]
    public string Department { get; set; } = "";
}

public class UpdateEmployeeDto
{
    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = "";

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = "";

    [Required, MaxLength(50)]
    public string Department { get; set; } = "";

    public bool IsActive { get; set; } = true;
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
public class EmployeeDto1
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public bool IsActive { get; set; }
}
// An asset currently held by an employee (one open allocation)
public class HeldAssetDto
{
    public int AllocationId { get; set; }
    public int AssetId { get; set; }
    public string AssetTag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime AllocatedDate { get; set; }
}
