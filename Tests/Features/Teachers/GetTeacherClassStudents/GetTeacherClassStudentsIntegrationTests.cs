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
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = await teacher.GetClassLessons(@class.Id);
        await teacher.CreateLessonAttendance(lessons[0], [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1], [ana.Id]);
        await teacher.CreateLessonAttendance(lessons[2], []);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

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
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = await teacher.GetClassLessons(@class.Id);
        await teacher.CreateLessonAttendance(lessons[0], [student.Id]);
        await teacher.CreateLessonAttendance(lessons[1], [student.Id]);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(100M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(0M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_zeroed_average_grades_when_the_class_has_no_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Should().AllSatisfy(s => s.AverageGrade.Should().Be(0M));
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_partial_average_grade_when_only_one_activity_was_graded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, student.Id, 9M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2.3M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_count_an_uncorrected_activity_as_zero()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100);
        await teacher.AddStudentActivityNote(@class.Id, n1.Id, student.Id, 8M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_add_up_the_weights_of_the_activities_of_the_same_note_type()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var first = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var second = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 60).Success();
        await teacher.AddStudentActivityNote(@class.Id, first.Id, student.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, second.Id, student.Id, 5M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(3.5M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_average_grade_from_the_two_highest_notes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        var n3 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N3, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, n1.Id, student.Id, 9M);
        await teacher.AddStudentActivityNote(@class.Id, n2.Id, student.Id, 4M);
        await teacher.AddStudentActivityNote(@class.Id, n3.Id, student.Id, 7M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(8M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_a_different_average_grade_for_each_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, ana.Id, 8M);
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, bruno.Id, 5M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        var students = result.Success.Students;
        students.First(s => s.Id == ana.Id).AverageGrade.Should().Be(4.0M);
        students.First(s => s.Id == bruno.Id).AverageGrade.Should().Be(2.5M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_average_grade_from_the_grade_rule_of_the_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOrThird);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        var n3 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N3, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, n1.Id, student.Id, 4M);
        await teacher.AddStudentActivityNote(@class.Id, n2.Id, student.Id, 6M);
        await teacher.AddStudentActivityNote(@class.Id, n3.Id, student.Id, 8M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(8M);
    }

    [Test]
    public async Task Teachers_GetTeacherClassStudents_Should_get_grades_and_attendances_of_a_full_semester()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfThree);

        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = await teacher.GetClassLessons(@class.Id);
        await teacher.CreateLessonAttendance(lessons[0], [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1], [ana.Id]);

        var firstN1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 60).Success();
        var secondN1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        var n3 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N3, weight: 100).Success();

        await teacher.AddStudentActivityNote(@class.Id, firstN1.Id, ana.Id, 9M);
        await teacher.AddStudentActivityNote(@class.Id, secondN1.Id, ana.Id, 7M);
        await teacher.AddStudentActivityNote(@class.Id, n2.Id, ana.Id, 6M);

        await teacher.AddStudentActivityNote(@class.Id, firstN1.Id, bruno.Id, 5M);
        await teacher.AddStudentActivityNote(@class.Id, secondN1.Id, bruno.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, n2.Id, bruno.Id, 8M);
        await teacher.AddStudentActivityNote(@class.Id, n3.Id, bruno.Id, 9M);

        // Act
        var result = await teacher.GetTeacherClassStudents(@class.Id);

        // Assert
        var students = result.Success.Students;
        students.Select(s => s.Name).Should().ContainInOrder("Ana Beatriz", "Bruno Silva");

        var anaStudent = students.First(s => s.Id == ana.Id);
        anaStudent.AverageGrade.Should().Be(4.7M);
        anaStudent.AverageAttendance.Should().Be(100M);

        var brunoStudent = students.First(s => s.Id == bruno.Id);
        brunoStudent.AverageGrade.Should().Be(8M);
        brunoStudent.AverageAttendance.Should().Be(50M);
    }

    #endregion
}
