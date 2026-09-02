namespace Estud.Back.Features.Students.GetStudentAgenda;

public class GetStudentAgendaDto
{
    public int Id { get; set; }
    public string Discipline { get; set; }
    public Day Day { get; set; }
    public Hour Start { get; set; }
    public Hour End { get; set; }
    public string? Classroom { get; set; }
}
