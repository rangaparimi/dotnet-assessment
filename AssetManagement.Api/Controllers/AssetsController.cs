using AssetManagement.Api.Common;
using AssetManagement.Api.DTOs;
using AssetManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssetDto>>> GetAll()
    {
        return Ok(await _assetService.GetAllAsync());
    }

    // GET /api/assets/paged?page=2&pageSize=10  -> one page of assets plus the totals
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<AssetDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _assetService.GetPagedAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetDto>> GetById(int id)
    {
        var result = await _assetService.GetByIdAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<ActionResult<AssetDto>> Create(CreateAssetDto dto)
    {
        var result = await _assetService.CreateAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetDto>> Update(int id, UpdateAssetDto dto)
    {
        var result = await _assetService.UpdateAsync(id, dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _assetService.DeleteAsync(id);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(new { message = result.Data });
    }
}
