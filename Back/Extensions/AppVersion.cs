using System.Reflection;

namespace Estud.Back.Extensions;

public static class AppVersion
{
    public static string Value { get; } = Resolve();

    private static string Resolve()
    {
        var informational = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (informational.IsEmpty()) return "unknown";

        var commit = informational!.Split('+').Last();
        return commit.Length >= 7 ? commit[..7] : commit;
    }
}
