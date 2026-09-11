using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.Dns.Records;

public class DnsRecordIn
{
    public string Name { get; set; }
    public List<string> Txt { get; set; } = [];
    public bool ServerFailure { get; set; }
}

/// <summary>
/// Publica registros na zona fake, fazendo o papel do painel DNS onde o TI da instituição
/// cria o TXT de verificação do domínio.
/// </summary>
[ApiController]
public class DnsRecordsController : ControllerBase
{
    [HttpPost("dns/records")]
    public IActionResult Publish([FromBody] DnsRecordIn data)
    {
        DnsMockZone.Records[DnsMockZone.Normalize(data.Name)] = new DnsMockRecord(data.Txt, data.ServerFailure);

        return Ok();
    }
}
