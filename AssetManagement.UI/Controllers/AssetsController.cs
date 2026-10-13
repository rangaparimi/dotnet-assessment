using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AssetsController : BaseController
{
    public AssetsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // GET /Assets  or  /Assets?editId=5 (fills the form with asset 5)
    public async Task<IActionResult> Index(int? editId)
    {
        var assets = await GetAsync<List<Asset>>("api/assets");
        ViewBag.Assets = assets;

        var form = assets.FirstOrDefault(a => a.Id == editId) ?? new Asset();
        return View(form);
    }

    // POST /Assets/Save - adds a new asset (Id = 0) or updates an existing one
    [HttpPost]
    public async Task<IActionResult> Save(Asset asset)
    {
        if (ModelState.IsValid)
        {
            var saved = asset.Id == 0
                ? await SendAsync(HttpMethod.Post, "api/assets", asset)
                : await SendAsync(HttpMethod.Put, $"api/assets/{asset.Id}", asset);

            if (saved)
            {
                TempData["Success"] = $"Asset {asset.AssetTag} saved.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Show the page again with what the user typed
        ViewBag.Assets = await GetAsync<List<Asset>>("api/assets");
        return View(nameof(Index), asset);
    }

    // POST /Assets/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await SendAsync(HttpMethod.Delete, $"api/assets/{id}");
        return RedirectToAction(nameof(Index));
    }
}
