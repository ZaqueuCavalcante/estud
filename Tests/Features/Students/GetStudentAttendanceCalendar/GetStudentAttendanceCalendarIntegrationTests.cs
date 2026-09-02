namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_not_get_calendar_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentAttendanceCalendar(year: 2026);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_not_get_calendar_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentAttendanceCalendar(year: 2026);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_not_get_calendar_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentAttendanceCalendar(year: 2026);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_get_all_days_of_the_year()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var calendar = await client.GetStudentAttendanceCalendar(2026).Success();

        // Assert
        calendar.Year.Should().Be(2026);
        calendar.Total.Should().Be(365);
        calendar.Items.Should().HaveCount(365);

        // Aluno sem turmas: todo dia é sem aula
        calendar.Items.Should().AllSatisfy(i => i.Status.Should().Be(StudentDayAttendanceStatus.NoClass));

        calendar.Items.First().Date.Should().Be(new DateTime(2026, 1, 1));
        calendar.Items.Last().Date.Should().Be(new DateTime(2026, 12, 31));
    }

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_use_current_year_when_year_is_not_informed()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var calendar = await client.GetStudentAttendanceCalendar().Success();

        // Assert
        calendar.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Test]
    public async Task Students_GetStudentAttendanceCalendar_Should_reflect_presence_absence_and_undefined_days()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Year;

        var periods = await director.GetAcademicPeriods().Success();
        var period = periods.Items.First(x => today >= x.StartAt && today <= x.EndAt);

        await director.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2));

        var @class = await director.ShortcutCreateStartedClass(periodId: period.Id);
        var studentId = @class.StudentIds[0];

        // Aulas do aluno (segundas-feiras do período), em ordem cronológica
        List<(int Id, DateOnly Date)> lessons;
        await using (var ctx = _back.GetDbContext())
        {
            var rows = await ctx.ClassLessons.AsNoTracking()
                .Where(l => l.ClassId == @class.Id)
                .OrderBy(l => l.Number)
                .Select(l => new { l.Id, l.Date })
                .ToListAsync();
            lessons = rows.Select(r => (r.Id, r.Date)).ToList();
        }

        var pastLessons = lessons.FindAll(l => l.Date < today);
        var presentLesson = pastLessons[0];
        var absentLesson = pastLessons[1];
        var futureLesson = lessons.First(l => l.Date > today);

        // Professor lança a frequência das duas primeiras aulas
        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        await teacherClient.CreateLessonAttendance(presentLesson.Id, [studentId]);   // presente
        await teacherClient.CreateLessonAttendance(absentLesson.Id, []);             // ausente

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var calendar = await client.GetStudentAttendanceCalendar(year).Success();

        // Assert
        StudentDayAttendanceStatus StatusOf(DateOnly d) =>
            calendar.Items.First(i => i.Date == d.ToDateTime(TimeOnly.MinValue)).Status;

        calendar.Total.Should().Be(DateTime.IsLeapYear(year) ? 366 : 365);

        StatusOf(presentLesson.Date).Should().Be(StudentDayAttendanceStatus.Present);
        StatusOf(absentLesson.Date).Should().Be(StudentDayAttendanceStatus.Absent);
        StatusOf(futureLesson.Date).Should().Be(StudentDayAttendanceStatus.Undefined);

        // Feriado (Confraternização Universal) → sem aula
        StatusOf(new DateOnly(year, 01, 01)).Should().Be(StudentDayAttendanceStatus.NoClass);

        // Domingo anterior a uma aula → sem aula
        StatusOf(presentLesson.Date.AddDays(-1)).Should().Be(StudentDayAttendanceStatus.NoClass);

        // Dia letivo (terça) em que o aluno não tem aula → sem aula
        StatusOf(presentLesson.Date.AddDays(1)).Should().Be(StudentDayAttendanceStatus.NoClass);
    }

    #endregion
}
