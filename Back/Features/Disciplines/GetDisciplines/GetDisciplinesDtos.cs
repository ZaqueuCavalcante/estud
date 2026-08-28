namespace Estud.Back.Features.Disciplines.GetDisciplines;

public class DisciplineRow
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public bool HasCourses { get; set; }
    public bool HasTeachers { get; set; }
    public int TotalRows { get; set; }
}
