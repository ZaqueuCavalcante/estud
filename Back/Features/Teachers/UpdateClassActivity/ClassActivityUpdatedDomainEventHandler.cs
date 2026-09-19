using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Teachers.UpdateClassActivity;

public class ClassActivityUpdatedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<ClassActivityUpdatedDomainEvent>
{
    public async Task Handle(int institutionId, int eventId, ClassActivityUpdatedDomainEvent evt)
    {
        var activityId = await ctx.ClassActivities.AsNoTracking()
            .Where(x => x.Uid == evt.Uid)
            .Select(x => x.Id)
            .FirstAsync();

        ctx.AddCommand(institutionId, new CreateUpdatedClassActivityNotificationCommand(activityId));
    }
}
