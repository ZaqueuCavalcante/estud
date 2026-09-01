namespace Estud.Back.Dns;

public class DnsTxtLookup
{
    public List<string> Records { get; init; } = [];
    public string? Error { get; init; }

    public bool Failed => Error.HasValue();

    public static DnsTxtLookup Ok(List<string> records) => new() { Records = records };
    public static DnsTxtLookup Fail(string error) => new() { Error = error };
}
