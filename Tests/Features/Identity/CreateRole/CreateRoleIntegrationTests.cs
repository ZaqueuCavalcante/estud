using Estud.Back.Domain.Identity;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateRole();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateRole();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Identity_CreateRole_Should_not_create_role_with_invalid_name(string? name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: name!);

        // Assert
        result.ShouldBeError(InvalidRoleName.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_name_above_the_size_limit()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: new string('a', 51));

        // Assert
        result.ShouldBeError(InvalidRoleName.I);
    }

    [Test]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public async Task Identity_CreateRole_Should_not_create_role_with_invalid_description(string? description)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(description: description!);

        // Assert
        result.ShouldBeError(InvalidRoleDescription.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_description_above_the_size_limit()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(description: new string('a', 201));

        // Assert
        result.ShouldBeError(InvalidRoleDescription.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_invalid_base_type()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(baseType: (UserType)99);

        // Assert
        result.ShouldBeError(InvalidRoleBaseType.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_invalid_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(permissions: [99999]);

        // Assert
        result.ShouldBeError(InvalidPermissionsList.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_negative_permission_id()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(permissions: [-1]);

        // Assert
        result.ShouldBeError(InvalidPermissionsList.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_duplicated_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(permissions: [EstudPermissions.ManageStudents.Id, EstudPermissions.ManageStudents.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsList.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_permissions_not_allowed_for_the_base_type()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(baseType: UserType.Teacher, permissions: [EstudPermissions.ManageRoles.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsForUserType.I);
    }

    [Test]
    [TestCase(UserType.Teacher)]
    [TestCase(UserType.Student)]
    [TestCase(UserType.Parent)]
    public async Task Identity_CreateRole_Should_not_create_role_with_manager_permissions_for_non_manager_base_types(UserType baseType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(baseType: baseType, permissions: [EstudPermissions.ManageStudents.Id]);

        // Assert
        result.ShouldBeError(InvalidPermissionsForUserType.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_when_name_already_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");

        // Act
        var result = await client.CreateRole(name: "Admin");

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("admin")]
    [TestCase("ADMIN")]
    [TestCase("AdMiN")]
    public async Task Identity_CreateRole_Should_not_create_role_when_name_already_exists_ignoring_case(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");

        // Act
        var result = await client.CreateRole(name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("  Admin")]
    [TestCase("Admin  ")]
    [TestCase("  Admin  ")]
    public async Task Identity_CreateRole_Should_not_create_role_when_name_already_exists_ignoring_surrounding_spaces(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Admin");

        // Act
        var result = await client.CreateRole(name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("Coordenacao")]
    [TestCase("coordenacao")]
    [TestCase("COORDENAÇÃO")]
    public async Task Identity_CreateRole_Should_not_create_role_when_name_already_exists_ignoring_accents(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateRole(name: "Coordenação");

        // Act
        var result = await client.CreateRole(name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    [TestCase("Diretor")]
    [TestCase("Professor")]
    [TestCase("Aluno")]
    [TestCase("Responsável")]
    [TestCase("Responsavel")]
    public async Task Identity_CreateRole_Should_not_create_role_when_name_conflicts_with_a_default_role(string name)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: name);

        // Assert
        result.ShouldBeError(RoleNameAlreadyExists.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_with_more_permissions_than_the_user_has()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRole = await director.CreateRole(name: "Gerente de Segurança", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageTwoFactor.Id]).Success();

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == director.User.Id);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRole.Id));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.CreateRole(name: "Super Admin", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageCampi.Id]);

        // Assert
        result.ShouldBeError(InvalidRolePermissions.I);
    }

    [Test]
    public async Task Identity_CreateRole_Should_not_create_role_when_user_has_none_of_the_role_permissions()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRole = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id]).Success();

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == director.User.Id);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRole.Id));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.CreateRole(name: "Gerente de Usuários", permissions: [EstudPermissions.ManageUsers.Id]);

        // Assert
        result.ShouldBeError(InvalidRolePermissions.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_CreateRole_Should_create_role()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: "Admin", description: "Administrador", baseType: UserType.Manager, permissions: []);

        // Assert
        var role = result.Success;
        role.Id.Should().NotBe(0);
    }

    [Test]
    public async Task Identity_CreateRole_Should_create_role_with_permissions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(
            name: "Secretaria",
            description: "Secretaria acadêmica",
            baseType: UserType.Manager,
            permissions: [EstudPermissions.ManageStudents.Id, EstudPermissions.ManageTeachers.Id]
        );

        // Assert
        var created = await client.GetRole(result.Success.Id).Success();
        created.Name.Should().Be("Secretaria");
        created.Description.Should().Be("Secretaria acadêmica");
        created.BaseType.Should().Be(UserType.Manager);
        created.Permissions.Should().BeEquivalentTo([EstudPermissions.ManageStudents.Id, EstudPermissions.ManageTeachers.Id]);
    }

    [Test]
    public async Task Identity_CreateRole_Should_create_role_trimming_the_name()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: "  Secretaria  ");

        // Assert
        var created = await client.GetRole(result.Success.Id).Success();
        created.Name.Should().Be("Secretaria");
    }

    [Test]
    public async Task Identity_CreateRole_Should_create_role_with_name_and_description_at_the_size_limit()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: new string('a', 50), description: new string('b', 200));

        // Assert
        result.Success.Id.Should().NotBe(0);
    }

    [Test]
    [TestCase(UserType.Teacher)]
    [TestCase(UserType.Student)]
    [TestCase(UserType.Parent)]
    public async Task Identity_CreateRole_Should_create_role_for_non_manager_base_types_without_permissions(UserType baseType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: $"Perfil {baseType}", baseType: baseType, permissions: []);

        // Assert
        var created = await client.GetRole(result.Success.Id).Success();
        created.BaseType.Should().Be(baseType);
        created.Permissions.Should().BeEmpty();
    }

    [Test]
    public async Task Identity_CreateRole_Should_create_role_with_a_name_already_used_by_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        await otherClient.CreateRole(name: "Coordenação").Success();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRole(name: "Coordenação");

        // Assert
        result.Success.Id.Should().NotBe(0);
    }

    [Test]
    public async Task Identity_CreateRole_Should_create_role_with_a_subset_of_the_user_permissions()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);

        var limitedRole = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id, EstudPermissions.ManageStudents.Id]).Success();

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == director.User.Id);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRole.Id));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.CreateRole(name: "Secretaria", permissions: [EstudPermissions.ManageStudents.Id]);

        // Assert
        var created = await client.GetRole(result.Success.Id).Success();
        created.Permissions.Should().BeEquivalentTo([EstudPermissions.ManageStudents.Id]);
    }

    #endregion
}
