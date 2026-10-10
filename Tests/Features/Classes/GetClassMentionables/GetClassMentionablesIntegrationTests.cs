namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetClassMentionables(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_when_user_is_a_manager()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        // Act
        var result = await director.GetClassMentionables(@class.Id);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await teacher.GetClassMentionables(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_of_class_of_another_institution_as_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var otherDirector = await _back.LoggedAsDirector();
        var otherClass = await otherDirector.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await teacher.GetClassMentionables(otherClass.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_of_class_of_another_institution_as_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var otherDirector = await _back.LoggedAsDirector();
        var otherClass = await otherDirector.ShortcutCreateStartedClass();

        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await student.GetClassMentionables(otherClass.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_when_teacher_is_not_assigned_to_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var teacher = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await teacher.GetClassMentionables(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_mentionables_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var otherStudent = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var student = await _back.LoginAs(otherStudent.Email);

        // Act
        var result = await student.GetClassMentionables(@class.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_GetClassMentionables_Should_get_students_of_the_class_as_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var names = new List<string> { DataGen.UserName, DataGen.UserName, DataGen.UserName };
        var studentIds = new List<int>();
        foreach (var name in names)
            studentIds.Add((await director.CreateStudent(name, DataGen.Email).Success()).Id);

        var @class = await director.ShortcutCreateStartedClass(students: studentIds);

        var teacher = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await teacher.GetClassMentionables(@class.Id);

        // Assert
        var items = result.Success.Items;
        items.Select(i => i.Name).Should().BeEquivalentTo(names);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_get_students_of_the_class_as_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 3);

        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await student.GetClassMentionables(@class.Id);

        // Assert
        var items = result.Success.Items;
        items.Should().HaveCount(3);
        items.Should().ContainSingle(i => i.Id == student.User.Id && i.Name == @class.StudentName);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_return_user_id_of_the_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await teacher.GetClassMentionables(@class.Id);

        // Assert
        var item = result.Success.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(student.User.Id);
        item.Name.Should().Be(@class.StudentName);
    }

    [Test]
    public async Task Classes_GetClassMentionables_Should_not_get_students_of_another_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var class1 = await director.ShortcutCreateStartedClass();
        var class2 = await director.ShortcutCreateStartedClass(studentsCount: 2);

        var teacher = await _back.LoginAs(class1.TeacherEmail);

        // Act
        var result = await teacher.GetClassMentionables(class1.Id);

        // Assert
        var item = result.Success.Items.Should().ContainSingle().Subject;
        item.Name.Should().Be(class1.StudentName);
        result.Success.Items.Select(i => i.Name).Should().NotContain(class2.StudentName);
    }

    #endregion
}
