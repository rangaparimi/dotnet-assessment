using AssetManagement.Api.Data;
using AssetManagement.Api.Models;
using AssetManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly AppDbContext _db;

    public AllocationRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AssetAllocation?> GetByIdWithDetailsAsync(int id) =>
        _db.AssetAllocations
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task<List<AssetAllocation>> GetAllWithDetailsAsync(bool? active)
    {
        var query = _db.AssetAllocations
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .AsQueryable();

        if (active == true)
            query = query.Where(x => x.ReturnedDate == null);
        else if (active == false)
            query = query.Where(x => x.ReturnedDate != null);

        return query.OrderByDescending(x => x.AllocatedDate).ToListAsync();
    }

    public Task<List<AssetAllocation>> GetReturnedWithDetailsAsync() =>
        _db.AssetAllocations
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Where(x => x.ReturnedDate != null)
            .OrderByDescending(x => x.ReturnedDate)
            .ToListAsync();

    public Task<List<AssetAllocation>> GetOpenByEmployeeWithAssetAsync(int employeeId) =>
        _db.AssetAllocations
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.EmployeeId == employeeId && x.ReturnedDate == null)
            .OrderBy(x => x.AllocatedDate)
            .ToListAsync();

    public Task<bool> EmployeeHoldsAssetsAsync(int employeeId) =>
        _db.AssetAllocations.AnyAsync(x => x.EmployeeId == employeeId && x.ReturnedDate == null);

    public Task<bool> AnyForEmployeeAsync(int employeeId) =>
        _db.AssetAllocations.AnyAsync(x => x.EmployeeId == employeeId);

    public Task<bool> AnyForAssetAsync(int assetId) =>
        _db.AssetAllocations.AnyAsync(x => x.AssetId == assetId);

    public void Add(AssetAllocation allocation) => _db.AssetAllocations.Add(allocation);
}
