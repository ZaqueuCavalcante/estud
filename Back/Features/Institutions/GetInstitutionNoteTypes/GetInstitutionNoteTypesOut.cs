namespace Estud.Back.Features.Institutions.GetInstitutionNoteTypes;

public class GetInstitutionNoteTypesOut : IApiDto<GetInstitutionNoteTypesOut>
{
    /// <summary>
    /// Tipos de nota usados pela instituição
    /// </summary>
    public List<ClassNoteType> NoteTypes { get; set; } = [];

    public static IEnumerable<(string, GetInstitutionNoteTypesOut)> GetExamples() =>
    [
        ("Exemplo",
        new GetInstitutionNoteTypesOut
        {
            NoteTypes = [ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3],
        }),
    ];
}
