namespace Estud.Back.Extensions;

public static class ShiftExtensions
{
    extension(Shift shift)
    {
        public Hour StartAtHour => shift switch
        {
            Shift.Morning => Hour.H07_00,
            Shift.Afternoon => Hour.H12_00,
            _ => Hour.H18_00,
        };

        public Hour EndAtHour => shift switch
        {
            Shift.Morning => Hour.H12_00,
            Shift.Afternoon => Hour.H18_00,
            _ => Hour.H23_00,
        };

        public int StartInMinutes => shift switch
        {
            Shift.Morning => shift.StartAtHour.ToMinutes(),
            Shift.Afternoon => shift.StartAtHour.ToMinutes(),
            _ => shift.StartAtHour.ToMinutes(),
        };

        public int EndInMinutes => shift switch
        {
            Shift.Morning => shift.EndAtHour.ToMinutes(),
            Shift.Afternoon => shift.EndAtHour.ToMinutes(),
            _ => shift.EndAtHour.ToMinutes(),
        };
    }
}
