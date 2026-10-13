using AssetManagement.UI.Services;
using AssetManagement.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class EmployeesController : Controller
{
    private readonly ApiClient _api;

    public EmployeesController(ApiClient api)
    {
        _api = api;
    }

    // GET /Employees              -> list + empty "Add Employee" form
    // GET /Employees?editId=3     -> form filled with employee 3
    // GET /Employees?heldById=3   -> also show the assets employee 3 holds
    public async Task<IActionResult> Index(int? editId, int? heldById)
    {
        var model = await BuildPageAsync(new EmployeeFormModel());

        if (editId.HasValue)
        {
            var employee = model.Employees.FirstOrDefault(e => e.Id == editId);
            if (employee == null)
                TempData["Error"] = $"Employee with id {editId} was not found.";
            else
                model.Form = EmployeeFormModel.FromDto(employee);
        }

        if (heldById.HasValue)
        {
            var held = await _api.GetHeldAssetsAsync(heldById.Value);
            if (held.Success)
            {
                model.HeldBy = model.Employees.FirstOrDefault(e => e.Id == heldById);
                model.HeldAssets = held.Data!;
            }
            else
            {
                TempData["Error"] = held.Error;
            }
        }

        return View(model);
    }

    // POST /Employees/Save - add when Form.Id is empty, update otherwise
    [HttpPost]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] EmployeeFormModel form)
    {
        if (ModelState.IsValid)
        {
            var result = form.Id is int id
                ? await _api.UpdateEmployeeAsync(id, form)
                : await _api.CreateEmployeeAsync(form);

            if (result.Success)
            {
                TempData["Success"] = $"Employee {result.Data!.EmployeeCode} {(form.Id.HasValue ? "updated" : "added")}.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result.Error;
        }

        return View(nameof(Index), await BuildPageAsync(form));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEmployeeAsync(id);
        if (result.Success)
            TempData["Success"] = result.Data!.Message;
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeesViewModel> BuildPageAsync(EmployeeFormModel form)
    {
        var employees = await _api.GetEmployeesAsync();
        if (!employees.Success)
            TempData["Error"] = employees.Error;

        return new EmployeesViewModel { Employees = employees.Data ?? [], Form = form };
    }
}
