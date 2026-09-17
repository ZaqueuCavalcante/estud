namespace Estud.Back.Domain.Enums;

/// <summary>
/// Tipo do Curso
/// </summary>
public enum CourseType
{
    [Description("Bacharelado")]
    Bacharelado = 0,

    [Description("Licenciatura")]
    Licenciatura = 1,

    [Description("Tecnólogo")]
    Tecnologo = 2,

    [Description("Especialização")]
    Especializacao = 3,

    [Description("Mestrado")]
    Mestrado = 4,

    [Description("Doutorado")]
    Doutorado = 5,

    [Description("Pós-Doutorado")]
    PosDoutorado = 6,
}
