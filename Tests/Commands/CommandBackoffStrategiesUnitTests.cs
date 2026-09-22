namespace Estud.Tests.Commands;

public class CommandBackoffStrategiesUnitTests
{
    [Test]
    [TestCase(0, 1)]
    [TestCase(10, 1)]
    [TestCase(10, 5)]
    [TestCase(60, 100)]
    public void CommandBackoffStrategies_None_should_return_null(int baseDelaySeconds, int retryAttempt)
    {
        // Arrange / Act
        var result = CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.None, baseDelaySeconds, retryAttempt);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [TestCase(10, 1, 10)]
    [TestCase(10, 2, 20)]
    [TestCase(10, 3, 40)]
    [TestCase(10, 4, 80)]
    [TestCase(10, 5, 160)]
    [TestCase(1, 1, 1)]
    [TestCase(1, 11, 1024)]
    [TestCase(5, 3, 20)]
    [TestCase(30, 6, 960)]
    [TestCase(0, 1, 0)]
    [TestCase(0, 10, 0)]
    public void CommandBackoffStrategies_Exponential_should_double_delay_on_each_retry(int baseDelaySeconds, int retryAttempt, int expected)
    {
        // Arrange / Act
        var result = CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Exponential, baseDelaySeconds, retryAttempt);

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase(10, 1, 10)]
    [TestCase(10, 2, 20)]
    [TestCase(10, 3, 30)]
    [TestCase(10, 4, 40)]
    [TestCase(10, 5, 50)]
    [TestCase(1, 1, 1)]
    [TestCase(1, 100, 100)]
    [TestCase(7, 3, 21)]
    [TestCase(60, 10, 600)]
    [TestCase(0, 1, 0)]
    [TestCase(0, 10, 0)]
    public void CommandBackoffStrategies_Linear_should_multiply_delay_by_retry_attempt(int baseDelaySeconds, int retryAttempt, int expected)
    {
        // Arrange / Act
        var result = CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Linear, baseDelaySeconds, retryAttempt);

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase(10, 1, 10)]
    [TestCase(10, 2, 10)]
    [TestCase(10, 5, 10)]
    [TestCase(10, 100, 10)]
    [TestCase(1, 3, 1)]
    [TestCase(60, 7, 60)]
    [TestCase(0, 1, 0)]
    [TestCase(0, 10, 0)]
    public void CommandBackoffStrategies_Fixed_should_always_return_base_delay(int baseDelaySeconds, int retryAttempt, int expected)
    {
        // Arrange / Act
        var result = CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Fixed, baseDelaySeconds, retryAttempt);

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(999)]
    public void CommandBackoffStrategies_Unknown_strategy_should_return_null(int strategy)
    {
        // Arrange / Act
        var result = CommandBackoffStrategies.GetDelaySeconds((BackoffStrategy)strategy, 10, 1);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void CommandBackoffStrategies_Should_produce_expected_sequence_for_each_strategy()
    {
        // Arrange
        const int baseDelay = 10;
        var attempts = Enumerable.Range(1, 5).ToList();

        // Act
        var none = attempts.Select(a => CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.None, baseDelay, a)).ToList();
        var exponential = attempts.Select(a => CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Exponential, baseDelay, a)).ToList();
        var linear = attempts.Select(a => CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Linear, baseDelay, a)).ToList();
        var fixedDelays = attempts.Select(a => CommandBackoffStrategies.GetDelaySeconds(BackoffStrategy.Fixed, baseDelay, a)).ToList();

        // Assert
        none.Should().AllSatisfy(x => x.Should().BeNull());
        exponential.Should().Equal(10, 20, 40, 80, 160);
        linear.Should().Equal(10, 20, 30, 40, 50);
        fixedDelays.Should().Equal(10, 10, 10, 10, 10);
    }
}
