namespace Estud.Tests.Auth;

public class EstudPermissionsIsAllowedForUnitTests
{
    private static IEnumerable<TestCaseData> AllowedCases()
    {
        foreach (var permission in EstudPermissions.Permissions)
            foreach (var type in permission.AllowedTypes)
                yield return new TestCaseData(permission.Id, type).SetArgDisplayNames($"{permission.Id}", $"{type}");
    }

    private static IEnumerable<TestCaseData> NotAllowedCases()
    {
        foreach (var permission in EstudPermissions.Permissions)
            foreach (var type in Enum.GetValues<UserType>().Except(permission.AllowedTypes))
                yield return new TestCaseData(permission.Id, type).SetArgDisplayNames($"{permission.Id}", $"{type}");
    }

    #region Allowed

    [Test]
    [TestCaseSource(nameof(AllowedCases))]
    public void EstudPermissions_IsAllowedFor_Should_allow_every_declared_type_of_every_permission(int permissionId, UserType userType)
    {
        var result = EstudPermissions.IsAllowedFor(permissionId, userType);

        result.Should().BeTrue();
    }

    [Test]
    [TestCase(0)]      // ManageRoles, primeiro id do enum de grupos
    [TestCase(100)]    // ManageInstitutionConfig
    [TestCase(1000)]   // ManageClasses, primeiro id de 4 dígitos
    [TestCase(1600)]   // ManageParents, maior id declarado
    public void EstudPermissions_IsAllowedFor_Should_allow_manager_on_boundary_ids(int permissionId)
    {
        var result = EstudPermissions.IsAllowedFor(permissionId, UserType.Manager);

        result.Should().BeTrue();
    }

    #endregion

    #region Not allowed

    [Test]
    [TestCaseSource(nameof(NotAllowedCases))]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_types_outside_the_declared_list(int permissionId, UserType userType)
    {
        var result = EstudPermissions.IsAllowedFor(permissionId, userType);

        result.Should().BeFalse();
    }

    [Test]
    [TestCase(UserType.Teacher)]
    [TestCase(UserType.Student)]
    [TestCase(UserType.Parent)]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_non_manager_types_on_any_permission(UserType userType)
    {
        var allowed = EstudPermissions.Permissions
            .Where(x => EstudPermissions.IsAllowedFor(x.Id, userType))
            .Select(x => x.Name)
            .ToList();

        allowed.Should().BeEmpty();
    }

    #endregion

    #region Invalid permission id

    [Test]
    [TestCase(-1)]
    [TestCase(3)]              // logo após ManageTwoFactor (2)
    [TestCase(99)]             // logo antes de ManageInstitutionConfig (100)
    [TestCase(101)]
    [TestCase(1500)]           // buraco real entre Calendar (1400) e Parents (1600)
    [TestCase(1601)]           // logo após o maior id declarado
    [TestCase(int.MaxValue)]
    [TestCase(int.MinValue)]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_when_permission_does_not_exist(int permissionId)
    {
        var result = EstudPermissions.IsAllowedFor(permissionId, UserType.Manager);

        result.Should().BeFalse();
    }

    [Test]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_any_type_when_permission_does_not_exist()
    {
        var results = Enum.GetValues<UserType>()
            .Select(type => EstudPermissions.IsAllowedFor(9999, type))
            .ToList();

        results.Should().OnlyContain(x => x == false);
    }

    #endregion

    #region Invalid user type

    [Test]
    [TestCase((UserType)4)]    // primeiro valor fora do enum
    [TestCase((UserType)99)]
    [TestCase((UserType)(-1))]
    [TestCase((UserType)int.MaxValue)]
    [TestCase((UserType)int.MinValue)]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_when_user_type_is_not_defined(UserType userType)
    {
        var results = EstudPermissions.Permissions
            .Select(x => EstudPermissions.IsAllowedFor(x.Id, userType))
            .ToList();

        results.Should().OnlyContain(x => x == false);
    }

    [Test]
    public void EstudPermissions_IsAllowedFor_Should_not_allow_when_both_permission_and_user_type_are_invalid()
    {
        var result = EstudPermissions.IsAllowedFor(-1, (UserType)99);

        result.Should().BeFalse();
    }

    #endregion

    #region Consistency

    [Test]
    public void EstudPermissions_IsAllowedFor_Should_be_true_for_at_least_one_type_of_every_permission()
    {
        var orphans = EstudPermissions.Permissions
            .Where(x => !Enum.GetValues<UserType>().Any(type => EstudPermissions.IsAllowedFor(x.Id, type)))
            .Select(x => x.Name)
            .ToList();

        orphans.Should().BeEmpty();
    }

    [Test]
    public void EstudPermissions_IsAllowedFor_Should_return_the_same_result_on_repeated_calls()
    {
        var first = EstudPermissions.IsAllowedFor(EstudPermissions.ManageRoles.Id, UserType.Manager);
        var second = EstudPermissions.IsAllowedFor(EstudPermissions.ManageRoles.Id, UserType.Manager);

        first.Should().Be(second);
    }

    #endregion
}
