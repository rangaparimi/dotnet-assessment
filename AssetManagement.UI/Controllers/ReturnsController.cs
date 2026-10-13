using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class ReturnsController : BaseController
{
    public ReturnsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Returns  or  /Returns?allocationId=7 (pre-selects allocation 7)
    public async Task<IActionResult> Index(int? allocationId)
    {
        await LoadPageDataAsync();
        return View(new AssetReturn { AllocationId = allocationId });
    }

    // POST /Returns/Create
    [HttpPost]
    public async Task<IActionResult> Create(AssetReturn assetReturn)
    {
        if (ModelState.IsValid && await SendAsync(HttpMethod.Post, "api/returns", assetReturn))
        {
            TempData["Success"] = "Return recorded.";
            return RedirectToAction(nameof(Index));
        }

        // Show the page again with what the user picked
        await LoadPageDataAsync();
        return View(nameof(Index), assetReturn);
    }

    // Open allocations for the dropdown, and the list of returned assets
    private async Task LoadPageDataAsync()
    {
        ViewBag.OpenAllocations = await GetAsync<List<Allocation>>("api/allocations?active=true");
        ViewBag.Returns = await GetAsync<List<AssetReturn>>("api/returns");
    }
}
