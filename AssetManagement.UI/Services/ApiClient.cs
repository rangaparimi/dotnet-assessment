using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.UI.Models;
using AssetManagement.UI.ViewModels;

namespace AssetManagement.UI.Services;

// Typed HttpClient for AssetManagement.Api. The UI server calls the API
// server-to-server, so the browser never talks to the API (no CORS needed).
// Calls never throw for API errors: they return an ApiResult with the API's message.
public class ApiClient
{
    // Same JSON rules as the API: camelCase names, enums as text ("Available")
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---------- Assets ----------

    public Task<ApiResult<List<AssetDto>>> GetAssetsAsync() =>
        SendAsync<List<AssetDto>>(HttpMethod.Get, "api/assets");

    public Task<ApiResult<AssetDto>> CreateAssetAsync(AssetFormModel form) =>
        SendAsync<AssetDto>(HttpMethod.Post, "api/assets", form);

    public Task<ApiResult<AssetDto>> UpdateAssetAsync(int id, AssetFormModel form) =>
        SendAsync<AssetDto>(HttpMethod.Put, $"api/assets/{id}", form);

    public Task<ApiResult<MessageDto>> DeleteAssetAsync(int id) =>
        SendAsync<MessageDto>(HttpMethod.Delete, $"api/assets/{id}");

    // ---------- Employees ----------

    public Task<ApiResult<List<EmployeeDto>>> GetEmployeesAsync() =>
        SendAsync<List<EmployeeDto>>(HttpMethod.Get, "api/employees");

    public Task<ApiResult<EmployeeDto>> CreateEmployeeAsync(EmployeeFormModel form) =>
        SendAsync<EmployeeDto>(HttpMethod.Post, "api/employees", form);

    public Task<ApiResult<EmployeeDto>> UpdateEmployeeAsync(int id, EmployeeFormModel form) =>
        SendAsync<EmployeeDto>(HttpMethod.Put, $"api/employees/{id}", form);

    public Task<ApiResult<MessageDto>> DeleteEmployeeAsync(int id) =>
        SendAsync<MessageDto>(HttpMethod.Delete, $"api/employees/{id}");

    public Task<ApiResult<List<HeldAssetDto>>> GetHeldAssetsAsync(int employeeId) =>
        SendAsync<List<HeldAssetDto>>(HttpMethod.Get, $"api/employees/{employeeId}/assets");

    // ---------- Allocations ----------

    // active: true = not returned yet, false = returned, null = all
    public Task<ApiResult<List<AllocationDto>>> GetAllocationsAsync(bool? active)
    {
        var path = active switch
        {
            true => "api/allocations?active=true",
            false => "api/allocations?active=false",
            null => "api/allocations"
        };
        return SendAsync<List<AllocationDto>>(HttpMethod.Get, path);
    }

    public Task<ApiResult<AllocationDto>> AllocateAsync(AllocationFormModel form) =>
        SendAsync<AllocationDto>(HttpMethod.Post, "api/allocations", form);

    // ---------- Returns ----------

    public Task<ApiResult<List<ReturnDto>>> GetReturnsAsync() =>
        SendAsync<List<ReturnDto>>(HttpMethod.Get, "api/returns");

    public Task<ApiResult<ReturnDto>> ReturnAsync(ReturnFormModel form) =>
        SendAsync<ReturnDto>(HttpMethod.Post, "api/returns", form);

    // ---------- Helpers ----------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult<T>.Fail($"Cannot reach the API at {_http.BaseAddress}. Is AssetManagement.Api running?");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return ApiResult<T>.Fail(await ReadErrorAsync(response));

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return data == null
                ? ApiResult<T>.Fail("The API returned an empty response.")
                : ApiResult<T>.Ok(data);
        }
    }

    // Business errors come as { message }, validation errors as { title, errors: { field: [..] } }
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return fallback;

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString()!;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                return string.Join(" ", errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray())
                    .Select(v => v.GetString()));

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString()!;
        }
        catch (JsonException)
        {
            // Not JSON - fall back to the generic message
        }
        return fallback;
    }
}
