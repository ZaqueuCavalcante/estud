namespace Estud.Back.Features.Teachers.GetTeacherClassStudents;

public class GetTeacherClassStudentDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public StudentClassStatus Status { get; set; }
    public int Presences { get; set; }
    public int Absences { get; set; }
}
