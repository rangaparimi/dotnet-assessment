using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AllocationsController : BaseController
{
    public AllocationsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Allocations?show=active|returned|all&page=2
    public async Task<IActionResult> Index(string show = "active", int page = 1)
    {
        await LoadPageDataAsync(show, page);
        return View(new Allocation());
    }

    // POST /Allocations/Create
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Create(Allocation allocation)
    {
        if (ModelState.IsValid && await SendAsync(HttpMethod.Post, "api/allocations", allocation))
        {
            TempData["Success"] = "Asset allocated.";
            return RedirectToAction(nameof(Index));   // newest first, so page 1 shows the new allocation
        }

        // Show the page again with what the user picked
        await LoadPageDataAsync("active", 1);
        return View(nameof(Index), allocation);
    }

    // Dropdowns (available assets, active employees) and one page of the allocation list
    private async Task LoadPageDataAsync(string show, int page)
    {
        // The dropdowns need every asset and employee, so they use the full lists
        var assets = await GetAsync<List<Asset>>("api/assets");
        var employees = await GetAsync<List<Employee>>("api/employees");

        var url = show switch
        {
            "returned" => "api/allocations/paged?active=false",
            "all" => "api/allocations/paged",
            _ => "api/allocations/paged?active=true"
        };
        var paged = await GetPageAsync<Allocation>(url, page);

        ViewBag.Assets = assets.Where(a => a.Status == "Available").ToList();
        ViewBag.Employees = employees.Where(e => e.IsActive).ToList();
        ViewBag.Allocations = paged.Items;
        ViewBag.Paged = paged;
        ViewBag.Show = show;
    }
}
