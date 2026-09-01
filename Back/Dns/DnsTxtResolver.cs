using System.Text.Json;

namespace Estud.Back.Dns;

/// <summary>
/// Resolves TXT records over DNS-over-HTTPS (RFC 8484 JSON), so the lookup goes through
/// a plain <see cref="HttpClient"/> and can be pointed at the mocks server in tests.
/// </summary>
public class DnsTxtResolver(SsoSettings settings, IHttpClientFactory httpClientFactory, ILogger<DnsTxtResolver> logger)
{
    private const int TxtRecordType = 16;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<DnsTxtLookup> ResolveTxt(string name)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(settings.DnsTimeoutInSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/dns-json");

            var url = $"{settings.DnsResolverUrl}?name={Uri.EscapeDataString(name)}&type=TXT";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return DnsTxtLookup.Fail($"Resolvedor DNS respondeu {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync();
            var doh = JsonSerializer.Deserialize<DohResponse>(body, JsonOptions);

            if (doh == null) return DnsTxtLookup.Fail("Resposta inválida do resolvedor DNS.");

            // NXDOMAIN just means the record is not published yet, which is a normal state here.
            if (doh.Status == 3) return DnsTxtLookup.Ok([]);
            if (doh.Status != 0) return DnsTxtLookup.Fail($"Consulta DNS retornou status {doh.Status}.");

            var records = (doh.Answer ?? [])
                .Where(a => a.Type == TxtRecordType && a.Data.HasValue())
                .Select(a => Unquote(a.Data))
                .ToList();

            return DnsTxtLookup.Ok(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DnsTxtResolver] failed resolving TXT for {Name}", name);
            return DnsTxtLookup.Fail("Não foi possível consultar o DNS do domínio.");
        }
    }

    /// <summary>
    /// TXT values come back quoted, and values over 255 chars arrive split as `"part1" "part2"`.
    /// </summary>
    private static string Unquote(string data) => data.Replace("\"", "").Trim();

    private class DohResponse
    {
        public int Status { get; set; }
        public List<DohAnswer>? Answer { get; set; }
    }

    private class DohAnswer
    {
        public int Type { get; set; }
        public string Data { get; set; }
    }
}
