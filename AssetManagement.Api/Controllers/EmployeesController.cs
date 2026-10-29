using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService; //Dependency injection of the employee service with an interface


    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll()
    {
        return Ok(await _employeeService.GetAllAsync());
    }

    // GET /api/employees/paged?page=2&pageSize=10  -> one page of employees plus the totals
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _employeeService.GetPagedAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var result = await _employeeService.GetByIdAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(CreateEmployeeDto dto)
    {
        var result = await _employeeService.CreateAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, UpdateEmployeeDto dto)
    {
        var result = await _employeeService.UpdateAsync(id, dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _employeeService.DeleteAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(new { message = result.Data });
    }

    [HttpGet("{id:int}/assets")]
    public async Task<ActionResult<List<HeldAssetDto>>> GetCurrentAssets(int id)
    {
        var result = await _employeeService.GetCurrentAssetsAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }
}
