using System.Text.Json;
using System.Diagnostics;

namespace Estud.Back.Middlewares;

public class ExceptionsMiddleware(RequestDelegate next, ILogger<ExceptionsMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var message = EnvironmentExtensions.IsDevelopmentOrTesting() ? GetFullExceptionMessage(ex) : "Erro ao executar essa ação.";
        var result = JsonSerializer.Serialize(new ErrorOut { Code = "Error", Message = message });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        logger.LogError(ex, "EstudInternalServerError - {Method} {Path}", context.Request.Method, context.Request.Path);

        Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
        Activity.Current?.AddException(ex);

        return context.Response.WriteAsync(result);
    }

    private static string GetFullExceptionMessage(Exception ex)
    {
        var message = ex.Message;
        if (ex.InnerException is not null)
        {
            message += " --> " + GetFullExceptionMessage(ex.InnerException);
        }
        return message;
    }
}
