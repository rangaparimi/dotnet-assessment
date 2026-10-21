using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class EmployeesController : BaseController
{
    public EmployeesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Employees?page=2  or  /Employees?editId=3&page=2 (fills the form with employee 3, who is on that page)
    //                        or  /Employees?heldById=3&page=2 (shows the assets employee 3 holds)
    public async Task<IActionResult> Index(int? editId, int? heldById, int page = 1)
    {
        await LoadPageAsync(page);
        var employees = (List<Employee>)ViewBag.Employees;

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
    public async Task<IActionResult> Save(Employee employee, int page = 1)
    {
        if (ModelState.IsValid)
        {
            var saved = employee.Id == 0
                ? await SendAsync(HttpMethod.Post, "api/employees", employee)
                : await SendAsync(HttpMethod.Put, $"api/employees/{employee.Id}", employee);

            if (saved)
            {
                TempData["Success"] = $"Employee {employee.EmployeeCode} saved.";
                return RedirectToAction(nameof(Index), new { page });   // stay on the same page
            }
        }

        // Show the page again with what the user typed
        await LoadPageAsync(page);
        return View(nameof(Index), employee);
    }

    // POST /Employees/Delete/3
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Delete(int id, int page = 1)
    {
        await SendAsync(HttpMethod.Delete, $"api/employees/{id}");
        return RedirectToAction(nameof(Index), new { page });   // the API moves back a page if this one is now empty
    }

    // One page of employees for the table and the page bar
    private async Task LoadPageAsync(int page)
    {
        var paged = await GetPageAsync<Employee>("api/employees/paged", page);
        ViewBag.Employees = paged.Items;
        ViewBag.Paged = paged;
    }
}
