using BrazilHolidays.Net;

namespace Estud.Back.Extensions;

public static class DateOnlyExtensions
{
    private static readonly DateOnly MinBirthdate = new(1900, 1, 1);

    extension(DateOnly date)
    {
        public bool IsHoliday()
        {
            return date.ToDateTime(TimeOnly.Parse("12:00")).IsHoliday();
        }

        public bool IsValidBirthdate()
        {
            return date >= MinBirthdate && date <= DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }

    extension(DateTime dateTime)
    {
        public DateOnly ToDateOnly()
        {
            return DateOnly.FromDateTime(dateTime);
        }
    }
}
