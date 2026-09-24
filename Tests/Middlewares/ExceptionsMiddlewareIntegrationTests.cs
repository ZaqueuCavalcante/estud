namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Middlewares_Exceptions_Should_return_internal_server_error_when_request_throws()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var response = await client.ThrowException();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var error = await response.ToError();
        error.Code.Should().Be("Error");
        error.Message.Should().Be($"{ThrowExceptionStartupFilter.Message} --> {ThrowExceptionStartupFilter.InnerMessage}");
    }

    #endregion
}
