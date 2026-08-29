namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.RevokeParentStudentLink(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.RevokeParentStudentLink(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_when_user_is_a_parent()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        var parentEmail = DataGen.Email;
        var parentId = (await director.CreateParent(DataGen.UserName, parentEmail, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var client = await _back.LoginAs(parentEmail);

        // Act
        var result = await client.RevokeParentStudentLink(parentId, studentId);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_when_link_does_not_exist()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;
        var otherStudentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        // Act
        var result = await director.RevokeParentStudentLink(parentId, otherStudentId);

        // Assert
        result.ShouldBeError(ParentStudentLinkNotFound.I);
    }

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;
        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        var otherDirector = await _back.LoggedAsDirector();

        // Act
        var result = await otherDirector.RevokeParentStudentLink(parentId, studentId);

        // Assert
        result.ShouldBeError(ParentStudentLinkNotFound.I);
    }

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_not_revoke_link_when_it_is_already_revoked()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;
        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        await director.RevokeParentStudentLink(parentId, studentId).Success();

        // Act
        var result = await director.RevokeParentStudentLink(parentId, studentId);

        // Assert
        result.ShouldBeError(ParentStudentLinkAlreadyRevoked.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_revoke_link()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var studentId = (await director.CreateStudent(DataGen.UserName, DataGen.Email).Success()).Id;
        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email, [new() { StudentId = studentId, Relationship = ParentRelationship.Mother }]).Success()).Id;

        // Act
        var result = await director.RevokeParentStudentLink(parentId, studentId);

        // Assert
        result.ShouldBeSuccess();

        var details = await director.GetParentDetails(parentId).Success();
        details.Students.Should().HaveCount(1);
        details.Students[0].LinkStatus.Should().Be(ParentStudentStatus.Revoked);
    }

    [Test]
    public async Task Parents_RevokeParentStudentLink_Should_revoke_only_the_informed_link()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var anaId = (await director.CreateStudent("Ana Lima", DataGen.Email).Success()).Id;
        var brunoId = (await director.CreateStudent("Bruno Silva", DataGen.Email).Success()).Id;

        var parentId = (await director.CreateParent(DataGen.UserName, DataGen.Email,
        [
            new() { StudentId = anaId, Relationship = ParentRelationship.Mother },
            new() { StudentId = brunoId, Relationship = ParentRelationship.Mother },
        ]).Success()).Id;

        // Act
        var result = await director.RevokeParentStudentLink(parentId, anaId);

        // Assert
        result.ShouldBeSuccess();

        var details = await director.GetParentDetails(parentId).Success();
        details.Students.Should().HaveCount(2);
        details.Students.Find(x => x.Id == anaId)!.LinkStatus.Should().Be(ParentStudentStatus.Revoked);
        details.Students.Find(x => x.Id == brunoId)!.LinkStatus.Should().Be(ParentStudentStatus.Active);
    }

    #endregion
}
