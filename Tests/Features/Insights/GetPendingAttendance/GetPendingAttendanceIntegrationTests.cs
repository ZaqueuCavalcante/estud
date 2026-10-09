using Estud.Back.Domain.Classes;
using Estud.Back.Features.Teachers.GetTeacherClassLessons;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_pending_attendance_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetPendingAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_pending_attendance_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetPendingAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_pending_attendance_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetPendingAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_pending_attendance_when_period_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetPendingAttendance(1234);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_pending_attendance_when_period_is_from_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var otherPeriod = await otherDirector.ShortcutGetFirstAcademicPeriod();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetPendingAttendance(otherPeriod.Id);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Insights_GetPendingAttendance_Should_get_everything_up_to_date_when_period_has_no_lessons()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        var pending = result.Success;
        pending.PastLessons.Should().Be(0);
        pending.PendingLessons.Should().Be(0);
        pending.UpToDate.Should().Be(100M);
        pending.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_get_pending_lessons_of_the_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(disciplineName: "Cálculo I", periodId: period.Id);

        await RecordFirstLessonAttendance(@class, @class.StudentIds);

        var pastLessons = await GetPastLessons(@class);

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        var pending = result.Success;
        pending.PastLessons.Should().Be(pastLessons.Count);
        pending.PendingLessons.Should().Be(pastLessons.Count - 1);
        pending.UpToDate.Should().Be(AttendanceRate.Of(1, pastLessons.Count));

        var item = pending.Classes.Should().ContainSingle().Subject;
        item.Id.Should().Be(@class.Id);
        item.Discipline.Should().Be("Cálculo I");
        item.Teachers.Should().Equal(@class.TeacherName);
        item.PendingLessons.Should().Be(pastLessons.Count - 1);
        item.OldestPendingAt.Should().Be(pastLessons.Skip(1).Min(l => l.Date));
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_count_future_lessons()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetLastAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(periodId: period.Id);

        var pastLessons = await GetPastLessons(@class);

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        result.Success.PastLessons.Should().Be(pastLessons.Count);
        result.Success.PendingLessons.Should().Be(pastLessons.Count);
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_get_period_classes_ordered_by_most_pending_lessons()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var otherPeriod = await director.ShortcutGetLastAcademicPeriod();

        var geometry = await director.ShortcutCreateStartedClass(disciplineName: "Geometria", periodId: period.Id);
        var algebra = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra", periodId: period.Id);
        var physics = await director.ShortcutCreateStartedClass(disciplineName: "Física", periodId: period.Id);
        var history = await director.ShortcutCreateStartedClass(disciplineName: "História", periodId: otherPeriod.Id);

        await RecordLessonsAttendance(geometry, 2);
        await RecordLessonsAttendance(physics, 1);
        await RecordLessonsAttendance(history, 1);

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        var classes = result.Success.Classes;
        classes.Select(c => c.Id).Should().Equal(algebra.Id, physics.Id, geometry.Id);
        classes[0].PendingLessons.Should().Be(classes[1].PendingLessons + 1);
        classes[1].PendingLessons.Should().Be(classes[2].PendingLessons + 1);
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_not_get_classes_with_all_attendances_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(periodId: period.Id);

        var pastLessons = await GetPastLessons(@class);
        await RecordLessonsAttendance(@class, pastLessons.Count);

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        var pending = result.Success;
        pending.PastLessons.Should().Be(pastLessons.Count);
        pending.PendingLessons.Should().Be(0);
        pending.UpToDate.Should().Be(100M);
        pending.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Insights_GetPendingAttendance_Should_get_at_most_five_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        for (var i = 0; i < 6; i++)
        {
            await director.ShortcutCreateStartedClass(disciplineName: $"Disciplina {i}", periodId: period.Id);
        }

        // Act
        var result = await director.GetPendingAttendance(period.Id);

        // Assert
        result.Success.Classes.Should().HaveCount(5);
    }

    #endregion

    private async Task<List<GetTeacherClassLessonsItemOut>> GetPastLessons(ShortcutCreateClassDto @class)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        return lessons.Where(l => l.Date < today).ToList();
    }

    private async Task RecordLessonsAttendance(ShortcutCreateClassDto @class, int count)
    {
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await GetPastLessons(@class);
        foreach (var lesson in lessons.Take(count))
        {
            await teacher.CreateLessonAttendance(lesson.Id, @class.StudentIds);
        }
    }
}
