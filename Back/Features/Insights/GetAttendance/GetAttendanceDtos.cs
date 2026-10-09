namespace Estud.Back.Features.Insights.GetAttendance;

public class GetAttendanceDayDto
{
    public DateOnly Date { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}

public class GetAttendanceClassDto
{
    public int Id { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}
