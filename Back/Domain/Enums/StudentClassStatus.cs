namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status do Aluno em uma Turma
/// </summary>
public enum StudentClassStatus
{
    [Description("Pendente")]
    Pendente = 0,

    [Description("Matriculado")]
    Matriculado = 1,

    [Description("Aprovado")]
    Aprovado = 2,

    [Description("Dispensado")]
    Dispensado = 3,

    [Description("Reprovado por nota")]
    ReprovadoPorNota = 4,

    [Description("Reprovado por falta")]
    ReprovadoPorFalta = 5,
}
