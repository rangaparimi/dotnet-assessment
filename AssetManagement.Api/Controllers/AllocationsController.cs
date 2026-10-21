using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

[ApiController]
[Route("api/allocations")]
public class AllocationsController : ControllerBase
{
    private readonly IAllocationService _allocationService;

    public AllocationsController(IAllocationService allocationService)
    {
        _allocationService = allocationService;
    }

    // GET /api/allocations?active=true  -> only open allocations
    // GET /api/allocations?active=false -> only returned allocations
    // GET /api/allocations              -> everything
    [HttpGet]
    public async Task<ActionResult<List<AllocationDto>>> GetAll([FromQuery] bool? active)
    {
        return Ok(await _allocationService.GetAllAsync(active));
    }

    // GET /api/allocations/paged?active=true&page=2&pageSize=10  -> one page, same active filter as above
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<AllocationDto>>> GetPaged(
        [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _allocationService.GetPagedAsync(active, page, pageSize));
    }

    [HttpPost]
    public async Task<ActionResult<AllocationDto>> Allocate(CreateAllocationDto dto)
    {
        var result = await _allocationService.AllocateAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }
}
