namespace Estud.Back.Extensions;

public static class HourExtensions
{
    extension(Hour hour)
    {
        // O valor do enum Hour é HHMM (ex: H07_30 = 730), então hora e minuto saem
        // da divisão e do resto por 100.
        public int ToMinutes()
        {
            var value = (int)hour;
            return value / 100 * 60 + value % 100;
        }
    }
}
