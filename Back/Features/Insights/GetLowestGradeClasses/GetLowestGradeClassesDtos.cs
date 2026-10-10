namespace Estud.Back.Features.Insights.GetLowestGradeClasses;

public class GetLowestGradeClassWorkDto
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public ClassNoteType NoteType { get; set; }
    public int Weight { get; set; }
    public decimal Note { get; set; }
}
