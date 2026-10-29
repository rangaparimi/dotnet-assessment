using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;

namespace AssetManagement.Api.Services.Interfaces;

public interface IAllocationService
{
    Task<ServiceResult<AllocationDto>> AllocateAsync(CreateAllocationDto dto);  //this async method is used for creating the allocation and it returns a ServiceResult<AllocationDto> object and this DTO is used to update in the database
    Task<List<AllocationDto>> GetAllAsync(bool? active);
    Task<PagedResult<AllocationDto>> GetPagedAsync(bool? active, int page, int pageSize);
}
