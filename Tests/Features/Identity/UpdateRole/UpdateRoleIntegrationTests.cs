using Estud.Back.Domain.Identity;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateRole(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateRole(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Identity_UpdateRole_Should_not_update_role_with_invalid_name(string? name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateRole(1, name: name!);

        // Assert
        result.ShouldBeError(InvalidRoleName.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_name_above_the_size_limit()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: new string('a', 51));

        // Assert
        result.ShouldBeError(InvalidRoleName.I);
    }

    [Test]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Identity_UpdateRole_Should_not_update_role_with_invalid_description(string? description)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateRole(1, description: description!);

        // Assert
        result.ShouldBeError(InvalidRoleDescription.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_description_above_the_size_limit()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Secretaria", description: new string('a', 201));

        // Assert
        result.ShouldBeError(InvalidRoleDescription.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_invalid_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateRole(1, permissions: [99999]);

        // Assert
        result.ShouldBeError(InvalidPermissionsList.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_duplicated_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Secretaria", permissions: [EstudPermissions.ManageStudents.Id, EstudPermissions.ManageStudents.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsList.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_permissions_not_allowed_for_the_base_type()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Professor Substituto", baseType: UserType.Teacher, permissions: []).Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Professor Substituto", permissions: [EstudPermissions.ManageRoles.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsForUserType.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_add_manager_permissions_to_the_default_student_role()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var roles = await client.GetRoles().Success();
        var studentRole = roles.Items.First(r => r.BaseType == UserType.Student);

        // Act
        var result = await client.UpdateRole(studentRole.Id, name: studentRole.Name, description: studentRole.Description, permissions: [EstudPermissions.ManageStudents.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsForUserType.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_when_it_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateRole(999999);

        // Assert
        result.ShouldBeError(RoleNotFound.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_other_institution_role()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        var otherRole = await otherClient.CreateRole(name: "Secretaria").Success();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateRole(otherRole.Id, name: "Invadida");

        // Assert
        result.ShouldBeError(RoleNotFound.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_when_name_already_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");
        var editor = await client.CreateRole(name: "Editor").Success();

        // Act
        var result = await client.UpdateRole(editor.Id, name: "Admin");

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("admin")]
    [TestCase("ADMIN")]
    [TestCase("AdMiN")]
    public async Task Identity_UpdateRole_Should_not_update_role_when_name_already_exists_ignoring_case(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");
        var editor = await client.CreateRole(name: "Editor").Success();

        // Act
        var result = await client.UpdateRole(editor.Id, name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("  Admin")]
    [TestCase("Admin  ")]
    [TestCase("  Admin  ")]
    public async Task Identity_UpdateRole_Should_not_update_role_when_name_already_exists_ignoring_surrounding_spaces(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");
        var editor = await client.CreateRole(name: "Editor").Success();

        // Act
        var result = await client.UpdateRole(editor.Id, name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("Coordenacao")]
    [TestCase("coordenacao")]
    [TestCase("COORDENAÇÃO")]
    public async Task Identity_UpdateRole_Should_not_update_role_when_name_already_exists_ignoring_accents(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Coordenação");
        var editor = await client.CreateRole(name: "Editor").Success();

        // Act
        var result = await client.UpdateRole(editor.Id, name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("Diretor")]
    [TestCase("  Diretor  ")]
    [TestCase("Responsável")]
    [TestCase("Responsavel")]
    public async Task Identity_UpdateRole_Should_not_update_role_when_name_conflicts_with_a_default_role(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_that_has_more_permissions_than_the_user_has()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRoleResult = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id]).Success();
        var limitedRoleId = limitedRoleResult.Id;
        var powerfulRoleResult = await director.CreateRole(name: "Poderosa", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageSso.Id]).Success();
        var powerfulRoleId = powerfulRoleResult.Id;
        var userId = director.User.Id;

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == userId);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRoleId));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.UpdateRole(powerfulRoleId, name: "Poderosa", permissions: [EstudPermissions.ManageRoles.Id]);

        // Assert
        result.ShouldBeError(InvalidRolePermissions.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_update_role_with_more_permissions_than_the_user_has()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRoleResult = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id]).Success();
        var limitedRoleId = limitedRoleResult.Id;
        var editableRoleResult = await director.CreateRole(name: "Editável", permissions: [EstudPermissions.ManageRoles.Id]).Success();
        var editableRoleId = editableRoleResult.Id;
        var userId = director.User.Id;

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == userId);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRoleId));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.UpdateRole(editableRoleId, name: "Editável", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageSso.Id]);

        // Assert
        result.ShouldBeError(InvalidRolePermissions.I);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_let_the_user_escalate_the_permissions_of_their_own_role()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRoleResult = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id]).Success();
        var limitedRoleId = limitedRoleResult.Id;
        var userId = director.User.Id;

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == userId);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRoleId));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.UpdateRole(limitedRoleId, name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageUsers.Id]);

        // Assert
        result.ShouldBeError(InvalidRolePermissions.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_UpdateRole_Should_update_role()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Admin", description: "Administrador", baseType: UserType.Manager, permissions: []).Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Gestor", description: "Gestor acadêmico", permissions: []);

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Name.Should().Be("Gestor");
        updated.Description.Should().Be("Gestor acadêmico");
    }

    [Test]
    public async Task Identity_UpdateRole_Should_update_role_keeping_its_own_name()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria", description: "Secretaria acadêmica").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Secretaria", description: "Secretaria de graduação");

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Name.Should().Be("Secretaria");
        updated.Description.Should().Be("Secretaria de graduação");
    }

    [Test]
    public async Task Identity_UpdateRole_Should_update_role_trimming_the_name()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "  Coordenação  ");

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Name.Should().Be("Coordenação");
    }

    [Test]
    public async Task Identity_UpdateRole_Should_update_role_name_casing()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "SECRETARIA");

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Name.Should().Be("SECRETARIA");
    }

    [Test]
    public async Task Identity_UpdateRole_Should_replace_role_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria", permissions: [EstudPermissions.ManageStudents.Id]).Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Secretaria", permissions: [EstudPermissions.ManageTeachers.Id, EstudPermissions.ManageCourses.Id]);

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Permissions.Should().BeEquivalentTo([EstudPermissions.ManageTeachers.Id, EstudPermissions.ManageCourses.Id]);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_remove_all_role_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria", permissions: [EstudPermissions.ManageStudents.Id, EstudPermissions.ManageTeachers.Id]).Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Secretaria", permissions: []);

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Permissions.Should().BeEmpty();
    }

    [Test]
    public async Task Identity_UpdateRole_Should_not_change_the_role_base_type()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Professor Substituto", baseType: UserType.Teacher, permissions: []).Success();

        // Act
        await client.UpdateRole(role.Id, name: "Professor Visitante", permissions: []);

        // Assert
        var updated = await client.GetRole(role.Id).Success();
        updated.BaseType.Should().Be(UserType.Teacher);
    }

    [Test]
    public async Task Identity_UpdateRole_Should_update_role_with_a_name_already_used_by_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        await otherClient.CreateRole(name: "Coordenação").Success();

        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRole(name: "Secretaria").Success();

        // Act
        var result = await client.UpdateRole(role.Id, name: "Coordenação");

        // Assert
        result.Success.Id.Should().Be(role.Id);

        var updated = await client.GetRole(role.Id).Success();
        updated.Name.Should().Be("Coordenação");
    }

    #endregion
}
