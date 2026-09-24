namespace Estud.Tests;

[SetUpFixture]
public class RequestsCounterReport
{
    [OneTimeTearDown]
    public async Task Report()
    {
        var byStatusCode = RequestsCounterMiddleware.ByStatusCode.OrderBy(x => x.Key).ToList();
        if (byStatusCode.Count == 0) return;

        var byMethod = RequestsCounterMiddleware.ByMethod.OrderByDescending(x => x.Value).ToList();
        var total = byStatusCode.Sum(x => x.Value);

        List<string> lines =
        [
            "http_response : requests",
            .. byStatusCode.Select(x => $"{x.Key}: {x.Value} requests"),
            "",
            "http_method : requests",
            .. byMethod.Select(x => $"{x.Key}: {x.Value} requests"),
            "",
            $"Total: {total} requests",
        ];

        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"api-requests-{total}.log");
        await File.WriteAllLinesAsync(path, lines);
    }
}
