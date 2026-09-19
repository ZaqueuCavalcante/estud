using Estud.Back.Domain.Notifications;

namespace Estud.Back.Features.Teachers.UpdateClassActivity;

[CommandDescription("Criar notificação de atividade alterada")]
public record CreateUpdatedClassActivityNotificationCommand(int ClassActivityId) : ICommand;

public class CreateUpdatedClassActivityNotificationCommandHandler(EstudDbContext ctx) : ICommandHandler<CreateUpdatedClassActivityNotificationCommand>
{
    public async Task Handle(int commandId, CreateUpdatedClassActivityNotificationCommand command)
    {
        var activity = await ctx.ClassActivities.Where(x => x.Id == command.ClassActivityId)
            .Select(x => new { x.ClassId, x.Title }).FirstAsync();

        var @class = await ctx.Classes.AsNoTracking()
            .Include(x => x.Discipline)
            .FirstAsync(x => x.Id == activity.ClassId);

        var userIds = await ctx.ClassStudents.AsNoTracking()
            .Where(x => x.ClassId == @class.Id)
            .Select(x => x.Student!.UserId)
            .ToListAsync();

        var notification = Notification.UpdatedClassActivity(
            @class.InstitutionId,
            @class.Id,
            command.ClassActivityId,
            @class.Discipline.Name,
            activity.Title
        );
        ctx.Add(notification);

        foreach (var userId in userIds)
        {
            ctx.Add(new UserNotification(userId, notification));
        }
    }
}
