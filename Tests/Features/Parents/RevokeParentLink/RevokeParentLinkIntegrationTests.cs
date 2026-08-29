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

        var parentEmail = DataGen.Email;
        var parentId = (await director.CreateParent(DataGen.UserName, parentEmail, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(parentEmail);

        // Act
        var result = await client.RevokeParentLink(parentId);

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
        var studentEmail = DataGen.Email;
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail, birthdate: AdultBirthdate).Success()).Id;
        var otherStudentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]);
        var otherParentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = otherStudentId, Relationship = ParentRelationship.Father }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);

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
        var studentEmail = DataGen.Email;
        var birthdate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18).AddDays(1);
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail, birthdate: birthdate).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);

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
        var studentEmail = DataGen.Email;
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);

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
        var studentEmail = DataGen.Email;
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail, birthdate: AdultBirthdate).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);
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
        var studentEmail = DataGen.Email;
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail, birthdate: AdultBirthdate).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);

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
        var studentEmail = DataGen.Email;
        var studentId = (await director.CreateStudent(DataGen.UserName, studentEmail, birthdate: AdultBirthdate).Success()).Id;

        var motherId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;
        var fatherId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Father }]).Success()).Id;

        var client = await _back.LoginAs(studentEmail);

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
