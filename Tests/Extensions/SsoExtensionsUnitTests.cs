namespace Estud.Tests.Extensions;

public class SsoExtensionsUnitTests
{
    [SetUp]
    public void SetUp()
    {
        EnvironmentExtensions.SetAsTesting();
    }

    [Test]
    [TestCase("https://8.8.8.8/")]
    [TestCase("https://[2001:4860:4860::8888]/")]
    [TestCase("https://login.microsoftonline.com/tenant-id/v2.0")]
    public void SsoExtensions_Should_allow_public_authority(string authority)
    {
        // Arrange / Act
        var result = authority.ValidateSsoAuthority();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [TestCase("")]
    [TestCase("login.microsoftonline.com")]
    public void SsoExtensions_Should_reject_invalid_authority(string authority)
    {
        // Arrange / Act
        var result = authority.ValidateSsoAuthority();

        // Assert
        result.Should().Be(InvalidSsoAuthority.I);
    }

    [Test]
    public void SsoExtensions_Should_require_https_authority()
    {
        // Arrange / Act
        var result = "http://login.microsoftonline.com/tenant-id/v2.0".ValidateSsoAuthority();

        // Assert
        result.Should().Be(SsoAuthorityMustBeHttps.I);
    }

    [Test]
    public void SsoExtensions_Should_block_authority_with_user_info()
    {
        // Arrange / Act
        var result = "https://evil.com@login.microsoftonline.com/tenant-id/v2.0".ValidateSsoAuthority();

        // Assert
        result.Should().Be(SsoAuthorityHasUserInfo.I);
    }

    [Test]
    [TestCase("https://169.254.169.254/")]
    [TestCase("https://169.254.0.1/")]
    [TestCase("https://2852039166/")]
    [TestCase("https://0xA9FEA9FE/")]
    [TestCase("https://[fe80::1]/")]
    [TestCase("https://[::ffff:169.254.169.254]/")]
    public void SsoExtensions_Should_block_link_local_authority(string authority)
    {
        // Arrange / Act
        var result = authority.ValidateSsoAuthority();

        // Assert
        result.Should().Be(SsoAuthorityLinkLocalNotAllowed.I);
    }

    [Test]
    public void SsoExtensions_Should_block_unspecified_ipv4_authority()
    {
        // Arrange / Act
        var result = "https://0.0.0.0/".ValidateSsoAuthority();

        // Assert
        result.Should().Be(SsoAuthorityLoopbackNotAllowed.I);
    }

    [Test]
    [TestCase("https://[::1]/")]
    [TestCase("https://10.0.0.1/")]
    [TestCase("https://[fd00::1]/")]
    [TestCase("https://127.0.0.1/")]
    [TestCase("https://172.16.0.1/")]
    [TestCase("https://192.168.1.1/")]
    [TestCase("http://localhost:8080/realms/estud")]
    public void SsoExtensions_Should_allow_local_authority_when_testing(string authority)
    {
        // Arrange / Act
        var result = authority.ValidateSsoAuthority();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [TestCase("estud.com", "estud.com")]
    [TestCase("a.b", "a.b")]
    [TestCase("123.com", "123.com")]
    [TestCase("mail.estud.com.br", "mail.estud.com.br")]
    [TestCase("my-school.edu.br", "my-school.edu.br")]
    [TestCase("my--school.edu.br", "my--school.edu.br")]
    [TestCase("xn--caf-dma.com", "xn--caf-dma.com")]
    public void SsoExtensions_NormalizeSsoDomain_Should_keep_valid_domain(string domain, string expected)
    {
        // Arrange / Act
        var result = domain.NormalizeSsoDomain();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("ESTUD.COM", "estud.com")]
    [TestCase("Mail.Estud.Com.Br", "mail.estud.com.br")]
    [TestCase("  estud.com", "estud.com")]
    [TestCase("estud.com  ", "estud.com")]
    [TestCase("\testud.com\n", "estud.com")]
    [TestCase("  Estud.COM  ", "estud.com")]
    public void SsoExtensions_NormalizeSsoDomain_Should_trim_and_lowercase_domain(string domain, string expected)
    {
        // Arrange / Act
        var result = domain.NormalizeSsoDomain();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase("@estud.com", "estud.com")]
    [TestCase("@ESTUD.COM", "estud.com")]
    [TestCase("  @Estud.com  ", "estud.com")]
    public void SsoExtensions_NormalizeSsoDomain_Should_remove_leading_at_sign(string domain, string expected)
    {
        // Arrange / Act
        var result = domain.NormalizeSsoDomain();

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void SsoExtensions_NormalizeSsoDomain_Should_return_null_when_empty(string? domain)
    {
        // Arrange / Act
        var result = domain!.NormalizeSsoDomain();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [TestCase("@")]
    [TestCase("  @  ")]
    [TestCase("@@estud.com")]
    [TestCase("@ estud.com")]
    [TestCase("estud.com@")]
    [TestCase("user@estud.com")]
    public void SsoExtensions_NormalizeSsoDomain_Should_return_null_when_at_sign_is_not_a_single_prefix(string domain)
    {
        // Arrange / Act
        var result = domain.NormalizeSsoDomain();

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [TestCase("estud")]
    [TestCase("localhost")]
    [TestCase(".estud.com")]
    [TestCase("estud.com.")]
    [TestCase("estud..com")]
    [TestCase("-estud.com")]
    [TestCase("estud-.com")]
    [TestCase("estud.-com")]
    [TestCase("estud.com-")]
    [TestCase("est ud.com")]
    [TestCase("estud_x.com")]
    [TestCase("café.com")]
    [TestCase("estud.com/path")]
    [TestCase("estud.com:443")]
    [TestCase("https://estud.com")]
    [TestCase("*.estud.com")]
    public void SsoExtensions_NormalizeSsoDomain_Should_return_null_when_domain_is_invalid(string domain)
    {
        // Arrange / Act
        var result = domain.NormalizeSsoDomain();

        // Assert
        result.Should().BeNull();
    }
}
