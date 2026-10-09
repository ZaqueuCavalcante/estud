namespace Estud.Back.Features.Insights.GetLowestAttendanceClasses;

public class GetLowestAttendanceClassDto
{
    public int Id { get; set; }
    public string Discipline { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}
