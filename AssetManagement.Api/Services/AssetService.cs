using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Models;
using AssetManagement.Api.Models.Enums;
using AssetManagement.Api.Repositories.Interfaces;
using AssetManagement.Api.Services.Interfaces;

namespace AssetManagement.Api.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assets;
    private readonly IAllocationRepository _allocations;
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(IAssetRepository assets, IAllocationRepository allocations, IUnitOfWork unitOfWork)
    {
        _assets = assets;
        _allocations = allocations;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AssetDto>> GetAllAsync()
    {
        var assets = await _assets.GetAllAsync();
        return assets.Select(ToDto).ToList();
    }

    public async Task<PagedResult<AssetDto>> GetPagedAsync(int page, int pageSize)
    {
        var result = await _assets.GetPagedAsync(page, pageSize);
        return result.Map(ToDto);
    }

    public async Task<ServiceResult<AssetDto>> GetByIdAsync(int id)
    {
        var asset = await _assets.GetByIdAsync(id);
        if (asset == null)
            return ServiceResult<AssetDto>.NotFound($"Asset with id {id} was not found.");

        return ServiceResult<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<ServiceResult<AssetDto>> CreateAsync(CreateAssetDto dto)
    {
        var tag = dto.AssetTag.Trim();
        if (await _assets.TagExistsAsync(tag))
            return ServiceResult<AssetDto>.Conflict($"Asset tag {tag} already exists.");

        var asset = new Asset
        {
            AssetTag = tag,
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            SerialNumber = dto.SerialNumber?.Trim(),
            PurchaseDate = dto.PurchaseDate,
            Status = AssetStatus.Available
        };

        _assets.Add(asset);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<ServiceResult<AssetDto>> UpdateAsync(int id, UpdateAssetDto dto)
    {
        var asset = await _assets.GetByIdAsync(id);
        if (asset == null)
            return ServiceResult<AssetDto>.NotFound($"Asset with id {id} was not found.");

        var tag = dto.AssetTag.Trim();
        if (await _assets.TagExistsAsync(tag, excludeId: id))
            return ServiceResult<AssetDto>.Conflict($"Asset tag {tag} already exists.");

        if (dto.Status.HasValue && dto.Status != asset.Status)
        {
            if (dto.Status == AssetStatus.Allocated)
                return ServiceResult<AssetDto>.BadRequest("Use the allocations endpoint to allocate an asset.");

            if (asset.Status == AssetStatus.Allocated)
                return ServiceResult<AssetDto>.BadRequest($"Asset {asset.AssetTag} is allocated. Record a return before changing its status.");

            asset.Status = dto.Status.Value;
        }

        asset.AssetTag = tag;
        asset.Name = dto.Name.Trim();
        asset.Category = dto.Category.Trim();
        asset.SerialNumber = dto.SerialNumber?.Trim();
        asset.PurchaseDate = dto.PurchaseDate;

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<AssetDto>.Ok(ToDto(asset));
    }

    public async Task<ServiceResult<string>> DeleteAsync(int id)
    {
        var asset = await _assets.GetByIdAsync(id);
        if (asset == null)
            return ServiceResult<string>.NotFound($"Asset with id {id} was not found.");

        if (asset.Status == AssetStatus.Allocated)
            return ServiceResult<string>.BadRequest($"Asset {asset.AssetTag} is currently allocated and cannot be deleted.");

        // Keep history: an asset that was ever allocated is retired, not deleted
        if (await _allocations.AnyForAssetAsync(id))
        {
            asset.Status = AssetStatus.Retired;
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<string>.Ok($"Asset {asset.AssetTag} has allocation history, so it was marked as Retired instead of deleted.");
        }

        _assets.Remove(asset);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult<string>.Ok($"Asset {asset.AssetTag} was deleted.");
    }

    private static AssetDto ToDto(Asset a) => new()
    {
        Id = a.Id,
        AssetTag = a.AssetTag,
        Name = a.Name,
        Category = a.Category,
        SerialNumber = a.SerialNumber,
        PurchaseDate = a.PurchaseDate,
        Status = a.Status
    };
}
