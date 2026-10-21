using AssetManagement.Api.Common;
using AssetManagement.Api.Models;

namespace AssetManagement.Api.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync();
    Task<PagedResult<Employee>> GetPagedAsync(int page, int pageSize);
    Task<Employee?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> CodeExistsAsync(string employeeCode, int? excludeId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    void Add(Employee employee);
    void Remove(Employee employee);
}
