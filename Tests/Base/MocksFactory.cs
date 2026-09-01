using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Estud.Tests.Base;

extern alias Mocks;

public class MocksFactory : WebApplicationFactory<Mocks::Program>
{
    public const string Url = "http://localhost:5678";

    private static readonly HttpClient _http = new() { BaseAddress = new Uri(Url) };

    public MocksFactory() : base()
    {
        UseKestrel(o => o.ListenLocalhost(5678));
    }

    public async Task SetDnsTxtRecord(string name, params string[] values)
    {
        var response = await _http.PutAsJsonAsync("dns/records", new { name, values });
        response.EnsureSuccessStatusCode();
    }
}
