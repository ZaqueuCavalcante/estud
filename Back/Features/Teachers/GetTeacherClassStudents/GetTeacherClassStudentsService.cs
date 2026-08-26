namespace Estud.Back.Features.Teachers.GetTeacherClassStudents;

public class GetTeacherClassStudentsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetTeacherClassStudentsOut, EstudError>> Get(int classId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var classStudents = await GetClassStudents(classId);

        // Mock: nota média aleatória, porém estável por aluno (seed = Id).
        // TODO: calcular a partir das notas reais do aluno na turma.
        var students = classStudents
            .Select(s =>
            {
                var random = new Random(s.Id);
                var attendances = s.Presences + s.Absences;
                return new GetTeacherClassStudentsItemOut
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status,
                    AverageGrade = Math.Round((decimal)(random.NextDouble() * 10), 1),
                    AverageAttendance = attendances > 0 ? Math.Round((decimal)s.Presences / attendances * 100, 1) : 0,
                };
            })
            .ToList();

        return new GetTeacherClassStudentsOut { Students = students };
    }

    private async Task<List<GetTeacherClassStudentDto>> GetClassStudents(int classId)
    {
        const string sql = @"
            SELECT
                s.id      AS id,
                s.name    AS name,
                cs.status AS status,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.classes__students cs
            INNER JOIN
                estud.students s ON s.id = cs.student_id
            LEFT JOIN
                estud.class_lesson_attendances cla ON cla.class_id = cs.class_id AND cla.student_id = s.id
            WHERE
                cs.class_id = {0}
            GROUP BY
                s.id, cs.status
            ORDER BY
                s.name
        ";

        return await ctx.Database
            .SqlQueryRaw<GetTeacherClassStudentDto>(sql, classId)
            .AsNoTracking().ToListAsync();
    }
}
