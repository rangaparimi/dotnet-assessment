using AssetManagement.Api.Models;

namespace AssetManagement.Api.Repositories.Interfaces;

public interface IAllocationRepository
{
    // Tracked, with Asset and Employee loaded - for recording a return
    Task<AssetAllocation?> GetByIdWithDetailsAsync(int id);

    // active: true = not returned yet, false = returned, null = all
    Task<List<AssetAllocation>> GetAllWithDetailsAsync(bool? active);
    Task<List<AssetAllocation>> GetReturnedWithDetailsAsync();
    Task<List<AssetAllocation>> GetOpenByEmployeeWithAssetAsync(int employeeId);

    Task<bool> EmployeeHoldsAssetsAsync(int employeeId);
    Task<bool> AnyForEmployeeAsync(int employeeId);
    Task<bool> AnyForAssetAsync(int assetId);
    void Add(AssetAllocation allocation);
}
