using AssetManagement.Api.Models;

namespace AssetManagement.Api.Repositories.Interfaces;

public interface IAssetRepository
{
    Task<List<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<bool> TagExistsAsync(string assetTag, int? excludeId = null);
    void Add(Asset asset);
    void Remove(Asset asset);
}
