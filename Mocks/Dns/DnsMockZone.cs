using System.Collections.Concurrent;

namespace Estud.Mocks.Dns;

public record DnsMockRecord(List<string> Txt, bool ServerFailure = false);

public static class DnsMockZone
{
    public static readonly ConcurrentDictionary<string, DnsMockRecord> Records = new(StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string name) => name.Trim().TrimEnd('.');
}
