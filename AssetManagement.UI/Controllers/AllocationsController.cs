using AssetManagement.UI.Models;
using AssetManagement.UI.Services;
using AssetManagement.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AllocationsController : Controller
{
    private readonly ApiClient _api;

    public AllocationsController(ApiClient api)
    {
        _api = api;
    }

    // GET /Allocations?show=active|returned|all
    public async Task<IActionResult> Index(string show = "active")
    {
        return View(await BuildPageAsync(new AllocationFormModel(), show));
    }

    // POST /Allocations/Create
    [HttpPost]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] AllocationFormModel form)
    {
        if (ModelState.IsValid)
        {
            var result = await _api.AllocateAsync(form);
            if (result.Success)
            {
                TempData["Success"] = $"Asset {result.Data!.AssetTag} allocated to {result.Data.EmployeeName}.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result.Error;
        }

        return View(nameof(Index), await BuildPageAsync(form, "active"));
    }

    private async Task<AllocationsViewModel> BuildPageAsync(AllocationFormModel form, string show)
    {
        bool? active = show switch
        {
            "returned" => false,
            "all" => null,
            _ => true
        };

        // The three API calls don't depend on each other, so run them at the same time
        var assetsTask = _api.GetAssetsAsync();
        var employeesTask = _api.GetEmployeesAsync();
        var allocationsTask = _api.GetAllocationsAsync(active);
        await Task.WhenAll(assetsTask, employeesTask, allocationsTask);

        var assets = assetsTask.Result;
        var employees = employeesTask.Result;
        var allocations = allocationsTask.Result;

        var error = new[] { assets.Error, employees.Error, allocations.Error }.FirstOrDefault(e => e != "");
        if (error != null)
            TempData["Error"] = error;

        return new AllocationsViewModel
        {
            AvailableAssets = (assets.Data ?? []).Where(a => a.Status == AssetStatus.Available).ToList(),
            ActiveEmployees = (employees.Data ?? []).Where(e => e.IsActive).ToList(),
            Allocations = allocations.Data ?? [],
            Form = form,
            Show = active switch { true => "active", false => "returned", null => "all" }
        };
    }
}
