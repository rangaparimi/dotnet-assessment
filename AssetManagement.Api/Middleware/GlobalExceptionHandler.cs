using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Middleware;

// Catches any exception that escapes a controller/service and turns it into
// a JSON response: { "message": "..." }. Expected business errors (not found,
// duplicate, asset not available...) never reach here - services return them
// as ServiceResult. This only handles the unexpected ones.
public class GlobalExceptionHandler : IExceptionHandler //here
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception for {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, message);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Message) Map(Exception exception) //in Map
    {
        // Someone else changed the same row at the same time
        if (exception is DbUpdateConcurrencyException)
            return (StatusCodes.Status409Conflict, "The record was changed by another user. Refresh and try again.");

        var sqlError = FindSqlException(exception);

        // 2601 / 2627 = unique index violated. Happens when two requests pass the
        // service's duplicate check at the same moment (e.g. same AssetTag, or the
        // same asset allocated twice).
        if (sqlError is { Number: 2601 or 2627 })
            return (StatusCodes.Status409Conflict, "This change conflicts with existing data (duplicate value or asset already allocated). Refresh and try again.");

        // 547 = foreign key violation
        if (sqlError is { Number: 547 })
            return (StatusCodes.Status400BadRequest, "This change references a record that does not exist or is still in use.");

        // Any other SQL error here means the database could not be reached or failed
        if (sqlError != null)
            return (StatusCodes.Status503ServiceUnavailable, "The database is not available. Check that SQL Server is running and the connection string is correct.");

        return (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again.");
    }

    // EF Core wraps the SqlException (e.g. DbUpdateException -> SqlException), so look through the chain
    private static SqlException? FindSqlException(Exception? exception)
    {
        while (exception != null)
        {
            if (exception is SqlException sql)
                return sql;
            exception = exception.InnerException;
        }
        return null;
    }
}
