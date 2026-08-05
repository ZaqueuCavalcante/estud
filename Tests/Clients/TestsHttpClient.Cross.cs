namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<HttpResponseMessage> GetHealth()
    {
        return await http.GetAsync("health");
    }

    public async Task<HttpResponseMessage> GetHome()
    {
        return await http.GetAsync("");
    }

    public async Task<HttpResponseMessage> GetVersion()
    {
        return await http.GetAsync("version");
    }

    public async Task<HttpResponseMessage> GetSwaggerDocs()
    {
        return await http.GetAsync("swagger/v1/swagger.json");
    }

    public async Task<HttpResponseMessage> GetOpenApiDocs()
    {
        return await http.GetAsync("openapi/v1.json");
    }
}
