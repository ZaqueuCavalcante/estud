using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Estud.Tests.Base;

public class RequestsCounterMiddleware(RequestDelegate next)
{
    private static readonly ConcurrentDictionary<int, int> _byStatusCode = new();
    private static readonly ConcurrentDictionary<string, int> _byMethod = new();

    public static IReadOnlyDictionary<int, int> ByStatusCode => _byStatusCode;
    public static IReadOnlyDictionary<string, int> ByMethod => _byMethod;

    public async Task InvokeAsync(HttpContext context)
    {
        _byMethod.AddOrUpdate(context.Request.Method, 1, (_, count) => count + 1);

        try
        {
            await next(context);
            _byStatusCode.AddOrUpdate(context.Response.StatusCode, 1, (_, count) => count + 1);
        }
        catch
        {
            _byStatusCode.AddOrUpdate(StatusCodes.Status500InternalServerError, 1, (_, count) => count + 1);
            throw;
        }
    }
}
