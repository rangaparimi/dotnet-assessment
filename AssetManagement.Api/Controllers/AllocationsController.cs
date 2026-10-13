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

    [HttpPost]
    public async Task<ActionResult<AllocationDto>> Allocate(CreateAllocationDto dto)
    {
        var result = await _allocationService.AllocateAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }
}
