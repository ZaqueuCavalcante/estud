namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetClass(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_GetClass_Should_get_class_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(@class.Id);
        details.Discipline.Should().Be("Geometria");
        details.Period.Should().Be(period.Name);
        details.Status.Should().Be(ClassStatus.OnPreEnrollment);
        details.Schedules.Should().BeEmpty();
        details.Students.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_GetClass_Should_get_class_with_enrolled_students()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2));
        await client.ReleaseClassForEnrollment(@class.Id);

        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Status.Should().Be(ClassStatus.OnEnrollment);
        details.Students.Should().ContainSingle();
        details.Students[0].Id.Should().Be(student.Id);
        details.Students[0].Status.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_class_as_awaiting_start_when_enrollment_period_is_finalized()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var enrollmentPeriod = await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2)).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        await client.UpdateEnrollmentPeriod(enrollmentPeriod.Id, startAt: today.AddDays(-2), endAt: today.AddDays(-1));

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        result.Success.Status.Should().Be(ClassStatus.OnReview);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_students_and_class_attendances()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id]);
        await teacher.CreateLessonAttendance(lessons[2].Id, []);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.First(s => s.Id == ana.Id).AverageAttendance.Should().Be(66.7M);
        details.Students.First(s => s.Id == bruno.Id).AverageAttendance.Should().Be(33.3M);
        details.AverageAttendance.Should().Be(50M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_full_attendances_when_every_student_was_present()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id, bruno.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageAttendance.Should().Be(100M));
        details.AverageAttendance.Should().Be(100M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageAttendance.Should().Be(0M));
        details.AverageAttendance.Should().Be(0M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_partial_average_grade_when_only_one_activity_was_graded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, student.Id, 9M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2.3M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_a_different_average_grade_for_each_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, ana.Id, 8M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, bruno.Id, 5M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.First(s => s.Id == ana.Id).AverageGrade.Should().Be(4.0M);
        details.Students.First(s => s.Id == bruno.Id).AverageGrade.Should().Be(2.5M);
        details.AverageGrade.Should().Be(3.3M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_average_grade_from_the_two_highest_notes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        var n3 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N3, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, student.Id, 9M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, student.Id, 4M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n3.Id, student.Id, 7M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(8M);
    }

    [Test]
    public async Task Classes_GetClass_Should_add_up_the_weights_of_the_activities_of_the_same_note_type()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var first = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var second = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 60).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, first.Id, student.Id, 10M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, second.Id, student.Id, 5M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(3.5M);
    }

    [Test]
    public async Task Classes_GetClass_Should_count_an_uncorrected_activity_as_zero()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, student.Id, 8M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_zeroed_average_grades_when_the_class_has_no_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageGrade.Should().Be(0M));
        details.AverageGrade.Should().Be(0M);
    }

    [Test]
    [TestCase(25, 1.0)]
    [TestCase(100, 4.0)]
    public async Task Classes_GetClass_Should_recalculate_the_average_grade_when_the_weight_of_a_graded_activity_changes(int newWeight, decimal newAverage)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, student.Id, 8M);

        var before = await director.GetClass(@class.Id).Success();

        // Act
        await teacher.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N1, weight: newWeight);

        // Assert
        before.Students.Single().AverageGrade.Should().Be(2M);
        before.AverageGrade.Should().Be(2M);

        var after = await director.GetClass(@class.Id).Success();
        after.Students.Single().AverageGrade.Should().Be(newAverage);
        after.AverageGrade.Should().Be(newAverage);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_teacher_and_student_profile_photos()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();

        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var teacherUpload = await teacherClient.ShortcutUploadProfilePhoto(storage);
        await teacherClient.UpdateProfilePhoto(teacherUpload.Path).Success();

        var studentClient = await _back.LoginAs(student.Email);
        var studentUpload = await studentClient.ShortcutUploadProfilePhoto(storage);
        await studentClient.UpdateProfilePhoto(studentUpload.Path).Success();

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Teachers.Should().ContainSingle();
        details.Teachers[0].Photo.Should().NotBeNullOrEmpty();
        details.Teachers[0].Photo.Should().Contain(teacherUpload.Path);

        details.Students.Should().ContainSingle();
        details.Students[0].Photo.Should().NotBeNullOrEmpty();
        details.Students[0].Photo.Should().Contain(studentUpload.Path);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_class_campus_and_schedules_with_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Agreste I").Success();
        var classroom = await client.CreateClassroom(campus.Id, name: "Sala 05").Success();

        var discipline = await client.CreateDiscipline().Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Wednesday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id),
        ]);

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.CampusId.Should().Be(campus.Id);
        details.Campus.Should().Be("Agreste I");

        details.Schedules.Should().HaveCount(2);
        details.Schedules[0].Day.Should().Be(Day.Monday);
        details.Schedules[0].TeacherId.Should().Be(teacher.Id);
        details.Schedules[0].Teacher.Should().Be("Ana Lima");
        details.Schedules[0].ClassroomId.Should().Be(classroom.Id);
        details.Schedules[0].Classroom.Should().Be("Sala 05");

        details.Schedules[1].Day.Should().Be(Day.Wednesday);
        details.Schedules[1].TeacherId.Should().BeNull();
        details.Schedules[1].Teacher.Should().BeNull();
        details.Schedules[1].ClassroomId.Should().BeNull();
        details.Schedules[1].Classroom.Should().BeNull();
    }

    #endregion
}
