namespace Estud.Back.Domain.Classes;

public static class AttendanceRate
{
    public static decimal Of(int presences, int total)
    {
        return total > 0
            ? Math.Round((decimal)presences / total * 100, 1, MidpointRounding.AwayFromZero)
            : 0;
    }
}
