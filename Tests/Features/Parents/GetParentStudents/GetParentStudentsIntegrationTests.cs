namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_students_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_students_when_user_is_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_students_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Parents_GetParentStudents_Should_get_linked_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentName = DataGen.UserName;
        var studentId = (await director.CreateStudent(studentName, DataGen.Email).Success()).Id;

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeSuccess();
        var items = result.Success.Items;
        items.Should().HaveCount(1);
        items[0].Id.Should().Be(studentId);
        items[0].Name.Should().Be(studentName);
        items[0].EnrollmentCode.Should().NotBeEmpty();
        items[0].Relationship.Should().Be(ParentRelationship.Mother);
    }

    [Test]
    public async Task Parents_GetParentStudents_Should_get_students_ordered_by_name()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var brunoId = (await director.CreateStudent("Bruno Silva", DataGen.Email).Success()).Id;
        var anaId = (await director.CreateStudent("Ana Lima", DataGen.Email).Success()).Id;

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = brunoId, Relationship = ParentRelationship.Father },
            new() { StudentId = anaId, Relationship = ParentRelationship.Father },
        ]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeSuccess();
        var items = result.Success.Items;
        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Ana Lima");
        items[1].Name.Should().Be("Bruno Silva");
    }

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_students_linked_to_other_parents()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;
        var otherStudentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();
        await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = otherStudentId, Relationship = ParentRelationship.Father }]);

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeSuccess();
        var items = result.Success.Items;
        items.Should().HaveCount(1);
        items[0].Id.Should().Be(studentId);
    }

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_student_when_link_is_revoked()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        await director.RevokeParentStudentLink(parent.Id, studentId).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Parents_GetParentStudents_Should_not_get_student_when_link_was_revoked_by_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success();

        var studentClient = await _back.LoginAs(student.Email);
        await studentClient.RevokeParentLink(parent.Id).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.GetParentStudents();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Items.Should().BeEmpty();
    }

    #endregion
}
