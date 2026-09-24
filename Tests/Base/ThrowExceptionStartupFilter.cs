using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Estud.Tests.Base;

public class ThrowExceptionStartupFilter : IStartupFilter
{
    public const string Path = "/tests/throw";
    public const string Message = "Erro lançado pelo teste";
    public const string InnerMessage = "Causa interna";

    // Registrado depois do pipeline do Back: só é alcançado quando nenhum endpoint casa,
    // e fica dentro do ExceptionsMiddleware.
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.Map(Path, branch => branch.Run(_ =>
            throw new InvalidOperationException(Message, new Exception(InnerMessage))));
    };
}
