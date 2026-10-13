using AssetManagement.UI.Services;
using AssetManagement.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AssetsController : Controller
{
    private readonly ApiClient _api;

    public AssetsController(ApiClient api)
    {
        _api = api;
    }

    // GET /Assets            -> list + empty "Add Asset" form
    // GET /Assets?editId=5   -> list + form filled with asset 5
    public async Task<IActionResult> Index(int? editId)
    {
        var model = await BuildPageAsync(new AssetFormModel());

        if (editId.HasValue)
        {
            var asset = model.Assets.FirstOrDefault(a => a.Id == editId);
            if (asset == null)
                TempData["Error"] = $"Asset with id {editId} was not found.";
            else
                model.Form = AssetFormModel.FromDto(asset);
        }

        return View(model);
    }

    // POST /Assets/Save - add when Form.Id is empty, update otherwise
    [HttpPost]
    public async Task<IActionResult> Save([Bind(Prefix = "Form")] AssetFormModel form)
    {
        if (ModelState.IsValid)
        {
            var result = form.Id is int id
                ? await _api.UpdateAssetAsync(id, form)
                : await _api.CreateAssetAsync(form);

            if (result.Success)
            {
                TempData["Success"] = $"Asset {result.Data!.AssetTag} {(form.Id.HasValue ? "updated" : "added")}.";
                return RedirectToAction(nameof(Index));   // Post/Redirect/Get: F5 won't re-submit
            }

            TempData["Error"] = result.Error;
        }

        // Validation or API error: show the page again with what the user typed
        return View(nameof(Index), await BuildPageAsync(form));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteAssetAsync(id);
        if (result.Success)
            TempData["Success"] = result.Data!.Message;
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    private async Task<AssetsViewModel> BuildPageAsync(AssetFormModel form)
    {
        var assets = await _api.GetAssetsAsync();
        if (!assets.Success)
            TempData["Error"] = assets.Error;

        return new AssetsViewModel { Assets = assets.Data ?? [], Form = form };
    }
}
