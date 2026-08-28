namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.StartClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.StartClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.StartClass(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_class_is_not_on_enrollment()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.StartClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassMustBeOnEnrollment.I);
    }

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_class_has_no_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        // Act
        var result = await client.StartClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassWithoutTeachers.I);
    }

    [Test]
    public async Task Classes_StartClass_Should_not_start_class_when_class_has_no_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.ReleaseClassForEnrollment(@class.Id);

        // Act
        var result = await client.StartClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassWithoutSchedules.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_StartClass_Should_start_class_and_generate_lessons()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);

        await client.ReleaseClassForEnrollment(@class.Id);

        // Act
        var result = await client.StartClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        await using var ctx = _back.GetDbContext();
        var started = await ctx.Classes.FirstAsync(c => c.Id == @class.Id);
        started.Status.Should().Be(ClassStatus.Started);
        started.Workload.Should().BeGreaterThan(0);

        var lessons = await ctx.ClassLessons.Where(l => l.ClassId == @class.Id).ToListAsync();
        lessons.Should().NotBeEmpty();
        lessons.Should().OnlyContain(l => l.Date.DayOfWeek == DayOfWeek.Monday);
        lessons.Should().OnlyContain(l => l.Status == ClassLessonStatus.Pending);
    }

    [Test]
    public async Task Classes_StartClass_Should_not_generate_lessons_on_non_school_days()
    {
        // Arrange — período 2024.1 (01/02 a 01/07), turma com horário na segunda.
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);

        await client.ReleaseClassForEnrollment(@class.Id);

        // Recesso numa segunda dentro do período: 11/03/2024.
        var recess = new DateTime(2024, 3, 11);
        await client.CreateCalendarDay(recess, DayType.Recess, "Recesso").Success();

        // Act
        await client.StartClass(@class.Id).Success();

        // Assert
        await using var ctx = _back.GetDbContext();
        var lessons = await ctx.ClassLessons.Where(l => l.ClassId == @class.Id).ToListAsync();

        lessons.Should().NotBeEmpty();
        lessons.Should().NotContain(l => l.Date == DateOnly.FromDateTime(recess));

        // 01/04/2024 (Páscoa foi 31/03) é uma segunda comum: continua virando aula.
        lessons.Should().Contain(l => l.Date == new DateOnly(2024, 4, 1));
    }

    [Test]
    public async Task Classes_StartClass_Should_only_skip_the_non_school_days_of_the_campus_of_the_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var first = await client.CreateCampus("Agreste I").Success();
        var second = await client.CreateCampus("Agreste II").Success();

        // Feriado local só do primeiro campus, numa segunda dentro do período.
        var holiday = new DateTime(2024, 3, 18);
        await client.CreateCalendarDay(holiday, DayType.Holiday, "Feriado local", campusId: first.Id).Success();

        var firstClass = await client.CreateClass(discipline.Id, period.Id, campusId: first.Id).Success();
        await client.UpdateClassTeachers(firstClass.Id, [teacher.Id]);
        await client.UpdateClassSchedules(firstClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);
        await client.ReleaseClassForEnrollment(firstClass.Id);

        var secondClass = await client.CreateClass(discipline.Id, period.Id, campusId: second.Id).Success();
        await client.UpdateClassTeachers(secondClass.Id, [teacher.Id]);
        await client.UpdateClassSchedules(secondClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);
        await client.ReleaseClassForEnrollment(secondClass.Id);

        // Act
        await client.StartClass(firstClass.Id).Success();
        await client.StartClass(secondClass.Id).Success();

        // Assert
        await using var ctx = _back.GetDbContext();
        var date = DateOnly.FromDateTime(holiday);

        var firstLessons = await ctx.ClassLessons.Where(l => l.ClassId == firstClass.Id).ToListAsync();
        firstLessons.Should().NotContain(l => l.Date == date);

        var secondLessons = await ctx.ClassLessons.Where(l => l.ClassId == secondClass.Id).ToListAsync();
        secondLessons.Should().Contain(l => l.Date == date);
    }

    #endregion
}
