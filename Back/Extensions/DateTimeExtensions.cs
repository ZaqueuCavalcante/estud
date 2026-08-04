namespace Estud.Back.Extensions;

public static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        public string Format()
        {
            return dateTime.ToString("dd/MM/yyy HH:mm");
        }
    }

    extension(DateOnly date)
    {
        public DateTime ToDateTime()
        {
            return date.ToDateTime(TimeOnly.MinValue);
        }
    }
}
