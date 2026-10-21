using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;

namespace AssetManagement.Api.Services.Interfaces;

public interface IAssetService
{
    Task<List<AssetDto>> GetAllAsync();
    Task<PagedResult<AssetDto>> GetPagedAsync(int page, int pageSize);
    Task<ServiceResult<AssetDto>> GetByIdAsync(int id);
    Task<ServiceResult<AssetDto>> CreateAsync(CreateAssetDto dto);
    Task<ServiceResult<AssetDto>> UpdateAsync(int id, UpdateAssetDto dto);
    Task<ServiceResult<string>> DeleteAsync(int id);
}
