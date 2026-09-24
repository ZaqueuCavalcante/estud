using System.Collections.Concurrent;

namespace Estud.Fakes.Dns;

public record DnsFakeRecord(List<string> Txt, bool ServerFailure = false);

public static class DnsFakeZone
{
    public static readonly ConcurrentDictionary<string, DnsFakeRecord> Records = new(StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string name) => name.Trim().TrimEnd('.');
}
