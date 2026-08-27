namespace Estud.Tests.Extensions;

public class EnvironmentExtensionsUnitTests
{
    [Test]
    public void EnvironmentExtensions_Should_get_env_as_testing()
    {
        // Arrange
        EnvironmentExtensions.SetAsTesting();

        // Act
        var env = EnvironmentExtensions.IsTesting();

        // Assert
        env.Should().BeTrue();
    }
}
