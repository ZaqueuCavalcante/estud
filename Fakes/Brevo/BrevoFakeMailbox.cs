using System.Collections.Concurrent;

namespace Estud.Fakes.Brevo;

public class BrevoFakeContact
{
    public string? Name { get; set; }
    public string Email { get; set; }
}

public class BrevoFakeEmail
{
    public BrevoFakeContact Sender { get; set; }
    public List<BrevoFakeContact> To { get; set; } = [];
    public string Subject { get; set; }
    public string HtmlContent { get; set; }
}

public static class BrevoFakeMailbox
{
    public static readonly ConcurrentQueue<BrevoFakeEmail> Emails = new();
}
