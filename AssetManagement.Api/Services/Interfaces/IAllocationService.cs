using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;

namespace AssetManagement.Api.Services.Interfaces;

public interface IAllocationService
{
    Task<ServiceResult<AllocationDto>> AllocateAsync(CreateAllocationDto dto);
    Task<List<AllocationDto>> GetAllAsync(bool? active);
    Task<PagedResult<AllocationDto>> GetPagedAsync(bool? active, int page, int pageSize);
}
