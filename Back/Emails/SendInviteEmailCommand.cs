namespace Estud.Back.Emails;

[CommandDescription("Envia convite de acesso por e-mail.")]
public record SendInviteEmailCommand(string Email, string Name, string Institution, string Role, Guid MagicLinkId) : ICommand;

public class SendInviteEmailCommandHandler(IEmailsService emailService) : ICommandHandler<SendInviteEmailCommand>
{
    public async Task Handle(int commandId, SendInviteEmailCommand command)
    {
        await emailService.SendInviteEmail(command.Email, command.Name, command.Institution, command.Role, command.MagicLinkId.ToString());
    }
}
