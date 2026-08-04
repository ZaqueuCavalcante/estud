namespace Estud.Back.Shared;

public static class ShiftExtensions
{
    // As janelas ficam em minutos a partir da meia-noite (e não em Hour) porque
    // a noite termina às 24h, que não existe no enum Hour.
    extension(Shift shift)
    {
        public int StartInMinutes => shift switch
        {
            Shift.Morning => 07 * 60,
            Shift.Afternoon => 12 * 60,
            _ => 18 * 60,
        };

        public int EndInMinutes => shift switch
        {
            Shift.Morning => 12 * 60,
            Shift.Afternoon => 18 * 60,
            _ => 24 * 60,
        };

        public int DurationInMinutes => shift.EndInMinutes - shift.StartInMinutes;
    }
}
