namespace Estud.Tests;

[SetUpFixture]
public class RequestsCounterReport
{
    [OneTimeTearDown]
    public async Task Report()
    {
        var counts = RequestsCounterMiddleware.Counts.OrderBy(x => x.Key).ToList();
        if (counts.Count == 0) return;

        var lines = counts.Select(x => $"{x.Key}: {x.Value} requests").ToList();
        var total = counts.Sum(x => x.Value);
        lines.Add($"Total: {total} requests");

        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"api-requests-{total}.log");
        await File.WriteAllLinesAsync(path, lines);
    }
}
