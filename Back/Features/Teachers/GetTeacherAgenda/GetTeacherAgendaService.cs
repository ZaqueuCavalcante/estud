namespace Estud.Back.Features.Teachers.GetTeacherAgenda;

public class GetTeacherAgendaService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetTeacherAgendaOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var classes = await GetTeacherClasses(institutionId, teacherId);

        var agenda = classes
            .GroupBy(x => x.Day)
            .OrderBy(x => x.Key)
            .Select(x => new GetTeacherAgendaItemOut
            {
                Day = x.Key,
                Disciplines = x.OrderBy(d => d.Start).Select(d => new GetTeacherAgendaItemDisciplineOut
                {
                    ClassId = d.Id,
                    Name = d.Discipline,
                    ClassroomName = d.Classroom,
                    Start = d.Start,
                    End = d.End
                }).ToList()
            })
            .ToList();

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
                s.day, s.start
        ";

        return await ctx.Database
            .SqlQueryRaw<GetTeacherAgendaDto>(sql, institutionId, ClassStatus.Started.ToInt(), teacherId)
            .AsNoTracking().ToListAsync();
    }
}
