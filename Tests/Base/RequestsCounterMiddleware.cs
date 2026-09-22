using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Estud.Tests.Base;

public class RequestsCounterMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<int, int> _counts = new();

    public static IReadOnlyDictionary<int, int> Counts => _counts;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
            _counts.AddOrUpdate(context.Response.StatusCode, 1, (_, count) => count + 1);
        }
        catch
        {
            _counts.AddOrUpdate(StatusCodes.Status500InternalServerError, 1, (_, count) => count + 1);
            throw;
        }
    }
}
