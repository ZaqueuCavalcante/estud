namespace Estud.Back.Features.Students.GetStudentDetails;

public class GetStudentClassAttendanceDto
{
    public int ClassId { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}

public class GetStudentClassWorkDto
{
    public int ClassId { get; set; }
    public ClassNoteType NoteType { get; set; }
    public int Weight { get; set; }
    public decimal Note { get; set; }
}
