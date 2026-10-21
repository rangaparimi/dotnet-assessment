using AssetManagement.Api.Common;
using AssetManagement.Api.Data;
using AssetManagement.Api.Models;
using AssetManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Employee>> GetAllAsync() =>
        _db.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).ToListAsync();

    public Task<PagedResult<Employee>> GetPagedAsync(int page, int pageSize) =>
        _db.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).ToPagedResultAsync(page, pageSize);

    // Tracked, so changes made by the service are saved by IUnitOfWork
    public async Task<Employee?> GetByIdAsync(int id) =>
        await _db.Employees.FindAsync(id);

    public Task<bool> ExistsAsync(int id) =>
        _db.Employees.AnyAsync(e => e.Id == id);

    public Task<bool> CodeExistsAsync(string employeeCode, int? excludeId = null) =>
        _db.Employees.AnyAsync(e => e.EmployeeCode == employeeCode && e.Id != excludeId);

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        _db.Employees.AnyAsync(e => e.Email == email && e.Id != excludeId);

    public void Add(Employee employee) => _db.Employees.Add(employee);

    public void Remove(Employee employee) => _db.Employees.Remove(employee);
}
