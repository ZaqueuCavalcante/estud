using BrazilHolidays.Net;
using System.Globalization;

namespace Estud.Back.Extensions;

public static class DateOnlyExtensions
{
    private const int AdultAge = 18;
    private static readonly DateOnly MinBirthdate = new(1900, 1, 1);

    extension(DateOnly date)
    {
        public bool IsHoliday()
        {
            return date.ToDateTime(TimeOnly.Parse("12:00")).IsHoliday();
        }

        public string FormatBr()
        {
            return date.ToString("dd/MM/yyyy");
        }

        public bool IsValidBirthdate()
        {
            return date >= MinBirthdate && date <= DateOnly.FromDateTime(DateTime.UtcNow);
        }

        public bool IsAdult()
        {
            return date <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-AdultAge);
        }
    }

    extension(string date)
    {
        public DateOnly ToDateOnly()
        {
            return DateOnly.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
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
