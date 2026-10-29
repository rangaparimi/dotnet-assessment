using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;

namespace AssetManagement.Api.Services.Interfaces;

public interface IReturnService
{
    Task<ServiceResult<ReturnDto>> ReturnAsync(CreateReturnDto dto); 
    Task<List<ReturnDto>> GetAllAsync();
    Task<PagedResult<ReturnDto>> GetPagedAsync(int page, int pageSize);
}
