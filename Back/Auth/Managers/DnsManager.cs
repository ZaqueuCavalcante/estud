using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Back.Auth.Managers;

public class DnsManager(DnsSettings settings, IHttpClientFactory httpClientFactory, ILogger<DnsManager> logger)
{
    private const int NoError = 0;
    private const int NonExistentDomain = 3;
    private const int TxtRecordType = 16;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Returns null when the lookup itself failed (resolver unreachable, SERVFAIL, ...),
    /// so callers can tell it apart from a name that simply has no TXT records.
    /// </summary>
    public async Task<List<string>?> GetTxtRecords(string name)
    {
        var url = QueryHelpers.AddQueryString(settings.ResolverUrl, new Dictionary<string, string?>
        {
            ["name"] = name,
            ["type"] = "TXT",
        });

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[DnsManager] TXT lookup for {Name} returned HTTP {StatusCode}", name, (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<DohResponse>(JsonOptions);

            if (body?.Status == NonExistentDomain) return [];
            if (body?.Status != NoError)
            {
                logger.LogWarning("[DnsManager] TXT lookup for {Name} returned DNS status {Status}", name, body?.Status);
                return null;
            }

            return (body.Answer ?? [])
                .Where(a => a.Type == TxtRecordType && a.Data != null)
                .Select(a => ParseTxtData(a.Data!))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "[DnsManager] TXT lookup for {Name} failed", name);
            return null;
        }
    }

    /// <summary>
    /// Some resolvers return TXT data raw and others as quoted character-strings,
    /// with records longer than 255 chars split into several of them ("part1" "part2").
    /// </summary>
    private static string ParseTxtData(string data)
    {
        var value = data.Trim();
        if (!value.StartsWith('"')) return value;

        var result = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '\\' && inQuotes && i + 1 < value.Length)
            {
                result.Append(value[++i]);
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes) result.Append(c);
        }

        return result.ToString();
    }

    private class DohResponse
    {
        public int Status { get; set; }
        public List<DohAnswer>? Answer { get; set; }
    }

    private class DohAnswer
    {
        public int Type { get; set; }
        public string? Data { get; set; }
    }
}
