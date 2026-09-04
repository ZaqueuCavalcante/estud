using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Students.GetStudentDetails;

public class GetStudentDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentDetailsOut, EstudError>> Get(int studentId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;
        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var student = await ctx.Students.AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.InstitutionId == institutionId);
        if (student == null) return StudentNotFound.I;

        var course = await GetCourse(studentId);
        var classes = await GetClasses(studentId, institutionId);
        var attendances = (await GetAttendances(studentId)).ToDictionary(a => a.ClassId);
        var works = await GetWorks(studentId);

        foreach (var @class in classes)
        {
            @class.AverageGrade = Round(config.GradeRule.Average(works.GetValueOrDefault(@class.Id, [])));
            @class.AverageAttendance = attendances.TryGetValue(@class.Id, out var attendance)
                ? Percent(attendance.Presences, attendance.Presences + attendance.Absences)
                : 0;
        }

        var startedClasses = classes.Where(c => c.Status == ClassStatus.Started).ToList();
        var averageGrade = startedClasses.Count > 0 ? Round(startedClasses.Average(c => c.AverageGrade)) : 0;

        var startedClassIds = startedClasses.Select(c => c.Id).ToHashSet();
        var started = attendances.Values.Where(a => startedClassIds.Contains(a.ClassId)).ToList();
        var averageAttendance = Percent(
            started.Sum(a => a.Presences),
            started.Sum(a => a.Presences + a.Absences)
        );

        return new GetStudentDetailsOut
        {
            Id = student.Id,
            Name = student.Name,
            Email = student.User!.Email!,
            Birthdate = student.User!.Birthdate!,
            PhoneNumber = student.User!.PhoneNumber!,
            EnrollmentCode = student.EnrollmentCode,
            Status = student.Status,
            YieldCoefficient = student.YieldCoefficient,
            AverageGrade = averageGrade,
            AverageAttendance = averageAttendance,
            Course = course,
            Classes = classes,
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static decimal Percent(int presences, int attendances) =>
        attendances > 0 ? Round((decimal)presences / attendances * 100) : 0;

    private async Task<Dictionary<int, List<(ClassNoteType NoteType, int Weight, decimal Note)>>> GetWorks(int studentId)
    {
        const string sql = @"
            SELECT
                cs.class_id              AS class_id,
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
                cs.student_id = {0}
        ";

        var works = await ctx.Database
            .SqlQueryRaw<GetStudentClassWorkDto>(sql, studentId)
            .AsNoTracking().ToListAsync();

        return works
            .GroupBy(w => w.ClassId)
            .ToDictionary(g => g.Key, g => g.Select(w => (w.NoteType, w.Weight, w.Note)).ToList());
    }

    private async Task<List<GetStudentClassAttendanceDto>> GetAttendances(int studentId)
    {
        const string sql = @"
            SELECT
                cs.class_id AS class_id,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.classes__students cs
            LEFT JOIN
                estud.class_lesson_attendances cla ON cla.class_id = cs.class_id AND cla.student_id = cs.student_id
            WHERE
                cs.student_id = {0}
            GROUP BY
                cs.class_id
        ";

        return await ctx.Database
            .SqlQueryRaw<GetStudentClassAttendanceDto>(sql, studentId)
            .AsNoTracking().ToListAsync();
    }

    private async Task<GetStudentDetailsCourseOut?> GetCourse(int studentId)
    {
        return await ctx.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.LeftAt == null)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new GetStudentDetailsCourseOut
            {
                CourseOfferingId = e.CourseOfferingId,
                Course = e.CourseOffering!.Course!.Name,
                Campus = e.CourseOffering.Campus!.Name,
                Period = e.CourseOffering.AcademicPeriod!.Name,
                Session = e.CourseOffering.Session,
                EnrolledAt = e.EnrolledAt,
            })
            .FirstOrDefaultAsync();
    }

    private async Task<List<GetStudentDetailsClassOut>> GetClasses(int studentId, int institutionId)
    {
        var classes = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.StudentId == studentId && cs.Class!.InstitutionId == institutionId)
            .OrderByDescending(cs => cs.Class!.Period.Name)
            .ThenBy(cs => cs.Class!.Discipline.Name)
            .Select(cs => new GetStudentDetailsClassOut
            {
                Id = cs.ClassId,
                Discipline = cs.Class!.Discipline.Name,
                Period = cs.Class.Period.Name,
                Workload = cs.Class.Workload,
                Status = cs.Class.Status,
                MyStatus = cs.Status,
            })
            .ToListAsync();

        // Mesma regra do GetClass: fora de um período de matrícula vigente, uma
        // turma liberada para matrícula é exibida como aguardando início.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (classes.Any(c => c.Status == ClassStatus.OnEnrollment))
        {
            var hasCurrentEnrollmentPeriod = await ctx.EnrollmentPeriods.AsNoTracking()
                .AnyAsync(p => p.InstitutionId == institutionId && p.StartAt <= today && today <= p.EndAt);

            if (!hasCurrentEnrollmentPeriod)
            {
                foreach (var @class in classes.Where(c => c.Status == ClassStatus.OnEnrollment))
                    @class.Status = ClassStatus.OnReview;
            }
        }

        return classes;
    }
}
