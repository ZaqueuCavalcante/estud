namespace Estud.Back.Extensions;

public static class DayExtensions
{
    extension(Day day)
    {
        public static Day[] All => Enum.GetValues<Day>();
    }
}
