using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Estud.Tests.Base;

extern alias Mocks;

public class MocksFactory : WebApplicationFactory<Mocks::Program>
{
    public const string Url = "http://localhost:5678";
    public const string OidcAuthority = $"{Url}/oidc";

    private static readonly HttpClient Http = new() { BaseAddress = new Uri(Url) };

    public MocksFactory() : base()
    {
        UseKestrel(o => o.ListenLocalhost(5678));
    }

    public static async Task PublishDnsTxtRecords(string name, params string[] values)
    {
        var response = await Http.PostAsJsonAsync("dns/records", new { name, txt = values });
        response.EnsureSuccessStatusCode();
    }

    public static async Task PublishDnsServerFailure(string name)
    {
        var response = await Http.PostAsJsonAsync("dns/records", new { name, serverFailure = true });
        response.EnsureSuccessStatusCode();
    }
}
