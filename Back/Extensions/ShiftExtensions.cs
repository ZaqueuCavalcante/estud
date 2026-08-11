using Estud.Back.Domain.Classes;

namespace Estud.Back.Extensions;

public static class ShiftExtensions
{
    extension(Shift shift)
    {
        public Schedule ToSchedule(Day day) => Schedule.Window(day, shift.StartAtHour, shift.EndAtHour);

        public Hour StartAtHour => shift switch
        {
            Shift.Morning => Hour.H06_00,
            Shift.Afternoon => Hour.H12_00,
            _ => Hour.H18_00,
        };

        public Hour EndAtHour => shift switch
        {
            Shift.Morning => Hour.H12_00,
            Shift.Afternoon => Hour.H18_00,
            _ => Hour.H24_00,
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
