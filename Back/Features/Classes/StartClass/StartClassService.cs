namespace Estud.Back.Features.Classes.StartClass;

public class StartClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Start(int classId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes
            .Include(c => c.Period)
            .Include(c => c.Teachers)
            .Include(c => c.Schedules)
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        if (@class.Status != ClassStatus.OnEnrollment) return ClassMustBeOnEnrollment.I;

        // Checkpoint de montagem: a turma só inicia com o conjunto completo (professores + horários),
        // pois as aulas derivam dos horários e ficam congeladas a partir daqui.
        if (@class.Teachers.Count == 0) return ClassWithoutTeachers.I;
        if (@class.Schedules.Count == 0) return ClassWithoutSchedules.I;

        // Turma não presencial não tem campus, então só o nível da instituição
        // (e o global) recorta as aulas dela.
        var calendar = await ctx.GetCalendarResolver(@class.CampusId, @class.Period.StartAt, @class.Period.EndAt);

        @class.CreateLessons(calendar);
        @class.Status = ClassStatus.Started;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
