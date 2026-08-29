namespace Estud.Back.Features.Students.GetStudentDetails;

public class GetStudentDetailsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentDetailsOut, EstudError>> Get(int studentId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var student = await ctx.Students.AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.InstitutionId == institutionId);
        if (student == null) return StudentNotFound.I;

        var course = await GetCourse(studentId);
        var classes = await GetClasses(studentId, institutionId);
        var attendances = (await GetAttendances(studentId)).ToDictionary(a => a.ClassId);

        // Mock: nota média aleatória, porém estável por aluno (seed = Id),
        // igual à exibida no detalhe da turma.
        // TODO: calcular a partir das notas reais do aluno.
        var random = new Random(student.Id);
        var averageGrade = Math.Round((decimal)(random.NextDouble() * 10), 1);

        foreach (var @class in classes)
        {
            @class.AverageGrade = averageGrade;
            @class.AverageAttendance = attendances.TryGetValue(@class.Id, out var attendance)
                ? Percent(attendance.Presences, attendance.Presences + attendance.Absences)
                : 0;
        }

        var startedClassIds = classes
            .Where(c => c.Status == ClassStatus.Started)
            .Select(c => c.Id)
            .ToHashSet();
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

    private static decimal Percent(int presences, int attendances) =>
        attendances > 0 ? Math.Round((decimal)presences / attendances * 100, 1) : 0;

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
