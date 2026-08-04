namespace Estud.Back.Features.Classrooms.UpdateClassroom;

public class UpdateClassroomIn : IApiDto<UpdateClassroomIn>
{
    public string Name { get; set; }
    public int Capacity { get; set; }

    public static IEnumerable<(string, UpdateClassroomIn)> GetExamples() =>
    [
        ("Sala 05",
        new UpdateClassroomIn
        {
            Name = "Sala 05",
            Capacity = 40,
        }),
        ("Laboratório de Química",
        new UpdateClassroomIn
        {
            Name = "Laboratório de Química",
            Capacity = 35,
        }),
    ];
}
