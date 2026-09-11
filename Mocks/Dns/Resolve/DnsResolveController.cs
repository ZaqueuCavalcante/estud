using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.Dns.Resolve;

/// <summary>
/// Resolver DNS-over-HTTPS no formato JSON (application/dns-json) do Google e da Cloudflare, só para TXT.
/// Nome sem registro publicado responde NXDOMAIN; registro marcado com ServerFailure responde SERVFAIL.
/// </summary>
[ApiController]
public class DnsResolveController : ControllerBase
{
    private const int NoError = 0;
    private const int ServerFailure = 2;
    private const int NonExistentDomain = 3;
    private const int TxtRecordType = 16;
    private const int MaxCharacterStringLength = 255;

    private static readonly JsonSerializerOptions JsonOptions = new();

    [HttpGet("dns/resolve")]
    public IActionResult Resolve([FromQuery] string name)
    {
        var normalized = DnsMockZone.Normalize(name);

        if (!DnsMockZone.Records.TryGetValue(normalized, out var record))
            return Doh(new { Status = NonExistentDomain });

        if (record.ServerFailure)
            return Doh(new { Status = ServerFailure });

        var answer = record.Txt
            .Select(txt => new { name = $"{normalized}.", type = TxtRecordType, TTL = 300, data = ToCharacterStrings(txt) })
            .ToList();

        return Doh(new { Status = NoError, Answer = answer });
    }

    /// <summary>
    /// Devolve o TXT como a Cloudflare: entre aspas e quebrado em strings de até 255 caracteres.
    /// </summary>
    private static string ToCharacterStrings(string txt)
    {
        var chunks = txt.Chunk(MaxCharacterStringLength).Select(c => $"\"{new string(c).Replace("\"", "\\\"")}\"");

        return string.Join(' ', chunks);
    }

    private static JsonResult Doh(object body)
    {
        return new JsonResult(body, JsonOptions) { ContentType = "application/dns-json" };
    }
}
