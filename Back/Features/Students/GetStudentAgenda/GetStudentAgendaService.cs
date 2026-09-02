namespace Estud.Back.Features.Students.GetStudentAgenda;

public class GetStudentAgendaService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetStudentAgendaOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classes = await GetStudentClasses(institutionId, studentId);

        var agenda = classes
            .GroupBy(x => x.Day)
            .OrderBy(g => g.Key)
            .Select(g => new GetStudentAgendaItemOut
            {
                Day = g.Key,
                Disciplines = g.OrderBy(x => x.Start).Select(x => new GetStudentAgendaItemDisciplineOut
                {
                    ClassId = x.Id,
                    Name = x.Discipline,
                    ClassroomName = x.Classroom,
                    Start = x.Start,
                    End = x.End
                }).ToList()
            })
            .ToList();

        return new GetStudentAgendaOut { Days = agenda };
    }

    private async Task<List<GetStudentAgendaDto>> GetStudentClasses(int institutionId, int studentId)
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
                estud.classes__students cs
            INNER JOIN
                estud.classes c ON c.id = cs.class_id
            INNER JOIN
                estud.disciplines d ON d.id = c.discipline_id
            INNER JOIN
                estud.schedules s ON s.class_id = c.id
            LEFT JOIN
                estud.classrooms cr ON cr.id = s.classroom_id
            WHERE
                c.institution_id = {0}
                    AND
                c.status = {1}
                    AND
                cs.student_id = {2}
                    AND
                cs.status = {3}
            ORDER BY
                s.day, s.start
        ";

        return await ctx.Database
            .SqlQueryRaw<GetStudentAgendaDto>(sql, institutionId, ClassStatus.Started.ToInt(), studentId, StudentClassStatus.Matriculado.ToInt())
            .AsNoTracking().ToListAsync();
    }
}
