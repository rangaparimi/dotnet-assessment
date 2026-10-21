using AssetManagement.Api.Common;
using AssetManagement.Api.Models;

namespace AssetManagement.Api.Repositories.Interfaces;

public interface IAssetRepository
{
    Task<List<Asset>> GetAllAsync();
    Task<PagedResult<Asset>> GetPagedAsync(int page, int pageSize);
    Task<Asset?> GetByIdAsync(int id);
    Task<bool> TagExistsAsync(string assetTag, int? excludeId = null);
    void Add(Asset asset);
    void Remove(Asset asset);
}
