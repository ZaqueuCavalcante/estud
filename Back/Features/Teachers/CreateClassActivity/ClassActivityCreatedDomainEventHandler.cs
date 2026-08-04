using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Teachers.CreateClassActivity;

public class ClassActivityCreatedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<ClassActivityCreatedDomainEvent>
{
    public async Task Handle(int institutionId, int eventId, ClassActivityCreatedDomainEvent evt)
    {
        var activityId = await ctx.ClassActivities.AsNoTracking()
            .Where(x => x.Uid == evt.Uid)
            .Select(x => x.Id)
            .FirstAsync();

        ctx.AddCommand(institutionId, new CreateNewClassActivityNotificationCommand(activityId));
    }
}
