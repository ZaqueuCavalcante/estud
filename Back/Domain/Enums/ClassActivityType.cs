namespace Estud.Back.Domain.Enums;

/// <summary>
/// Tipo de Atividade
/// </summary>
public enum ClassActivityType
{
    [Description("Prova")]
    Exam = 0,

    [Description("Projeto")]
    Project = 1,
    
    [Description("Trabalho")]
    Work = 2,

    [Description("Apresentação")]
    Presentation = 3,
}
