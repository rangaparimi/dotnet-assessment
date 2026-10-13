namespace AssetManagement.UI.Services;

// Result of one API call: the data on success, or a message to show the user
public class ApiResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public string Error { get; private init; } = "";

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string error) => new() { Error = error };
}
