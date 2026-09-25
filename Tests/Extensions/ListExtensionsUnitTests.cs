namespace Estud.Tests.Extensions;

public class ListExtensionsUnitTests
{
    private static readonly List<int> _ids = [10, 20, 30, 40, 50];

    #region True

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_empty_list()
    {
        // Arrange
        List<int> ids = [];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_both_lists_are_empty()
    {
        // Arrange
        List<int> ids = [];

        // Act
        var result = ids.IsSubsetOf([]);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    [TestCase(10)]
    [TestCase(30)]
    [TestCase(50)]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_one_item_list(int id)
    {
        // Arrange
        List<int> ids = [id];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_two_item_list()
    {
        // Arrange
        List<int> ids = [20, 40];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_all_items_list()
    {
        // Arrange
        List<int> ids = [10, 20, 30, 40, 50];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_all_items_in_different_order()
    {
        // Arrange
        List<int> ids = [50, 30, 10, 40, 20];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_others_has_duplicates()
    {
        // Arrange
        List<int> ids = [10, 20];

        // Act
        var result = ids.IsSubsetOf([10, 10, 20, 20]);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_true_when_zero_and_negative_ids_are_in_others()
    {
        // Arrange
        List<int> ids = [0, -1];

        // Act
        var result = ids.IsSubsetOf([-1, 0, 1]);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_not_change_the_lists()
    {
        // Arrange
        List<int> ids = [20, 10];
        List<int> others = [10, 20, 30];

        // Act
        ids.IsSubsetOf(others);

        // Assert
        ids.Should().Equal(20, 10);
        others.Should().Equal(10, 20, 30);
    }

    #endregion

    #region False

    [Test]
    [TestCase(0)]
    [TestCase(-10)]
    [TestCase(11)]
    [TestCase(60)]
    [TestCase(int.MaxValue)]
    [TestCase(int.MinValue)]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_item_is_out(int id)
    {
        // Arrange
        List<int> ids = [id];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_others_is_empty()
    {
        // Arrange
        List<int> ids = [10];

        // Act
        var result = ids.IsSubsetOf([]);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    [TestCase(99, 20, 30)]
    [TestCase(10, 99, 30)]
    [TestCase(10, 20, 99)]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_has_one_out(int first, int second, int third)
    {
        // Arrange
        List<int> ids = [first, second, third];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_duplicated()
    {
        // Arrange
        List<int> ids = [20, 20];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_triplicated()
    {
        // Arrange
        List<int> ids = [20, 20, 20];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_duplicated_not_adjacent()
    {
        // Arrange
        List<int> ids = [10, 20, 30, 10];

        // Act
        var result = ids.IsSubsetOf(_ids);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ListExtensions_IsSubsetOf_Should_return_false_when_duplicated_even_if_others_has_duplicates()
    {
        // Arrange
        List<int> ids = [10, 10];

        // Act
        var result = ids.IsSubsetOf([10, 10]);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
