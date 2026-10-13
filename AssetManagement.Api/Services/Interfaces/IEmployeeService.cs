using AssetManagement.Api.DTOs;

namespace AssetManagement.Api.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync();
    Task<ServiceResult<EmployeeDto>> GetByIdAsync(int id);
    Task<ServiceResult<EmployeeDto>> CreateAsync(CreateEmployeeDto dto);
    Task<ServiceResult<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task<ServiceResult<string>> DeleteAsync(int id);
    Task<ServiceResult<List<HeldAssetDto>>> GetCurrentAssetsAsync(int id);
}
