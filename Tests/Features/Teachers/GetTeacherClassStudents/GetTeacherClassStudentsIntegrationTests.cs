namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_not_get_students_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherClassStudents(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_not_get_students_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherClassStudents(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_not_get_students_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassStudents(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_not_get_students_of_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassStudents(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_not_get_students_of_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(otherTeacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClassStudents(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_empty_list_when_class_has_no_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_class_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent("Zaqueu do Vale", DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClassStudents(@class.Id);

        // Assert
        var students = result.Success.Students;
        students.Should().ContainSingle();

        var item = students[0];
        item.Id.Should().Be(student.Id);
        item.Name.Should().Be("Zaqueu do Vale");
        item.Status.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_class_students_ordered_by_name()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var carlos = await director.CreateStudent("Carlos Andrade", DataGen.Email).Success();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        await director.AssignStudentToClass(carlos.Id, @class.Id);
        await director.AssignStudentToClass(ana.Id, @class.Id);
        await director.AssignStudentToClass(bruno.Id, @class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClassStudents(@class.Id);

        // Assert
        var students = result.Success.Students;
        students.Should().HaveCount(3);
        students.Select(x => x.Name).Should().ContainInOrder("Ana Beatriz", "Bruno Silva", "Carlos Andrade");
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_students_attendances()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.GetFirstAcademicPeriod();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var (classId, teacher) = await _back.ArrangeStartedClass(director, period.Id, [ana.Id, bruno.Id]);

        var lessons = await teacher.GetClassLessons(classId);
        await teacher.CreateLessonAttendance(lessons[0], [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1], [ana.Id]);
        await teacher.CreateLessonAttendance(lessons[2], []);

        // Act
        var result = await teacher.GetTeacherClassStudents(classId);

        // Assert
        var students = result.Success.Students;
        students.First(s => s.Id == ana.Id).AverageAttendance.Should().Be(66.7M);
        students.First(s => s.Id == bruno.Id).AverageAttendance.Should().Be(33.3M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_full_attendance_when_student_was_always_present()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.GetFirstAcademicPeriod();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var (classId, teacher) = await _back.ArrangeStartedClass(director, period.Id, [student.Id]);

        var lessons = await teacher.GetClassLessons(classId);
        await teacher.CreateLessonAttendance(lessons[0], [student.Id]);
        await teacher.CreateLessonAttendance(lessons[1], [student.Id]);

        // Act
        var result = await teacher.GetTeacherClassStudents(classId);

        // Assert
        result.Success.Students.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(100M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.GetFirstAcademicPeriod();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var (classId, teacher) = await _back.ArrangeStartedClass(director, period.Id, [student.Id]);

        // Act
        var result = await teacher.GetTeacherClassStudents(classId);

        // Assert
        result.Success.Students.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(0M);
    }

    #endregion
}
