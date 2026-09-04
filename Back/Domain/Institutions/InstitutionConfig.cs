namespace Estud.Back.Domain.Institutions;

public class InstitutionConfig
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }

    /// <summary>
    /// Nota mínima para aprovação na disciplina (de 0 a 10).
    /// </summary>
    public decimal NoteLimit { get; set; }

    /// <summary>
    /// Frequência mínima para aprovação na disciplina (de 0% a 100%).
    /// </summary>
    public decimal FrequencyLimit { get; set; }

    /// <summary>
    /// Algoritmo usado para calcular a média final dos alunos nas turmas da instituição.
    /// </summary>
    public ClassGradeRule GradeRule { get; set; }

    public const decimal DefaultNoteLimit = 7.00M;
    public const decimal DefaultFrequencyLimit = 70.00M;
    public const ClassGradeRule DefaultGradeRule = ClassGradeRule.BestTwoOfThree;

    public InstitutionConfig()
    {
        NoteLimit = DefaultNoteLimit;
        FrequencyLimit = DefaultFrequencyLimit;
        GradeRule = DefaultGradeRule;
    }

    public InstitutionConfig(int institutionId) : this()
    {
        InstitutionId = institutionId;
    }

    public void Setup(decimal noteLimit, decimal frequencyLimit)
    {
        NoteLimit = noteLimit;
        FrequencyLimit = frequencyLimit;
    }
}
