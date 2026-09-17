namespace Estud.Back.Domain.Enums;

/// <summary>
/// Grupo de Usuários
/// </summary>
public enum UsersGroup
{
    [Description("Todos")]
    All = 0,

    [Description("Alunos")]
    Students = 1,

    [Description("Professores")]
    Teachers = 2,
}
