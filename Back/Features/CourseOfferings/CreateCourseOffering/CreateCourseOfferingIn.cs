namespace Estud.Back.Features.CourseOfferings.CreateCourseOffering;

public class CreateCourseOfferingIn : IApiDto<CreateCourseOfferingIn>
{
    /// <summary>
    /// Id do campus
    /// </summary>
    public int CampusId { get; set; }

    /// <summary>
    /// Id do curso
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Id da grade curricular do curso
    /// </summary>
    public int CourseCurriculumId { get; set; }

    /// <summary>
    /// Id do período acadêmico
    /// </summary>
    public int AcademicPeriodId { get; set; }

    /// <summary>
    /// Turno da oferta de curso
    /// </summary>
    public CourseSession? CourseSession { get; set; }

    public static IEnumerable<(string, CreateCourseOfferingIn)> GetExamples() =>
    [
        ("Exemplo", new() { }),
    ];
}
