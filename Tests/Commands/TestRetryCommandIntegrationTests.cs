using Estud.Back.Domain.Commands;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Commands_TestRetryCommand_Should_succeed_on_first_attempt_without_creating_retries()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(failUntilAttempt: 1, maxRetries: 3);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 1);

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        var command = commands.Single();
        command.Status.Should().Be(CommandStatus.Success);
        command.Error.Should().BeNull();
        command.RetryAttempt.Should().Be(0);
    }

    [Test]
    public async Task Commands_TestRetryCommand_Should_not_retry_when_max_retries_is_zero()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(failUntilAttempt: 2, maxRetries: 0);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 1);

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        var command = commands.Single();
        command.Status.Should().Be(CommandStatus.Error);
        command.Error.Should().Contain("failed on attempt 1");
    }

    [Test]
    public async Task Commands_TestRetryCommand_Should_retry_until_success()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(failUntilAttempt: 3, maxRetries: 5);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 3);

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        commands.Should().HaveCount(3);
        commands.Select(x => x.Status).Should().Equal(CommandStatus.Error, CommandStatus.Error, CommandStatus.Success);
        commands.Select(x => x.RetryAttempt).Should().Equal(0, 1, 2);
        commands.Select(x => x.MaxRetries).Should().Equal(5, 4, 3);
        commands.Select(x => x.OriginalId).Should().Equal(null, commandId, commandId);
        commands.Should().AllSatisfy(x => x.NotBefore.Should().BeNull());

        commands[0].Error.Should().Contain("failed on attempt 1");
        commands[1].Error.Should().Contain("failed on attempt 2");
        commands[2].Error.Should().BeNull();
    }

    [Test]
    public async Task Commands_TestRetryCommand_Should_succeed_on_last_available_retry()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(failUntilAttempt: 3, maxRetries: 2);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 3);

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        commands.Should().HaveCount(3);
        commands.Select(x => x.Status).Should().Equal(CommandStatus.Error, CommandStatus.Error, CommandStatus.Success);
        commands.Last().MaxRetries.Should().Be(0);
    }

    [Test]
    public async Task Commands_TestRetryCommand_Should_stop_retrying_when_max_retries_is_exhausted()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(failUntilAttempt: 10, maxRetries: 2);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 3);
        await _back.AwaitCommandsProcessing();

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        commands.Should().HaveCount(3);
        commands.Should().AllSatisfy(x => x.Status.Should().Be(CommandStatus.Error));
        commands.Select(x => x.RetryAttempt).Should().Equal(0, 1, 2);
        commands.Select(x => x.MaxRetries).Should().Equal(2, 1, 0);
        commands.Last().Error.Should().Contain("failed on attempt 3");
    }

    [Test]
    public async Task Commands_TestRetryCommand_Should_not_process_retry_before_backoff_delay()
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(
            failUntilAttempt: 2, maxRetries: 1, backoffStrategy: BackoffStrategy.Fixed, baseDelaySeconds: 5);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 1);
        await _back.AwaitCommandsProcessing();

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        commands.Should().HaveCount(2);

        var original = commands[0];
        original.Status.Should().Be(CommandStatus.Error);

        var retry = commands[1];
        retry.Status.Should().Be(CommandStatus.Pending);
        retry.ProcessedAt.Should().BeNull();
        retry.NotBefore.Should().BeCloseTo(original.ProcessedAt!.Value.AddSeconds(5), TimeSpan.FromSeconds(1));
    }

    [Test]
    [TestCase(BackoffStrategy.Exponential, new[] { 1, 2, 4 })]
    [TestCase(BackoffStrategy.Linear, new[] { 1, 2, 3 })]
    [TestCase(BackoffStrategy.Fixed, new[] { 1, 1, 1 })]
    public async Task Commands_TestRetryCommand_Should_apply_backoff_delay_between_retries(BackoffStrategy strategy, int[] expectedDelays)
    {
        // Arrange
        var commandId = await _back.ShortcutEnqueueTestRetryCommand(
            failUntilAttempt: 4, maxRetries: 3, backoffStrategy: strategy, baseDelaySeconds: 1);

        // Act
        await AwaitTestRetryCommandChain(commandId, processedCount: 4);

        // Assert
        var commands = await GetTestRetryCommandChain(commandId);

        commands.Should().HaveCount(4);
        commands.Select(x => x.Status).Should().Equal(
            CommandStatus.Error, CommandStatus.Error, CommandStatus.Error, CommandStatus.Success);
        commands.Should().AllSatisfy(x =>
        {
            x.BackoffStrategy.Should().Be(strategy);
            x.BaseDelaySeconds.Should().Be(1);
        });

        for (var i = 1; i < commands.Count; i++)
        {
            var failed = commands[i - 1];
            var retry = commands[i];

            retry.NotBefore.Should().BeCloseTo(failed.ProcessedAt!.Value.AddSeconds(expectedDelays[i - 1]), TimeSpan.FromSeconds(1));
            retry.ProcessedAt.Should().BeOnOrAfter(retry.NotBefore!.Value);
        }
    }

    #endregion

    private async Task AwaitTestRetryCommandChain(int commandId, int processedCount)
    {
        await using var ctx = _back.GetDbContext();
        var scheduler = await _back.GetSchedulerFactory().GetScheduler();

        for (var i = 0; i < 100; i++)
        {
            var processed = await ctx.Commands.AsNoTracking()
                .CountAsync(x => (x.Id == commandId || x.OriginalId == commandId) && x.ProcessedAt != null);
            if (processed >= processedCount) return;

            await scheduler.TriggerCommandsProcessorJob();
            await Task.Delay(200);
        }
    }

    private async Task<List<Command>> GetTestRetryCommandChain(int commandId)
    {
        await using var ctx = _back.GetDbContext();

        return await ctx.Commands.AsNoTracking()
            .Where(x => x.Id == commandId || x.OriginalId == commandId)
            .OrderBy(x => x.RetryAttempt)
            .ToListAsync();
    }
}
