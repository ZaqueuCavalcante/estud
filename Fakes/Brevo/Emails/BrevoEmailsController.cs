using Microsoft.AspNetCore.Mvc;

namespace Estud.Fakes.Brevo.Emails;

/// <summary>
/// Lista os e-mails recebidos pelo mock do Brevo, filtrando pelo destinatário.
/// </summary>
[ApiController]
public class BrevoEmailsController : ControllerBase
{
    [HttpGet("brevo/emails")]
    public IActionResult List([FromQuery] string to)
    {
        var emails = BrevoFakeMailbox.Emails
            .Where(e => e.To.Any(x => string.Equals(x.Email, to, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Ok(emails);
    }
}
