using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AllocationsController : BaseController
{
    public AllocationsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Allocations?show=active|returned|all
    public async Task<IActionResult> Index(string show = "active")
    {
        await LoadPageDataAsync(show);
        return View(new Allocation());
    }

    // POST /Allocations/Create
    [HttpPost]
    public async Task<IActionResult> Create(Allocation allocation)
    {
        if (ModelState.IsValid && await SendAsync(HttpMethod.Post, "api/allocations", allocation))
        {
            TempData["Success"] = "Asset allocated.";
            return RedirectToAction(nameof(Index));
        }

        // Show the page again with what the user picked
        await LoadPageDataAsync("active");
        return View(nameof(Index), allocation);
    }

    // Dropdowns (available assets, active employees) and the allocation list
    private async Task LoadPageDataAsync(string show)
    {
        var assets = await GetAsync<List<Asset>>("api/assets");
        var employees = await GetAsync<List<Employee>>("api/employees");

        var url = show switch
        {
            "returned" => "api/allocations?active=false",
            "all" => "api/allocations",
            _ => "api/allocations?active=true"
        };

        ViewBag.Assets = assets.Where(a => a.Status == "Available").ToList();
        ViewBag.Employees = employees.Where(e => e.IsActive).ToList();
        ViewBag.Allocations = await GetAsync<List<Allocation>>(url);
        ViewBag.Show = show;
    }
}
