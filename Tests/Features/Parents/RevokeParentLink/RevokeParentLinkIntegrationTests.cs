namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.RevokeParentLink(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_user_is_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.RevokeParentLink(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_user_is_a_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        var parent = await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success();

        var client = await _back.LoginAs(parent.Email);

        // Act
        var result = await client.RevokeParentLink(parent.Id);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_student_is_not_linked_to_the_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();
        var otherStudentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]);
        var otherParentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = otherStudentId, Relationship = ParentRelationship.Father }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RevokeParentLink(otherParentId);

        // Assert
        result.ShouldBeError(ParentStudentLinkNotFound.I);
    }

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_student_is_under_age()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var birthdate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18).AddDays(1);
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: birthdate).Success();

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RevokeParentLink(parentId);

        // Assert
        result.ShouldBeError(StudentMustBeAdult.I);
    }

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_student_has_no_birthdate()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RevokeParentLink(parentId);

        // Assert
        result.ShouldBeError(StudentMustBeAdult.I);
    }

    [Test]
    public async Task Parents_RevokeParentLink_Should_not_revoke_link_when_it_is_already_revoked()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);
        await client.RevokeParentLink(parentId).Success();

        // Act
        var result = await client.RevokeParentLink(parentId);

        // Assert
        result.ShouldBeError(ParentStudentLinkAlreadyRevoked.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Parents_RevokeParentLink_Should_revoke_link()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RevokeParentLink(parentId);

        // Assert
        result.ShouldBeSuccess();

        var details = await director.GetParentDetails(parentId).Success();
        details.Students.Should().HaveCount(1);
        details.Students[0].RevokedByStudent.Should().BeTrue();
        details.Students[0].LinkStatus.Should().Be(ParentStudentStatus.Active);
    }

    [Test]
    public async Task Parents_RevokeParentLink_Should_revoke_only_the_link_of_the_informed_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email, birthdate: AdultBirthdate).Success();

        var motherId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Mother }]).Success()).Id;
        var fatherId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = student.Id, Relationship = ParentRelationship.Father }]).Success()).Id;

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RevokeParentLink(motherId);

        // Assert
        result.ShouldBeSuccess();

        var mother = await director.GetParentDetails(motherId).Success();
        mother.Students[0].RevokedByStudent.Should().BeTrue();

        var father = await director.GetParentDetails(fatherId).Success();
        father.Students[0].RevokedByStudent.Should().BeFalse();
    }

    #endregion

    private static DateOnly AdultBirthdate => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-20);
}
