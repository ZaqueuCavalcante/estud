using Estud.Back.Domain.Students;
using Estud.Back.Domain.Institutions;

namespace Estud.Back.Domain.Classes;

/// <summary>
/// Vínculo entre Turma e Aluno
/// </summary>
public class ClassStudent
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public StudentClassStatus Status { get; set; }

    public Class? Class { get; set; }
    public EstudStudent? Student { get; set; }

    private ClassStudent() { }

    public ClassStudent(
        int classId,
        int studentId
    ) {
        ClassId = classId;
        StudentId = studentId;
        Status = StudentClassStatus.Matriculado;
    }

    public void Finalize(decimal average, decimal frequency, InstitutionConfig config)
    {
        if (frequency < config.FrequencyLimit)
            Status = StudentClassStatus.ReprovadoPorFalta;
        else if (average < config.NoteLimit)
            Status = StudentClassStatus.ReprovadoPorNota;
        else
            Status = StudentClassStatus.Aprovado;
    }
}
