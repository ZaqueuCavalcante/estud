namespace Estud.Back.Features.Teachers.GetTeacherAgenda;

public class GetTeacherAgendaService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetTeacherAgendaOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var classes = await ctx.Classes.AsNoTracking()
            .Include(x => x.Discipline)
            .Include(x => x.Schedules).ThenInclude(s => s.Classroom)
            .Where(x => x.InstitutionId == institutionId && x.Teachers.Any(x => x.Id == teacherId) && x.Status == ClassStatus.Started)
            .ToListAsync();

        // Pra cada dia, pegar as aulas que acontecem nesse dia, ordenadas pelo horário de início
        var days = classes.Select(x => x.Schedules.Where(s => s.TeacherId == teacherId).Select(s => s.Day)).SelectMany(x => x).Distinct().OrderBy(x => x).ToList();
        var agenda = new List<GetTeacherAgendaItemOut>();

        foreach (var day in days)
        {
            var dayClasses = classes.Where(c => c.Schedules.Any(s => s.Day == day && s.TeacherId == teacherId)).ToList();
            var disciplines = dayClasses.SelectMany(c => c.Schedules.Where(s => s.Day == day && s.TeacherId == teacherId).Select(s => new GetTeacherAgendaItemDisciplineOut
            {
                ClassId = c.Id,
                Name = c.Discipline.Name,
                ClassroomName = s.Classroom != null ? s.Classroom.Name : null,
                Start = s.Start,
                End = s.End
            })).OrderBy(d => d.Start).ToList();

            agenda.Add(new GetTeacherAgendaItemOut
            {
                Day = day,
                Disciplines = disciplines
            });
        }

        return new GetTeacherAgendaOut { Days = agenda };
    }

    private async Task<List<GetTeacherAgendaDto>> GetTeacherClasses(int institutionId, int teacherId)
    {
        const string sql = @"
            SELECT
                c.id,
                d.name AS discipline,
                s.day,
                s.start,
                s.end,
                cr.name AS classroom
            FROM
                estud.classes__teachers ct
            INNER JOIN
                estud.classes c ON c.id = ct.class_id
            INNER JOIN
                estud.disciplines d ON d.id = c.discipline_id
            INNER JOIN
                estud.schedules s ON s.class_id = c.id AND s.teacher_id = ct.teacher_id
            LEFT JOIN
                estud.classrooms cr ON cr.id = s.classroom_id
            WHERE
                c.institution_id = {0}
                    AND
                c.status = {1}
                    AND
                ct.teacher_id = {2}
            ORDER BY
                c.id, s.DAY, s.start
        ";

        return await ctx.Database
            .SqlQueryRaw<GetTeacherAgendaDto>(sql, institutionId, ClassStatus.Started.ToInt(), teacherId)
            .AsNoTracking().ToListAsync();
    }
}
