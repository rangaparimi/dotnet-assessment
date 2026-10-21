using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class ReturnsController : BaseController
{
    public ReturnsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Returns?page=2  or  /Returns?allocationId=7 (pre-selects allocation 7)
    public async Task<IActionResult> Index(int? allocationId, int page = 1)
    {
        await LoadPageDataAsync(page);
        return View(new AssetReturn { AllocationId = allocationId });
    }

    // POST /Returns/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Create(AssetReturn assetReturn)
    {
        if (ModelState.IsValid && await SendAsync(HttpMethod.Post, "api/returns", assetReturn))
        {
            TempData["Success"] = "Return recorded.";
            return RedirectToAction(nameof(Index));   // newest first, so page 1 shows the new return
        }

        // Show the page again with what the user picked
        await LoadPageDataAsync(1);
        return View(nameof(Index), assetReturn);
    }

    // Open allocations for the dropdown (all of them), and one page of returned assets
    private async Task LoadPageDataAsync(int page)
    {
        var paged = await GetPageAsync<AssetReturn>("api/returns/paged", page);

        ViewBag.OpenAllocations = await GetAsync<List<Allocation>>("api/allocations?active=true");
        ViewBag.Returns = paged.Items;
        ViewBag.Paged = paged;
    }
}
