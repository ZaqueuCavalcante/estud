namespace Estud.Back.Domain.Enums;

/// <summary>
/// Algoritmo usado pela Instituição para calcular a média final de um Aluno numa Turma.
/// A regra também define quais tipos de nota a Instituição usa.
/// </summary>
public enum ClassGradeRule
{
    [Description("Média das duas maiores entre N1, N2 e N3")]
    BestTwoOfThree = 0,

    [Description("Média de N1 e N2")]
    AverageOfTwo = 1,

    [Description("Média de N1, N2 e N3")]
    AverageOfThree = 2,

    /// <summary>
    /// A maior entre a média de N1 e N2 e a nota de N3, que funciona como substitutiva.
    /// </summary>
    [Description("Média de N1 e N2, ou N3")]
    AverageOrThird = 3,
}
