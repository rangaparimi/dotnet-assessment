using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Models;
using AssetManagement.Api.Repositories.Interfaces;
using AssetManagement.Api.Services.Interfaces;

namespace AssetManagement.Api.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IAllocationRepository _allocations;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IEmployeeRepository employees, IAllocationRepository allocations, IUnitOfWork unitOfWork)
    {
        _employees = employees;
        _allocations = allocations;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EmployeeDto>> GetAllAsync()
    {
        var employees = await _employees.GetAllAsync();
        return employees.Select(ToDto).ToList();
    }

    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(int page, int pageSize)
    {
        var result = await _employees.GetPagedAsync(page, pageSize);
        return result.Map(ToDto);
    }

    public async Task<ServiceResult<EmployeeDto>> GetByIdAsync(int id)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound($"Employee with id {id} was not found.");

        return ServiceResult<EmployeeDto>.Ok(ToDto(employee));
    }

    public async Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeDto dto)
    {
        var code = dto.EmployeeCode.Trim();
        var email = dto.Email.Trim();

        var duplicate = await FindDuplicateAsync(code, email, excludeId: null);
        if (duplicate != null)
            return ServiceResult<EmployeeDto>.Conflict(duplicate);

        var employee = new Employee
        {
            EmployeeCode = code,
            FullName = dto.FullName.Trim(),
            Email = email,
            Department = dto.Department.Trim(),
            IsActive = true
        };

        _employees.Add(employee);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<EmployeeDto>.Ok(ToDto(employee));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee == null)
            return ServiceResult<EmployeeDto>.NotFound($"Employee with id {id} was not found.");

        var code = dto.EmployeeCode.Trim();
        var email = dto.Email.Trim();

        var duplicate = await FindDuplicateAsync(code, email, excludeId: id);
        if (duplicate != null)
            return ServiceResult<EmployeeDto>.Conflict(duplicate);

        if (employee.IsActive && !dto.IsActive && await _allocations.EmployeeHoldsAssetsAsync(id))
            return ServiceResult<EmployeeDto>.BadRequest($"Employee {employee.EmployeeCode} still holds assets and cannot be deactivated until all assets are returned.");

        employee.EmployeeCode = code;
        employee.FullName = dto.FullName.Trim();
        employee.Email = email;
        employee.Department = dto.Department.Trim();
        employee.IsActive = dto.IsActive;

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<EmployeeDto>.Ok(ToDto(employee));
    }

    public async Task<ServiceResult<string>> DeleteAsync(int id)
    {
        var employee = await _employees.GetByIdAsync(id);
        if (employee == null)
            return ServiceResult<string>.NotFound($"Employee with id {id} was not found.");

        if (await _allocations.EmployeeHoldsAssetsAsync(id))
            return ServiceResult<string>.BadRequest($"Employee {employee.EmployeeCode} still holds assets and cannot be deactivated until all assets are returned.");

        // Keep history: an employee who ever had an asset is deactivated, not deleted
        if (await _allocations.AnyForEmployeeAsync(id))
        {
            employee.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<string>.Ok($"Employee {employee.EmployeeCode} has allocation history, so they were deactivated instead of deleted.");
        }

        _employees.Remove(employee);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<string>.Ok($"Employee {employee.EmployeeCode} was deleted.");
    }

    public async Task<ServiceResult<List<HeldAssetDto>>> GetCurrentAssetsAsync(int id)
    {
        if (!await _employees.ExistsAsync(id))
            return ServiceResult<List<HeldAssetDto>>.NotFound($"Employee with id {id} was not found.");

        var allocations = await _allocations.GetOpenByEmployeeWithAssetAsync(id);
        var assets = allocations.Select(x => new HeldAssetDto
        {
            AllocationId = x.Id,
            AssetId = x.AssetId,
            AssetTag = x.Asset.AssetTag,
            Name = x.Asset.Name,
            Category = x.Asset.Category,
            AllocatedDate = x.AllocatedDate
        }).ToList();

        return ServiceResult<List<HeldAssetDto>>.Ok(assets);
    }

    // Returns an error message if the code or email is already used by another employee
    private async Task<string?> FindDuplicateAsync(string code, string email, int? excludeId)
    {
        if (await _employees.CodeExistsAsync(code, excludeId))
            return $"Employee code {code} already exists.";

        if (await _employees.EmailExistsAsync(email, excludeId))
            return $"Email {email} is already used by another employee.";

        return null;
    }

    private static EmployeeDto ToDto(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        Email = e.Email,
        Department = e.Department,
        IsActive = e.IsActive
    };
}
