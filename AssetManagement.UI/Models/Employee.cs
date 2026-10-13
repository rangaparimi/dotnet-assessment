using System.ComponentModel.DataAnnotations;

namespace AssetManagement.UI.Models;

// An employee as the API sends it. Also used as the Add/Edit form.
public class Employee
{
    public int Id { get; set; }                 // 0 = new employee

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
