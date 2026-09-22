using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Estud.Tests.Base;

public class RequestsCounterStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseMiddleware<RequestsCounterMiddleware>();
        next(app);
    };
}
