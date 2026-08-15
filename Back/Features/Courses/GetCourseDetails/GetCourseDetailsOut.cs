namespace Estud.Back.Features.Courses.GetCourseDetails;

public class GetCourseDetailsOut : IApiDto<GetCourseDetailsOut>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public CourseType TypeValue { get; set; }

    /// <summary>
    /// Quantidade de alunos com vínculo ativo em alguma oferta do curso
    /// </summary>
    public int Students { get; set; }

    public List<GetCourseDetailsDisciplineOut> Disciplines { get; set; } = [];
    public List<GetCourseDetailsCurriculumOut> Curriculums { get; set; } = [];
    public List<GetCourseDetailsOfferingOut> Offerings { get; set; } = [];

    public static IEnumerable<(string, GetCourseDetailsOut)> GetExamples() =>
    [
        ("Exemplo", new GetCourseDetailsOut
        {
            Id = 1,
            Name = "Análise e Desenvolvimento de Sistemas",
            Type = "Tecnólogo",
            TypeValue = CourseType.Tecnologo,
            Students = 32,
            Disciplines = [new GetCourseDetailsDisciplineOut { Id = 1, Name = "Cálculo I", Code = "A1B2C3D4" }],
            Curriculums = [new GetCourseDetailsCurriculumOut { Id = 1, Name = "Grade ADS 2024", Disciplines = 30 }],
            Offerings =
            [
                new GetCourseDetailsOfferingOut
                {
                    Id = 1,
                    Campus = "Campus Maceió",
                    Curriculum = "Grade ADS 2024",
                    Period = "2026.1",
                    Session = CourseSession.Evening,
                    Students = 32,
                },
            ],
        }),
    ];
}

public class GetCourseDetailsDisciplineOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}

public class GetCourseDetailsCurriculumOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Disciplines { get; set; }
}

public class GetCourseDetailsOfferingOut
{
    public int Id { get; set; }
    public string Campus { get; set; }
    public string Curriculum { get; set; }
    public string Period { get; set; }
    public CourseSession Session { get; set; }
    public int Students { get; set; }
}
