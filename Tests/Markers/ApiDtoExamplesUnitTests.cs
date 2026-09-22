using System.Reflection;
using System.Collections;
using Estud.Back.Markers;

namespace Estud.Tests.Markers;

public class ApiDtoExamplesUnitTests
{
    [Test]
    public void ApiDto_All_dtos_should_provide_examples()
    {
        // Arrange
        var dtoTypes = typeof(IApiDto<>).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IApiDto<>)))
            .ToList();

        // Act / Assert
        dtoTypes.Should().NotBeEmpty();

        foreach (var type in dtoTypes)
        {
            var method = type.GetMethod(nameof(IApiDto<>.GetExamples), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            method.Should().NotBeNull($"{type.FullName} should implement GetExamples()");

            var examples = ((IEnumerable)method!.Invoke(null, null)!).Cast<object>().ToList();
            examples.Should().NotBeEmpty($"{type.FullName} should provide at least one example");
        }
    }
}
