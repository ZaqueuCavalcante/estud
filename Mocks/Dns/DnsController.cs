using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.Dns;

/// <summary>
/// Simula um resolvedor DNS-over-HTTPS (formato JSON do Google/Cloudflare), usado pela
/// verificação de posse de domínio do SSO. Os testes semeiam os registros TXT via
/// <c>PUT /dns/records</c> e o back consulta <c>GET /dns/resolve</c> como faria em produção.
/// </summary>
[ApiController]
public class DnsController : ControllerBase
{
    private const int TxtRecordType = 16;
    private const int NoError = 0;
    private const int NxDomain = 3;

    private static readonly ConcurrentDictionary<string, List<string>> Records = new(StringComparer.OrdinalIgnoreCase);

    [HttpPut("dns/records")]
    public IActionResult SetRecords([FromBody] SetDnsRecordsIn data)
    {
        Records[Normalize(data.Name)] = data.Values;
        return Ok(new { name = data.Name, values = data.Values });
    }

    [HttpGet("dns/resolve")]
    public IActionResult Resolve([FromQuery] string name, [FromQuery] string type = "TXT")
    {
        if (type != "TXT")
            return Ok(new DohResponseOut { Status = NoError, Answer = [] });

        var key = Normalize(name);

        if (!Records.TryGetValue(key, out var values))
            return Ok(new DohResponseOut { Status = NxDomain, Answer = [] });

        var answer = values
            .Select(v => new DohAnswerOut
            {
                Name = $"{key}.",
                Type = TxtRecordType,
                TTL = 300,
                Data = $"\"{v}\"",
            })
            .ToList();

        return Ok(new DohResponseOut { Status = NoError, Answer = answer });
    }

    private static string Normalize(string name) => name.Trim().TrimEnd('.').ToLowerInvariant();
}

public class SetDnsRecordsIn
{
    public string Name { get; set; }
    public List<string> Values { get; set; } = [];
}

public class DohResponseOut
{
    public int Status { get; set; }
    public List<DohAnswerOut> Answer { get; set; } = [];
}

public class DohAnswerOut
{
    public string Name { get; set; }
    public int Type { get; set; }
    public int TTL { get; set; }
    public string Data { get; set; }
}
