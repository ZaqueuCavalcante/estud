namespace Estud.Back.Emails;

public class FakeEmailsService : IEmailsService
{
    private readonly EmailSettings _settings;
    public List<string> InviteEmails = [];
    public List<string> ResetPasswordEmails = [];
    public List<string> FirstAccessMagicLinkEmails = [];

    public FakeEmailsService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendResetPasswordEmail(string to, string token)
    {
        await Task.Yield();
        var link = $"{_settings.FrontUrl}/reset-password?token={token}";
        Console.WriteLine($"SendResetPasswordEmail [{to} -> {link}]");
        ResetPasswordEmails.Add($"[{to} -> {link}]");
    }

    public async Task SendFirstAccessMagicLinkEmail(string to, string token)
    {
        await Task.Yield();
        var link = $"{_settings.FrontUrl}/magic-link?token={token}";
        Console.WriteLine($"SendFirstAccessMagicLinkEmail [{to} -> {link}]");
        FirstAccessMagicLinkEmails.Add($"[{to} -> {link}]");
    }

    public async Task SendInviteEmail(string to, string name, string institution, string role, string token)
    {
        await Task.Yield();
        var link = $"{_settings.FrontUrl}/magic-link?token={token}";
        Console.WriteLine($"SendInviteEmail [{to} -> {link}]");
        InviteEmails.Add($"[{to} -> {link}]");
    }
}
