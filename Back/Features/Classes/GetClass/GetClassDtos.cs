namespace Estud.Back.Features.Classes.GetClass;

public class GetClassStudentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public StudentClassStatus Status { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}

public class GetClassStudentWorkDto
{
    public int Id { get; set; }
    public ClassNoteType NoteType { get; set; }
    public int Weight { get; set; }
    public decimal Note { get; set; }
}
