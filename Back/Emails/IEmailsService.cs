namespace Estud.Back.Emails;

public interface IEmailsService
{
    Task SendResetPasswordEmail(string to, string token);
    Task SendFirstAccessMagicLinkEmail(string to, string token);
    Task SendInviteEmail(string to, string name, string institution, string role, string token);
}
