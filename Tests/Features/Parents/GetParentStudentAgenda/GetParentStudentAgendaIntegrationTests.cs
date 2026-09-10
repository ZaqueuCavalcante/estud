namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetParentStudentAgenda(studentId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_user_is_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetParentStudentAgenda(studentId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetParentStudentAgenda(studentId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_of_a_student_that_does_not_exist()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id + 999_999);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_of_a_student_not_linked_to_the_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var otherStudent = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(otherStudent.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_the_link_is_revoked()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();
        await director.RevokeParentStudentLink(parent.Id, studentId).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(studentId);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_not_get_agenda_when_the_link_was_revoked_by_the_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();
        await director.ShortcutCreateStartedClass(students: [student.Id]);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var studentClient = await _back.LoginAs(student.Email);
        await studentClient.RevokeParentLink(parent.Id).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_the_agenda_of_the_other_student_when_one_of_the_links_is_revoked()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var revokedClass = await director.ShortcutCreateStartedClass(disciplineName: "Geometria", day: Day.Monday);
        var activeClass = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra", day: Day.Wednesday);

        var revokedStudentId = revokedClass.StudentIds[0];
        var activeStudentId = activeClass.StudentIds[0];

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = revokedStudentId, Relationship = ParentRelationship.Mother },
            new() { StudentId = activeStudentId, Relationship = ParentRelationship.Mother },
        ]).Success();
        await director.RevokeParentStudentLink(parent.Id, revokedStudentId).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var revokedResult = await client.GetParentStudentAgenda(revokedStudentId);
        var activeResult = await client.GetParentStudentAgenda(activeStudentId);

        // Assert
        revokedResult.ShouldBeError(StudentNotFound.I);

        activeResult.ShouldBeSuccess();
        var days = activeResult.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Wednesday);
        days[0].Disciplines[0].ClassId.Should().Be(activeClass.Id);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_empty_agenda_when_student_has_no_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_agenda_of_a_class_with_a_single_schedule()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(studentId);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(@class.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
        days[0].Disciplines[0].Start.Should().Be(Hour.H07_00);
        days[0].Disciplines[0].End.Should().Be(Hour.H10_00);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_order_the_disciplines_of_a_day_by_start_hour()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [geometria.Id, algebra.Id]);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var morningClass = await director.CreateClass(geometria.Id, period.Id).Success();
        await director.UpdateClassTeachers(morningClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(morningClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(morningClass.Id);
        await director.AssignStudentToClass(student.Id, morningClass.Id);
        await director.StartClass(morningClass.Id);

        var nightClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(nightClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(nightClass.Id, [(Day.Monday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(nightClass.Id);
        await director.AssignStudentToClass(student.Id, nightClass.Id);
        await director.StartClass(nightClass.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.Start).Should().Equal(Hour.H07_00, Hour.H19_00);
        days[0].Disciplines.Select(d => d.End).Should().Equal(Hour.H09_00, Hour.H22_00);
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(morningClass.Id, nightClass.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
        days[0].Disciplines[1].Name.Should().Be("Álgebra");
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_order_the_days_by_week_day_when_the_classes_are_created_out_of_order()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var fridayClass = await director.ShortcutCreateStartedClass(students: [student.Id], disciplineName: "Geometria", day: Day.Friday);
        var mondayClass = await director.ShortcutCreateStartedClass(students: [student.Id], disciplineName: "Álgebra", day: Day.Monday);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(2);
        days.Select(d => d.Day).Should().Equal(Day.Monday, Day.Friday);
        days[0].Disciplines[0].ClassId.Should().Be(mondayClass.Id);
        days[1].Disciplines[0].ClassId.Should().Be(fridayClass.Id);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_both_schedules_when_the_class_has_two_schedules_on_the_same_day()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null),
            (Day.Monday, Hour.H10_00, Hour.H12_00, teacher.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);
        await director.StartClass(@class.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(@class.Id, @class.Id);
        days[0].Disciplines.Select(d => d.Start).Should().Equal(Hour.H07_00, Hour.H10_00);
        days[0].Disciplines.Select(d => d.End).Should().Equal(Hour.H09_00, Hour.H12_00);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_two_classes_of_the_same_discipline_on_the_same_day()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline("Geometria").Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var morningClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(morningClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(morningClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(morningClass.Id);
        await director.AssignStudentToClass(student.Id, morningClass.Id);
        await director.StartClass(morningClass.Id);

        var nightClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(nightClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(nightClass.Id, [(Day.Monday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(nightClass.Id);
        await director.AssignStudentToClass(student.Id, nightClass.Id);
        await director.StartClass(nightClass.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.Name).Should().Equal("Geometria", "Geometria");
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(morningClass.Id, nightClass.Id);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_show_the_classroom_name_when_the_schedule_has_a_classroom_and_null_when_online()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var campus = await director.CreateCampus().Success();
        var classroom = await director.CreateClassroom(campus.Id, "Sala 07").Success();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, teacher.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);
        await director.StartClass(@class.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(2);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines[0].ClassroomName.Should().Be("Sala 07");
        days[1].Day.Should().Be(Day.Wednesday);
        days[1].Disciplines[0].ClassroomName.Should().BeNull();
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_the_schedules_without_a_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, null, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);
        await director.StartClass(@class.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(2);
        days.Select(d => d.Day).Should().Equal(Day.Monday, Day.Wednesday);
        days[1].Disciplines.Should().HaveCount(1);
        days[1].Disciplines[0].ClassId.Should().Be(@class.Id);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_empty_agenda_when_the_class_is_on_enrollment()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_empty_agenda_when_the_class_is_finalized()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];

        await director.FinalizeClass(@class.Id).Success();

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(studentId);

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_only_the_started_class_when_the_student_is_also_in_a_not_started_one()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [geometria.Id, algebra.Id]);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var startedClass = await director.CreateClass(geometria.Id, period.Id).Success();
        await director.UpdateClassTeachers(startedClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(startedClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(startedClass.Id);
        await director.AssignStudentToClass(student.Id, startedClass.Id);
        await director.StartClass(startedClass.Id);

        var notStartedClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(notStartedClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(notStartedClass.Id, [(Day.Wednesday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(notStartedClass.Id);
        await director.AssignStudentToClass(student.Id, notStartedClass.Id);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(startedClass.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_empty_agenda_when_the_student_is_no_longer_enrolled_in_the_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];

        // TODO: use the endpoint that changes the student status in the class
        await using (var ctx = _back.GetDbContext())
        {
            var entity = await ctx.ClassStudents.FirstAsync(x => x.ClassId == @class.Id && x.StudentId == studentId);
            entity.Status = StudentClassStatus.ReprovadoPorFalta;
            await ctx.SaveChangesAsync();
        }

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
            [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(studentId);

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_a_separate_agenda_for_each_student_of_the_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var mondayClass = await director.ShortcutCreateStartedClass(disciplineName: "Geometria", day: Day.Monday);
        var wednesdayClass = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra", day: Day.Wednesday);

        var firstStudentId = mondayClass.StudentIds[0];
        var secondStudentId = wednesdayClass.StudentIds[0];

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = firstStudentId, Relationship = ParentRelationship.Mother },
            new() { StudentId = secondStudentId, Relationship = ParentRelationship.Mother },
        ]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var firstAgenda = await client.GetParentStudentAgenda(firstStudentId).Success();
        var secondAgenda = await client.GetParentStudentAgenda(secondStudentId).Success();

        // Assert
        firstAgenda.Days.Should().HaveCount(1);
        firstAgenda.Days[0].Day.Should().Be(Day.Monday);
        firstAgenda.Days[0].Disciplines.Should().HaveCount(1);
        firstAgenda.Days[0].Disciplines[0].ClassId.Should().Be(mondayClass.Id);
        firstAgenda.Days[0].Disciplines[0].Name.Should().Be("Geometria");

        secondAgenda.Days.Should().HaveCount(1);
        secondAgenda.Days[0].Day.Should().Be(Day.Wednesday);
        secondAgenda.Days[0].Disciplines.Should().HaveCount(1);
        secondAgenda.Days[0].Disciplines[0].ClassId.Should().Be(wednesdayClass.Id);
        secondAgenda.Days[0].Disciplines[0].Name.Should().Be("Álgebra");
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_the_same_agenda_for_two_students_of_the_parent_in_the_same_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var firstStudentId = @class.StudentIds[0];
        var secondStudentId = @class.StudentIds[1];

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = firstStudentId, Relationship = ParentRelationship.Mother },
            new() { StudentId = secondStudentId, Relationship = ParentRelationship.Mother },
        ]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var firstAgenda = await client.GetParentStudentAgenda(firstStudentId).Success();
        var secondAgenda = await client.GetParentStudentAgenda(secondStudentId).Success();

        // Assert
        firstAgenda.Days.Should().HaveCount(1);
        firstAgenda.Days[0].Day.Should().Be(Day.Monday);
        firstAgenda.Days[0].Disciplines[0].ClassId.Should().Be(@class.Id);

        secondAgenda.Days.Should().HaveCount(1);
        secondAgenda.Days[0].Day.Should().Be(Day.Monday);
        secondAgenda.Days[0].Disciplines[0].ClassId.Should().Be(@class.Id);
    }

    [Test]
    public async Task Parents_GetParentStudentAgenda_Should_get_only_the_agenda_of_the_requested_student_when_the_parent_has_two_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var sibling = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var @class = await director.ShortcutCreateStartedClass(students: [student.Id], disciplineName: "Geometria", day: Day.Monday);
        await director.ShortcutCreateStartedClass(students: [sibling.Id], disciplineName: "Álgebra", day: Day.Monday);

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = student.Id, Relationship = ParentRelationship.Mother },
            new() { StudentId = sibling.Id, Relationship = ParentRelationship.Mother },
        ]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudentAgenda(student.Id);

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(@class.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
    }

    #endregion
}
