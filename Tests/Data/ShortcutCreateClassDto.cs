namespace Estud.Tests.Data;

public class ShortcutCreateClassDto
{
    public int Id { get; set; }
    public string TeacherEmail { get; set; }
    public string StudentEmail { get; set; }
    public List<int> StudentIds { get; set; } = [];
}
