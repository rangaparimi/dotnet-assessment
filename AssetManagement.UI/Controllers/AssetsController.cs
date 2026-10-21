using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AssetsController : BaseController
{
    public AssetsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Assets?page=2  or  /Assets?editId=5&page=2 (fills the form with asset 5, which is on that page)
    public async Task<IActionResult> Index(int? editId, int page = 1)
    {
        await LoadPageAsync(page);

        var assets = (List<Asset>)ViewBag.Assets;
        var form = assets.FirstOrDefault(a => a.Id == editId) ?? new Asset();
        return View(form);
    }

    // POST /Assets/Save - adds a new asset (Id = 0) or updates an existing one
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Save(Asset asset, int page = 1)
    {
        if (ModelState.IsValid)
        {
            var saved = asset.Id == 0
                ? await SendAsync(HttpMethod.Post, "api/assets", asset)
                : await SendAsync(HttpMethod.Put, $"api/assets/{asset.Id}", asset);

            if (saved)
            {
                TempData["Success"] = $"Asset {asset.AssetTag} saved.";
                return RedirectToAction(nameof(Index), new { page });   // stay on the same page
            }
        }

        // Show the page again with what the user typed
        await LoadPageAsync(page);
        return View(nameof(Index), asset);
    }

    // POST /Assets/Delete/5
    [HttpPost]
    [Authorize(Roles = "Admin")]   // viewers can look but not change
    public async Task<IActionResult> Delete(int id, int page = 1)
    {
        await SendAsync(HttpMethod.Delete, $"api/assets/{id}");
        return RedirectToAction(nameof(Index), new { page });   // the API moves back a page if this one is now empty
    }

    // One page of assets for the table and the page bar
    private async Task LoadPageAsync(int page)
    {
        var paged = await GetPageAsync<Asset>("api/assets/paged", page);
        ViewBag.Assets = paged.Items;
        ViewBag.Paged = paged;
    }
}
