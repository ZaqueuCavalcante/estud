namespace Estud.Back.Features.CourseOfferings.CreateCourseOffering;

public class CreateCourseOfferingOut : IApiDto<CreateCourseOfferingOut>
{
    /// <summary>
    /// Id da oferta de curso criada
    /// </summary>
    public int Id { get; set; }

    public static IEnumerable<(string, CreateCourseOfferingOut)> GetExamples() =>
    [
        ("Exemplo", new() { Id = 1 }),
    ];
}
