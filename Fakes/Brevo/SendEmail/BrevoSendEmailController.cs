using Estud.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Estud.Fakes.Brevo.SendEmail;

/// <summary>
/// Fake do endpoint transacional do Brevo (POST /v3/smtp/email).
/// Guarda os e-mails recebidos na caixa fake e loga os links, para uso em Development.
/// </summary>
[ApiController]
public partial class BrevoSendEmailController(ILogger<BrevoSendEmailController> logger) : ControllerBase
{
    [HttpPost("brevo/v3/smtp/email")]
    public IActionResult Send([FromBody] BrevoFakeEmail data)
    {
        var apiKey = Request.Headers["api-key"].ToString();
        if (apiKey.IsEmpty()) return Unauthorized(new { code = "unauthorized", message = "Key not found" });

        if (data.Sender is null || data.Sender.Email.IsEmpty() || data.To.Count == 0 || data.Subject.IsEmpty() || data.HtmlContent.IsEmpty())
            return BadRequest(new { code = "missing_parameter", message = "sender, to, subject and htmlContent are required" });

        BrevoFakeMailbox.Emails.Enqueue(data);

        var links = HrefRegex().Matches(data.HtmlContent).Select(m => m.Groups[1].Value).Distinct();
        logger.LogInformation("[Brevo] {Subject} -> {To} | {Links}",
            data.Subject, string.Join(", ", data.To.Select(x => x.Email)), string.Join(" ", links));

        return StatusCode(201, new { messageId = $"<{Guid.NewGuid()}@smtp-relay.mailin.fr>" });
    }

    [GeneratedRegex("href=\"([^\"]+)\"")]
    private static partial Regex HrefRegex();
}
