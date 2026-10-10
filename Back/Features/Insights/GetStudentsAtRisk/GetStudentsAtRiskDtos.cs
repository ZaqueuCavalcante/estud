namespace Estud.Back.Features.Insights.GetStudentsAtRisk;

public class GetStudentsAtRiskAttendanceDto
{
    public int StudentId { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}

public class GetStudentsAtRiskWorkDto
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public ClassNoteType NoteType { get; set; }
    public int Weight { get; set; }
    public decimal Note { get; set; }
}
