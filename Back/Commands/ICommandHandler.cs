using System.Text.Json;
using Estud.Back.Converters;

namespace Estud.Back.Commands;

public interface ICommandHandler<T> where T : ICommand
{
    Task<OneOf<EstudSuccess, EstudError>> Handle(int commandId, T command);
}

public interface ICommandInvoker
{
    Task<OneOf<EstudSuccess, EstudError>> Invoke(IServiceProvider sp, int commandId, string json);
    object Deserialize(string json);
}

public class CommandInvoker<T> : ICommandInvoker where T : ICommand
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new EstudStringEnumConverter() },
    };

    public async Task<OneOf<EstudSuccess, EstudError>> Invoke(IServiceProvider sp, int commandId, string json)
    {
        var data = JsonSerializer.Deserialize<T>(json, _options)!;
        var handler = sp.GetRequiredService<ICommandHandler<T>>();
        return await handler.Handle(commandId, data);
    }

    public object Deserialize(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}
