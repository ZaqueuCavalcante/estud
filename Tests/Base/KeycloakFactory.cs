using System.Net.Http.Json;
using Testcontainers.Keycloak;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Estud.Tests.Base;

public record KeycloakLoginPage(Uri Action, string Username, string Cookies);

public static partial class KeycloakFactory
{
    public const string ClientId = "estud";
    public const string ClientSecret = "estud-keycloak-secret";
    public const string UserPassword = "Keycloak@123";

    private const int Port = 5446;
    private const string Realm = "estud";
    public static readonly string Url = $"http://localhost:{Port}";
    public static readonly string Authority = $"{Url}/realms/{Realm}";

    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });

    // Sobe só quando o primeiro teste de Keycloak roda, pra não pesar nas execuções que não o usam.
    private static readonly Lazy<Task> Started = new(async () =>
    {
        var container = new KeycloakBuilder("quay.io/keycloak/keycloak:26.4")
            .WithName("estud-tests-keycloak")
            .WithRealm(Path.Combine(Directory.GetCurrentDirectory(), "Base", "KeycloakRealm.json"))
            .WithPortBinding(Port, 8080)
            .WithReuse(true)
            .Build();

        await container.StartAsync();
    });

    public static Task Start() => Started.Value;

    public static async Task CreateUser(string email, bool emailVerified = true)
    {
        await Start();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}/admin/realms/{Realm}/users");
        request.Headers.Authorization = new("Bearer", await GetAdminToken());
        // Sem firstName/lastName o user profile do Keycloak exige "Update profile" antes de voltar pro client.
        request.Content = JsonContent.Create(new
        {
            username = email,
            email,
            emailVerified,
            enabled = true,
            firstName = "Usuário",
            lastName = "Keycloak",
            credentials = new[] { new { type = "password", value = UserPassword, temporary = false } },
        });

        var response = await Http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<KeycloakLoginPage> OpenLoginPage(Uri authorizeUrl)
    {
        var url = authorizeUrl;
        var cookies = new Dictionary<string, string>();

        for (var hops = 0; ; hops++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (cookies.Count > 0) request.Headers.Add("Cookie", string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));

            var response = await Http.SendAsync(request);

            // Os cookies de sessão do Keycloak podem vir Secure e aqui é http, então são reenviados na mão.
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var pair in setCookies.Select(c => c.Split(';')[0].Split('=', 2)))
                {
                    cookies[pair[0]] = pair.Length > 1 ? pair[1] : "";
                }
            }

            var location = response.Headers.Location;
            if (location != null && hops < 5)
            {
                url = location.IsAbsoluteUri ? location : new Uri(url, location);
                if (url.GetLeftPart(UriPartial.Authority) == Url) continue;
            }

            var html = await response.Content.ReadAsStringAsync();

            var action = LoginFormAction().Match(html);
            if (!action.Success) throw new InvalidOperationException($"Keycloak login form not found ({(int)response.StatusCode}, Location: {location}): {html}");

            var usernameInput = UsernameInput().Match(html).Value;
            var username = InputValue().Match(usernameInput).Groups[1].Value;

            return new KeycloakLoginPage(
                new Uri(WebUtility.HtmlDecode(action.Groups[1].Value)),
                WebUtility.HtmlDecode(username),
                string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}")));
        }
    }

    public static async Task<HttpResponseMessage> SubmitLogin(KeycloakLoginPage page, string email, string password = UserPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, page.Action);
        request.Headers.Add("Cookie", page.Cookies);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = email,
            ["password"] = password,
            ["credentialId"] = "",
        });

        return await Http.SendAsync(request);
    }

    public static async Task<HttpRequestMessage> BuildCallback(HttpResponseMessage authenticate)
    {
        var html = await authenticate.Content.ReadAsStringAsync();

        var action = FormPostAction().Match(html);
        if (!action.Success) throw new InvalidOperationException($"Keycloak form_post response not found ({(int)authenticate.StatusCode}, Location: {authenticate.Headers.Location}): {html}");

        var fields = HiddenInputs().Matches(html).ToDictionary(
            m => WebUtility.HtmlDecode(m.Groups[1].Value),
            m => WebUtility.HtmlDecode(m.Groups[2].Value));

        return new HttpRequestMessage(HttpMethod.Post, WebUtility.HtmlDecode(action.Groups[1].Value))
        {
            Content = new FormUrlEncodedContent(fields),
        };
    }

    private static async Task<string> GetAdminToken()
    {
        var response = await Http.PostAsync($"{Url}/realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = "admin",
            ["password"] = "admin",
        }));
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AdminToken>();
        return token!.AccessToken;
    }

    private record AdminToken([property: JsonPropertyName("access_token")] string AccessToken);

    [GeneratedRegex(@"<form[^>]*id=""kc-form-login""[^>]*action=""([^""]+)""")]
    private static partial Regex LoginFormAction();

    [GeneratedRegex(@"<form[^>]*method=""post""[^>]*action=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex FormPostAction();

    [GeneratedRegex(@"<input[^>]*type=""hidden""[^>]*name=""([^""]+)""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex HiddenInputs();

    [GeneratedRegex(@"<input[^>]*name=""username""[^>]*>")]
    private static partial Regex UsernameInput();

    [GeneratedRegex(@"value=""([^""]*)""")]
    private static partial Regex InputValue();
}
