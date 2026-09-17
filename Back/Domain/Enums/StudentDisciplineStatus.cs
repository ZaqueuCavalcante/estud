namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma Disciplina da grade em relação ao Aluno
/// </summary>
public enum StudentDisciplineStatus
{
    /// <summary>
    /// O aluno ainda não cursou a disciplina (vai cursar).
    /// </summary>
    [Description("Não cursada")]
    NaoCursada = 0,

    /// <summary>
    /// O aluno está cursando a disciplina atualmente.
    /// </summary>
    [Description("Cursando")]
    Cursando = 1,

    /// <summary>
    /// O aluno já cursou e foi aprovado na disciplina.
    /// </summary>
    [Description("Aprovada")]
    Aprovada = 2,

    /// <summary>
    /// O aluno foi dispensado da disciplina.
    /// </summary>
    [Description("Dispensada")]
    Dispensada = 3,

    /// <summary>
    /// O aluno já cursou e foi reprovado na disciplina (por nota ou por falta).
    /// </summary>
    [Description("Reprovada")]
    Reprovada = 4,
}
