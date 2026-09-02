namespace Estud.Back.Features.Parents.GetParentStudentAgenda;

public class GetParentStudentAgendaDto
{
    public int Id { get; set; }
    public string Discipline { get; set; }
    public Day Day { get; set; }
    public Hour Start { get; set; }
    public Hour End { get; set; }
    public string? Classroom { get; set; }
}
