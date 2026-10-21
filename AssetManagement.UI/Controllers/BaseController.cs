using System.Text.Json;
using AssetManagement.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

// Small helpers every UI controller uses to call AssetManagement.Api.
// Errors are put in TempData["Error"], which the layout shows in the red bar.
public abstract class BaseController : Controller
{
    private readonly HttpClient _api;

    protected BaseController(IHttpClientFactory httpClientFactory)
    {
        _api = httpClientFactory.CreateClient("Api");
    }

    // GET from the API. Returns an empty list/object if the call fails.
    protected async Task<T> GetAsync<T>(string url) where T : new()
    {
        try
        {
            var response = await _api.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>() ?? new T();

            TempData["Error"] = await ReadMessageAsync(response) ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Cannot reach the API. Is AssetManagement.Api running?";
        }
        return new T();
    }

    // Rows per page on every list
    protected const int PageSize = 10;

    // GET one page from a ".../paged" endpoint. An empty page is returned if the call fails.
    protected Task<PagedResult<T>> GetPageAsync<T>(string url, int page)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return GetAsync<PagedResult<T>>($"{url}{separator}page={page}&pageSize={PageSize}");
    }

    // POST, PUT or DELETE to the API. Returns true if it worked.
    protected async Task<bool> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null)
            request.Content = JsonContent.Create(body);

        try
        {
            var response = await _api.SendAsync(request);
            var message = await ReadMessageAsync(response);

            if (response.IsSuccessStatusCode)
            {
                if (message != null)
                    TempData["Success"] = message;      // e.g. delete: "Asset LAP-01 was deleted."
                return true;
            }

            TempData["Error"] = message ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Cannot reach the API. Is AssetManagement.Api running?";
        }
        return false;
    }

    // The API sends { "message": "..." } for business errors and deletes,
    // and { "errors": { "Field": ["..."] } } for validation errors
    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.ValueKind != JsonValueKind.Object)
                return null;

            if (json.TryGetProperty("message", out var message))
                return message.GetString();

            if (json.TryGetProperty("errors", out var errors))
                return string.Join(" ", errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray())
                    .Select(v => v.GetString()));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Empty or non-JSON body
        }
        return null;
    }
}
