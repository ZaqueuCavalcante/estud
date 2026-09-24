namespace Estud.Back.Domain.Classes;

/// <summary>
/// Vínculo entre Turma e Professor
/// </summary>
public class ClassTeacher
{
    public int ClassId { get; set; }
    public int TeacherId { get; set; }
}
