using Estud.Back.Features.Identity.ResetPassword;

namespace Estud.Tests.Integration;

public partial class IntegrationTests : IntegrationTestBase
{
    #region Validation errors

    [Test]
    [TestCaseSource(nameof(InvalidPasswords))]
    public async Task Identity_ResetPassword_Should_not_reset_password_to_a_weak_one(string password)
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = await _back.GetResetPasswordToken(user.Email);

        // Act
        var result = await client.ResetPassword(token!, password);

        // Assert
        result.ShouldBeError(WeakPassword.I);
    }

    private static IEnumerable<object[]> InvalidPasswords()
    {
        foreach (var role in new List<string>()
        {
            "",
            " ",
            "capi",
            "capi123",
            "Capi123",
            "lalal.com",
            "12@3lalala",
            "5816811681816",
        })
        {
            yield return [role];
        }
    }

    [Test]
    public async Task Identity_ResetPassword_Should_record_activity_when_reset_password_is_weak()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = await _back.GetResetPasswordToken(user.Email);

        // Act
        var result = await client.ResetPassword(token!, "weak");

        // Assert
        result.ShouldBeError(WeakPassword.I);
    }

    [Test]
    public async Task Identity_ResetPassword_Should_not_reset_password_with_wrong_token()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = "token_errado_lalala";

        // Act
        var result = await client.ResetPassword(token, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeError(InvalidResetPasswordToken.I);
    }

    [Test]
    public async Task Identity_ResetPassword_Should_not_reset_password_twice_with_same_token()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = await _back.GetResetPasswordToken(user.Email);

        await client.ResetPassword(token!, "My@nEw@strong@P4ssword");

        // Act
        var result = await client.ResetPassword(token!, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeError(InvalidResetPasswordToken.I);
    }

    [Test]
    public async Task Identity_ResetPassword_Should_not_reset_password_with_expired_token()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var tokenId = await _back.GetResetPasswordToken(user.Email);

        // Nenhum endpoint expira um token de reset, então a expiração vai direto no banco.
        await using var ctx = _back.GetDbContext();
        var resetToken = await ctx.ResetPasswordTokens.FirstAsync(r => r.Id == Guid.Parse(tokenId!));
        resetToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await ctx.SaveChangesAsync();

        // Act
        var result = await client.ResetPassword(tokenId!, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeError(InvalidResetPasswordToken.I);
    }

    [Test]
    public async Task Identity_ResetPassword_Should_not_reset_password_with_invalidated_token_after_security_stamp_change()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var firstToken = await _back.GetResetPasswordToken(user.Email);

        await client.SendResetPasswordToken(user.Email);
        var secondToken = await _back.GetResetPasswordToken(user.Email);

        await client.ResetPassword(secondToken!, "My@nEw@strong@P4ssword");

        // Act
        var result = await client.ResetPassword(firstToken!, "An0ther@strong@P4ssword");

        // Assert
        result.ShouldBeError(InvalidResetPasswordToken.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_ResetPassword_Should_reset_password()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = await _back.GetResetPasswordToken(user.Email);

        // Act
        var result = await client.ResetPassword(token!, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeSuccess();

        await using var ctx = _back.GetDbContext();
        var webUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        webUser!.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task Identity_ResetPassword_Should_login_with_new_password_after_reset()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var token = await _back.GetResetPasswordToken(user.Email);

        await client.ResetPassword(token!, "My@nEw@strong@P4ssword");

        // Act
        var result = await client.EmailPasswordLogin(user.Email, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeSuccess();
        result.Success.UserId.Should().Be(user.Id);

        await using var ctx = _back.GetDbContext();
        var webUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        webUser!.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task Identity_ResetPassword_Should_use_latest_token_when_requesting_reset_multiple_times()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        await client.SendResetPasswordToken(user.Email);
        var firstToken = await _back.GetResetPasswordToken(user.Email);

        await client.SendResetPasswordToken(user.Email);
        var secondToken = await _back.GetResetPasswordToken(user.Email);

        firstToken.Should().NotBe(secondToken);

        // Act
        var result = await client.ResetPassword(secondToken!, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeSuccess();

        var loginResult = await client.EmailPasswordLogin(user.Email, "My@nEw@strong@P4ssword");
        loginResult.ShouldBeSuccess();

        await using var ctx = _back.GetDbContext();
        var webUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        webUser!.EmailConfirmed.Should().BeTrue();
    }

    #endregion
}
