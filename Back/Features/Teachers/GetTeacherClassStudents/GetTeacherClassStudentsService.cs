using Estud.Back.Domain.Classes;

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

        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var classStudents = await GetClassStudents(classId);
        var classStudentsWorks = await GetClassStudentsWorks(classId);

        var students = classStudents
            .Select(s =>
            {
                var works = classStudentsWorks.GetValueOrDefault(s.Id, []);
                var attendances = s.Presences + s.Absences;
                return new GetTeacherClassStudentsItemOut
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status,
                    AverageGrade = Math.Round(config.GradeRule.Average(works), 1, MidpointRounding.AwayFromZero),
                    AverageAttendance = attendances > 0
                        ? Math.Round((decimal)s.Presences / attendances * 100, 1, MidpointRounding.AwayFromZero)
                        : 0,
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

    private async Task<Dictionary<int, List<(ClassNoteType NoteType, int Weight, decimal Note)>>> GetClassStudentsWorks(int classId)
    {
        const string sql = @"
            SELECT
                cs.student_id            AS id,
                ca.note                  AS note_type,
                ca.weight                AS weight,
                COALESCE(caw.note, 0)    AS note
            FROM
                estud.classes__students cs
            INNER JOIN
                estud.class_activities ca ON ca.class_id = cs.class_id
            LEFT JOIN
                estud.class_activity_works caw ON caw.class_activity_id = ca.id AND caw.student_id = cs.student_id
            WHERE
                cs.class_id = {0}
        ";

        var works = await ctx.Database
            .SqlQueryRaw<GetTeacherClassStudentWorkDto>(sql, classId)
            .AsNoTracking().ToListAsync();

        return works
            .GroupBy(w => w.Id)
            .ToDictionary(g => g.Key, g => g.Select(w => (w.NoteType, w.Weight, w.Note)).ToList());
    }
}
