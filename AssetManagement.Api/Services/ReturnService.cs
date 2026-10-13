using AssetManagement.Api.DTOs;
using AssetManagement.Api.Models;
using AssetManagement.Api.Models.Enums;
using AssetManagement.Api.Repositories.Interfaces;
using AssetManagement.Api.Services.Interfaces;

namespace AssetManagement.Api.Services;

public class ReturnService : IReturnService
{
    private readonly IAllocationRepository _allocations;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnService(IAllocationRepository allocations, IUnitOfWork unitOfWork)
    {
        _allocations = allocations;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<ReturnDto>> ReturnAsync(CreateReturnDto dto)
    {
        var allocation = await _allocations.GetByIdWithDetailsAsync(dto.AllocationId);

        if (allocation == null)
            return ServiceResult<ReturnDto>.NotFound($"Allocation with id {dto.AllocationId} was not found.");

        if (allocation.ReturnedDate != null)
            return ServiceResult<ReturnDto>.BadRequest($"Asset {allocation.Asset.AssetTag} from allocation {allocation.Id} was already returned on {allocation.ReturnedDate:yyyy-MM-dd}.");

        // Close the allocation (the return is recorded on the same row)
        allocation.ReturnedDate = DateTime.UtcNow;
        allocation.ReturnCondition = dto.ReturnCondition;
        allocation.Remarks = MergeRemarks(allocation.Remarks, dto.Remarks);

        // Damaged assets go for repair, good ones are available again
        allocation.Asset.Status = dto.ReturnCondition == "Damaged"
            ? AssetStatus.UnderRepair
            : AssetStatus.Available;

        // Two changes, one SaveChangesAsync => one transac
        await _unitOfWork.SaveChangesAsync(); //    what await does is it waits for the SaveChangesAsync() method to complete before returning the result of the ToDto(allocation) method, which converts the allocation object to a ReturnDto object and returns it as the result of the ReturnAsync() method.

        return ServiceResult<ReturnDto>.Ok(ToDto(allocation));  
    }

    public async Task<List<ReturnDto>> GetAllAsync() //what GetAllSync() does is it retrieves all the returned assets from the database, including their associated asset and employee information, and returns them as a list of ReturnDto objects.
    {
        var returned = await _allocations.GetReturnedWithDetailsAsync();

        return returned.Select(ToDto).ToList();
    }

    // Keeps the allocation remarks and adds the return remarks after them
    private static string? MergeRemarks(string? allocationRemarks, string? returnRemarks)
    {
        if (string.IsNullOrWhiteSpace(returnRemarks))
            return allocationRemarks;

        if (string.IsNullOrWhiteSpace(allocationRemarks))
            return $"Return: {returnRemarks.Trim()}";

        return $"{allocationRemarks} | Return: {returnRemarks.Trim()}";
    }

    private static ReturnDto ToDto(AssetAllocation x) => new()
    {
        AllocationId = x.Id,
        AssetId = x.AssetId,
        AssetTag = x.Asset.AssetTag,
        AssetName = x.Asset.Name,
        EmployeeId = x.EmployeeId,
        EmployeeName = x.Employee.FullName,
        AllocatedDate = x.AllocatedDate,
        ReturnedDate = x.ReturnedDate!.Value,
        ReturnCondition = x.ReturnCondition ?? "",
        Remarks = x.Remarks,
        AssetStatus = x.Asset.Status
    };
}
