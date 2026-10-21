using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

[ApiController]
[Route("api/returns")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReturnDto>>> GetAll()
    {
        return Ok(await _returnService.GetAllAsync());
    }

    // GET /api/returns/paged?page=2&pageSize=10  -> one page of returned assets plus the totals
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<ReturnDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _returnService.GetPagedAsync(page, pageSize));
    }

    [HttpPost]
    public async Task<ActionResult<ReturnDto>> Return(CreateReturnDto dto)
    {
        var result = await _returnService.ReturnAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }
}
