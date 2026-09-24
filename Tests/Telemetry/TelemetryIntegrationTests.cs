using System.Diagnostics;
using Estud.Back.Configs;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Telemetry_GetCourses_Should_trace_request_with_database_queries()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        using var activity = _back.StartTestActivity();

        // Act
        await client.GetCourses();

        // Assert
        var spans = await _back.AwaitSpans(activity.TraceId, x => x.Any(s => s.Source.Name == "Microsoft.AspNetCore"));

        var server = spans.Single(s => s.Source.Name == "Microsoft.AspNetCore");
        server.Kind.Should().Be(ActivityKind.Server);
        server.GetTagItem("http.route").Should().Be("courses");
        server.GetTagItem("http.response.status_code").Should().Be(200);

        spans.Should().Contain(s => s.Source.Name == "Npgsql" && s.ParentSpanId == server.SpanId);
    }

    [Test]
    public async Task Telemetry_CreateTeacher_Should_trace_command_processing_in_request_trace()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        using var activity = _back.StartTestActivity();

        // Act
        await client.CreateTeacher(DataGen.UserName, DataGen.Email);
        await _back.AwaitCommandsProcessing();

        // Assert
        var spans = await _back.AwaitSpans(activity.TraceId, x => x.Any(s => s.Source.Name == OpenTelemetryConfigs.CommandsProcessing));

        var command = spans.Single(s => s.Source.Name == OpenTelemetryConfigs.CommandsProcessing);
        command.Kind.Should().Be(ActivityKind.Consumer);
        command.GetTagItem("command.type").Should().Be("SendInviteEmailCommand");
        command.Status.Should().NotBe(ActivityStatusCode.Error);
    }

    [Test]
    public async Task Telemetry_GetCourses_Should_record_request_duration_metric()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        await client.GetCourses();

        // Assert
        var points = await _back.AwaitMetricPoints("http.server.request.duration", x => x.Any(p => p.HasTag("http.route", "courses")));

        points.Should().Contain(p => p.HasTag("http.route", "courses") && p.HasTag("http.response.status_code", 200));
    }

    #endregion
}
