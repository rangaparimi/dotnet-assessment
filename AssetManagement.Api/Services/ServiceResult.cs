namespace AssetManagement.Api.Services;

// Small wrapper so services can report success or a business-rule failure
// without knowing anything about HTTP. Controllers turn it into a response.
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public int StatusCode { get; private set; }
    public string Message { get; private set; } = "";

    public static ServiceResult<T> Ok(T data) =>
        new() { Success = true, Data = data, StatusCode = StatusCodes.Status200OK };

    public static ServiceResult<T> NotFound(string message) =>
        new() { StatusCode = StatusCodes.Status404NotFound, Message = message };

    public static ServiceResult<T> BadRequest(string message) =>
        new() { StatusCode = StatusCodes.Status400BadRequest, Message = message };

    public static ServiceResult<T> Conflict(string message) =>
        new() { StatusCode = StatusCodes.Status409Conflict, Message = message };
}
