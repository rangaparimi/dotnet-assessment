using AssetManagement.UI.Services;
using AssetManagement.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class ReturnsController : Controller
{
    private readonly ApiClient _api;

    public ReturnsController(ApiClient api)
    {
        _api = api;
    }

    // GET /Returns
    // GET /Returns?allocationId=7  -> "Return" link from the Allocation page, pre-selects allocation 7
    public async Task<IActionResult> Index(int? allocationId)
    {
        return View(await BuildPageAsync(new ReturnFormModel { AllocationId = allocationId }));
    }

    // POST /Returns/Create
    [HttpPost]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] ReturnFormModel form)
    {
        if (ModelState.IsValid)
        {
            var result = await _api.ReturnAsync(form);
            if (result.Success)
            {
                TempData["Success"] = $"Asset {result.Data!.AssetTag} returned. Status is now {result.Data.AssetStatus}.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result.Error;
        }

        return View(nameof(Index), await BuildPageAsync(form));
    }

    private async Task<ReturnsViewModel> BuildPageAsync(ReturnFormModel form)
    {
        var openTask = _api.GetAllocationsAsync(active: true);
        var returnsTask = _api.GetReturnsAsync();
        await Task.WhenAll(openTask, returnsTask);

        var open = openTask.Result;
        var returns = returnsTask.Result;

        var error = new[] { open.Error, returns.Error }.FirstOrDefault(e => e != "");
        if (error != null)
            TempData["Error"] = error;

        return new ReturnsViewModel
        {
            OpenAllocations = open.Data ?? [],
            Returns = returns.Data ?? [],
            Form = form
        };
    }
}
