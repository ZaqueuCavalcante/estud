using System.Diagnostics;
using OpenTelemetry.Metrics;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Estud.Tests.Base;

public static class BackFactoryTelemetry
{
    public static readonly ActivitySource TestsActivitySource = new("Estud.Tests");

    private static readonly ConcurrentDictionary<ActivityTraceId, byte> _testTraces = new();

    public static bool IsTestTrace(Activity activity) => _testTraces.ContainsKey(activity.TraceId);

    public static Activity StartTestActivity(this BackFactory _, [CallerMemberName] string name = "")
    {
        var activity = TestsActivitySource.StartActivity(name)!;
        _testTraces.TryAdd(activity.TraceId, 0);
        return activity;
    }

    public static async Task<List<Activity>> AwaitSpans(this BackFactory factory, ActivityTraceId traceId, Func<List<Activity>, bool> until)
    {
        var spans = new List<Activity>();

        var count = 0;
        while (true)
        {
            if (count == 25) break;

            spans = factory.Spans.Snapshot().Where(x => x.TraceId == traceId).ToList();
            if (until(spans)) break;
            await Task.Delay(200);
            count ++;
        }

        return spans;
    }

    public static async Task<List<MetricPoint>> AwaitMetricPoints(this BackFactory factory, string name, Func<List<MetricPoint>, bool> until)
    {
        var meterProvider = factory.Services.GetRequiredService<MeterProvider>();
        var points = new List<MetricPoint>();

        var count = 0;
        while (true)
        {
            if (count == 25) break;

            meterProvider.ForceFlush();
            points = factory.Metrics.Snapshot().LastOrDefault(x => x.Name == name)?.MetricPoints.ToList() ?? [];
            if (until(points)) break;
            await Task.Delay(200);
            count ++;
        }

        return points;
    }

    public static bool HasTag(this MetricPoint point, string key, object value)
    {
        foreach (var tag in point.Tags)
        {
            if (tag.Key == key && Equals(tag.Value, value)) return true;
        }

        return false;
    }
}

public class TelemetryCollection<T>(Func<T, bool>? filter = null) : Collection<T>
{
    private readonly Lock _lock = new();

    protected override void InsertItem(int index, T item)
    {
        if (filter != null && !filter(item)) return;

        lock (_lock) base.InsertItem(index, item);
    }

    public List<T> Snapshot()
    {
        lock (_lock) return [.. this];
    }
}
