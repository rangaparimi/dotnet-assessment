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

    [HttpPost]
    public async Task<ActionResult<ReturnDto>> Return(CreateReturnDto dto)
    {
        var result = await _returnService.ReturnAsync(dto);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }
}
