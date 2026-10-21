using AssetManagement.Api.Common;
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

    public Task<List<AssetAllocation>> GetAllWithDetailsAsync(bool? active) =>
        AllocationsQuery(active).ToListAsync();

    public Task<PagedResult<AssetAllocation>> GetPagedWithDetailsAsync(bool? active, int page, int pageSize) =>
        AllocationsQuery(active).ToPagedResultAsync(page, pageSize);

    public Task<List<AssetAllocation>> GetReturnedWithDetailsAsync() =>
        ReturnedQuery().ToListAsync();

    public Task<PagedResult<AssetAllocation>> GetReturnedPagedWithDetailsAsync(int page, int pageSize) =>
        ReturnedQuery().ToPagedResultAsync(page, pageSize);

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

    // Shared by the full-list and the paged versions, so both always show the same rows in the same order.
    // The Id in the ordering keeps rows with the same date from swapping between pages.
    private IQueryable<AssetAllocation> AllocationsQuery(bool? active)
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

        return query.OrderByDescending(x => x.AllocatedDate).ThenByDescending(x => x.Id);
    }

    private IQueryable<AssetAllocation> ReturnedQuery() =>
        _db.AssetAllocations
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.Employee)
            .Where(x => x.ReturnedDate != null)
            .OrderByDescending(x => x.ReturnedDate)
            .ThenByDescending(x => x.Id);
}
