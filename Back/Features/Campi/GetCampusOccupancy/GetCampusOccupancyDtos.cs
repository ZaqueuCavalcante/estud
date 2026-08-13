namespace Estud.Back.Features.Campi.GetCampusOccupancy;

public class GetClassStudentsDto
{
    public int Id { get; set; }
    public int Students { get; set; }
}

public class ClassroomTotals
{
    public int UsedMinutes { get; set; }
    public int UsedCapacity { get; set; }
    public int AvailableMinutes { get; set; }
}
