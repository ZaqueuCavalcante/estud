namespace Estud.Back.Features.Parents.GetParentStudentAgenda;

public class GetParentStudentAgendaService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetParentStudentAgendaOut, EstudError>> Get(int studentId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var parentId = await ctx.GetParentId(institutionId, userId);

        var hasActiveLink = await ctx.ParentStudents.AnyAsync(x =>
            x.ParentId == parentId &&
            x.StudentId == studentId &&
            x.Status == ParentStudentStatus.Active &&
            !x.RevokedByStudent);
        if (!hasActiveLink) return StudentNotFound.I;

        var classes = await GetStudentClasses(institutionId, studentId);

        var agenda = classes
            .GroupBy(x => x.Day)
            .OrderBy(g => g.Key)
            .Select(g => new GetParentStudentAgendaItemOut
            {
                Day = g.Key,
                Disciplines = g.OrderBy(x => x.Start).Select(x => new GetParentStudentAgendaItemDisciplineOut
                {
                    ClassId = x.Id,
                    Name = x.Discipline,
                    ClassroomName = x.Classroom,
                    Start = x.Start,
                    End = x.End
                }).ToList()
            })
            .ToList();

        return new GetParentStudentAgendaOut { Days = agenda };
    }

    private async Task<List<GetParentStudentAgendaDto>> GetStudentClasses(int institutionId, int studentId)
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
            .SqlQueryRaw<GetParentStudentAgendaDto>(sql, institutionId, ClassStatus.Started.ToInt(), studentId, StudentClassStatus.Matriculado.ToInt())
            .AsNoTracking().ToListAsync();
    }
}
