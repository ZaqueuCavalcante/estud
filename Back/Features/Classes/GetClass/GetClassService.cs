namespace Estud.Back.Features.Classes.GetClass;

public class GetClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetClassOut, EstudError>> Get(int classId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes.AsNoTracking()
            .Include(c => c.Discipline)
            .Include(c => c.Teachers)
            .Include(c => c.Period)
            .Include(c => c.Campus)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (@class.Status == ClassStatus.OnEnrollment)
        {
            var hasCurrentEnrollmentPeriod = await ctx.EnrollmentPeriods.AsNoTracking()
                .AnyAsync(p => p.InstitutionId == institutionId && p.StartAt <= today && today <= p.EndAt);
            if (!hasCurrentEnrollmentPeriod) @class.Status = ClassStatus.OnReview;
        }

        var classroomIds = @class.Schedules
            .Where(s => s.ClassroomId != null)
            .Select(s => s.ClassroomId!.Value)
            .Distinct()
            .ToList();
        var classroomNames = await ctx.Classrooms.AsNoTracking()
            .Where(c => classroomIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var classStudents = await GetClassStudents(classId);

        // Mock: nota média aleatória, porém estáveis por aluno (seed = Id).
        // TODO: calcular a partir das notas reais do aluno na turma.
        var students = classStudents
            .Select(s =>
            {
                var random = new Random(s.Id);
                var attendances = s.Presences + s.Absences;
                return new GetClassStudentOut
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status,
                    AverageGrade = Math.Round((decimal)(random.NextDouble() * 10), 1),
                    AverageAttendance = attendances > 0 ? Math.Round((decimal)s.Presences / attendances * 100, 1) : 0,
                };
            })
            .ToList();

        var totalAttendances = classStudents.Sum(s => s.Presences + s.Absences);
        var averageAttendance = totalAttendances > 0
            ? Math.Round((decimal)classStudents.Sum(s => s.Presences) / totalAttendances * 100, 1)
            : 0;

        return new GetClassOut
        {
            Id = @class.Id,
            DisciplineId = @class.DisciplineId,
            Discipline = @class.Discipline?.Name ?? "",
            Period = @class.Period?.Name ?? "",
            CampusId = @class.CampusId,
            Campus = @class.Campus?.Name,
            Vacancies = @class.Vacancies,
            Workload = @class.Workload,
            Status = @class.Status,
            AverageAttendance = averageAttendance,
            Teachers = @class.Teachers
                .OrderBy(t => t.Name)
                .Select(t => new GetClassTeacherOut { Id = t.Id, Name = t.Name })
                .ToList(),
            Schedules = @class.Schedules
                .OrderBy(s => s.Day).ThenBy(s => s.Start)
                .Select(s => new GetClassScheduleOut(s.Day, s.Start, s.End)
                {
                    TeacherId = s.TeacherId,
                    Teacher = s.TeacherId == null ? null : @class.Teachers.FirstOrDefault(t => t.Id == s.TeacherId)?.Name,
                    ClassroomId = s.ClassroomId,
                    Classroom = s.ClassroomId != null && classroomNames.TryGetValue(s.ClassroomId.Value, out var name) ? name : null,
                })
                .ToList(),
            Students = students,
        };
    }

    private async Task<List<GetClassStudentDto>> GetClassStudents(int classId)
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
            .SqlQueryRaw<GetClassStudentDto>(sql, classId)
            .AsNoTracking().ToListAsync();
    }
}
