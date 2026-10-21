using AssetManagement.Api.Common;
using AssetManagement.Api.Data;
using AssetManagement.Api.Models;
using AssetManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _db;

    public AssetRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Asset>> GetAllAsync() =>
        _db.Assets.AsNoTracking().OrderBy(a => a.AssetTag).ToListAsync();

    public Task<PagedResult<Asset>> GetPagedAsync(int page, int pageSize) =>
        _db.Assets.AsNoTracking().OrderBy(a => a.AssetTag).ToPagedResultAsync(page, pageSize);

    // Tracked, so changes made by the service are saved by IUnitOfWork
    public async Task<Asset?> GetByIdAsync(int id) =>
        await _db.Assets.FindAsync(id);

    public Task<bool> TagExistsAsync(string assetTag, int? excludeId = null) =>
        _db.Assets.AnyAsync(a => a.AssetTag == assetTag && a.Id != excludeId);

    public void Add(Asset asset) => _db.Assets.Add(asset);

    public void Remove(Asset asset) => _db.Assets.Remove(asset);
}
