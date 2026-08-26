namespace Estud.Back.Features.Classes.GetClass;

public class GetClassStudentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public StudentClassStatus Status { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}
