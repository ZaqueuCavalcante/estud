using System.Text.Json;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Cross_Docs_Should_not_expose_admin_endpoints_on_swagger()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var response = await client.GetSwaggerDocs();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paths = await ReadDocsPaths(response);
        paths.Should().NotBeEmpty();
        paths.Where(IsAdminPath).Should().BeEmpty();
    }

    [Test]
    public async Task Cross_Docs_Should_not_expose_admin_endpoints_on_openapi()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var response = await client.GetOpenApiDocs();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paths = await ReadDocsPaths(response);
        paths.Should().NotBeEmpty();
        paths.Where(IsAdminPath).Should().BeEmpty();
    }

    [Test]
    public async Task Cross_Docs_Should_keep_admin_endpoints_routed_even_when_hidden()
    {
        // Arrange — garante que o teste acima não passa por rota admin inexistente.
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetInstitutions();

        // Assert — a rota existe (401, não 404), mas fica fora da documentação.
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    private static async Task<List<string>> ReadDocsPaths(HttpResponseMessage response)
    {
        using var docs = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return docs.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(x => x.Name)
            .ToList();
    }

    private static bool IsAdminPath(string path)
    {
        return path.TrimStart('/').StartsWith("admin/", StringComparison.OrdinalIgnoreCase);
    }
}
