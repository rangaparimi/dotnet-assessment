using AssetManagement.Api.DTOs;
using AssetManagement.Api.Models;
using AssetManagement.Api.Models.Enums;
using AssetManagement.Api.Repositories.Interfaces;
using AssetManagement.Api.Services.Interfaces;

namespace AssetManagement.Api.Services;

public class AllocationService : IAllocationService
{
    private readonly IAssetRepository _assets;
    private readonly IEmployeeRepository _employees;
    private readonly IAllocationRepository _allocations;
    private readonly IUnitOfWork _unitOfWork;

    public AllocationService(
        IAssetRepository assets,
        IEmployeeRepository employees,
        IAllocationRepository allocations,
        IUnitOfWork unitOfWork)
    {
        _assets = assets;
        _employees = employees;
        _allocations = allocations;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<AllocationDto>> AllocateAsync(CreateAllocationDto dto)
    {
        var asset = await _assets.GetByIdAsync(dto.AssetId);
        if (asset == null)
            return ServiceResult<AllocationDto>.NotFound($"Asset with id {dto.AssetId} was not found.");

        if (asset.Status == AssetStatus.Allocated)
            return ServiceResult<AllocationDto>.BadRequest($"Asset {asset.AssetTag} is already allocated.");

        if (asset.Status != AssetStatus.Available)
            return ServiceResult<AllocationDto>.BadRequest($"Asset {asset.AssetTag} is not available (status: {asset.Status}).");

        var employee = await _employees.GetByIdAsync(dto.EmployeeId);
        if (employee == null)
            return ServiceResult<AllocationDto>.NotFound($"Employee with id {dto.EmployeeId} was not found.");

        if (!employee.IsActive)
            return ServiceResult<AllocationDto>.BadRequest($"Employee {employee.EmployeeCode} is not active.");

        var allocation = new AssetAllocation
        {
            AssetId = asset.Id,
            EmployeeId = employee.Id,
            AllocatedDate = DateTime.UtcNow,
            Remarks = dto.Remarks?.Trim()
        };

        // Two changes, one SaveChangesAsync => one transaction
        _allocations.Add(allocation);
        asset.Status = AssetStatus.Allocated;
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult<AllocationDto>.Ok(ToDto(allocation, asset, employee));
    }

    public async Task<List<AllocationDto>> GetAllAsync(bool? active)
    {
        var allocations = await _allocations.GetAllWithDetailsAsync(active);
        return allocations.Select(x => ToDto(x, x.Asset, x.Employee)).ToList();
    }

    private static AllocationDto ToDto(AssetAllocation x, Asset asset, Employee employee) => new()
    {
        Id = x.Id,
        AssetId = asset.Id,
        AssetTag = asset.AssetTag,
        AssetName = asset.Name,
        EmployeeId = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        EmployeeName = employee.FullName,
        AllocatedDate = x.AllocatedDate,
        ReturnedDate = x.ReturnedDate,
        ReturnCondition = x.ReturnCondition,
        Remarks = x.Remarks
    };
}
