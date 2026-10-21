using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class EmployeesController : BaseController
{
    public EmployeesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Employees  or  /Employees?editId=3 (fills the form)
    //                 or  /Employees?heldById=3 (shows the assets employee 3 holds)
    public async Task<IActionResult> Index(int? editId, int? heldById)
    {
        var employees = await GetAsync<List<Employee>>("api/employees");
        ViewBag.Employees = employees;

        if (heldById != null)
        {
            var open = await GetAsync<List<Allocation>>("api/allocations?active=true");
            ViewBag.HeldBy = employees.FirstOrDefault(e => e.Id == heldById);
            ViewBag.HeldAssets = open.Where(a => a.EmployeeId == heldById).ToList();
        }

        var form = employees.FirstOrDefault(e => e.Id == editId) ?? new Employee();
        return View(form);
    }

    // POST /Employees/Save - adds a new employee (Id = 0) or updates an existing one
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Save(Employee employee)
    {
        if (ModelState.IsValid)
        {
            var saved = employee.Id == 0
                ? await SendAsync(HttpMethod.Post, "api/employees", employee)
                : await SendAsync(HttpMethod.Put, $"api/employees/{employee.Id}", employee);

            if (saved)
            {
                TempData["Success"] = $"Employee {employee.EmployeeCode} saved.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Show the page again with what the user typed
        ViewBag.Employees = await GetAsync<List<Employee>>("api/employees");
        return View(nameof(Index), employee);
    }

    // POST /Employees/Delete/3
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Delete(int id)
    {
        await SendAsync(HttpMethod.Delete, $"api/employees/{id}");
        return RedirectToAction(nameof(Index));
    }
}
