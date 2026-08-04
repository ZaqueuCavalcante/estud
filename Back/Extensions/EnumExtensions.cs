namespace Estud.Back.Extensions;

public static class EnumExtensions
{
    extension(Enum value)
    {
        public string GetDescription()
        {
            if (value == null)
            {
                return string.Empty;
            }

            var attribute = value.GetType()
                .GetField(value.ToString())!
                .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false);

            if (attribute is DescriptionAttribute[] source && source.Length != 0)
            {
                return source.First().Description;
            }

            return value.ToString();
        }

        public bool IsIn(params Enum[] valuesToCheck)
        {
            if (valuesToCheck == null || valuesToCheck.Length == 0)
            {
                return false;
            }

            return valuesToCheck.Contains(value);
        }

        public bool IsValid()
        {
            return Enum.IsDefined(value.GetType(), value);
        }
    }

    extension(string value)
    {
        public T ToEnum<T>()
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }

    extension(short value)
    {
        public T ToEnum<T>()
        {
            return (T)Enum.Parse(typeof(T), value.ToString(), true);
        }
    }

    extension(int value)
    {
        public T IntToEnum<T>()
        {
            return (T)Enum.Parse(typeof(T), value.ToString(), true);
        }
    }

    extension<TEnum>(TEnum enumValue) where TEnum : Enum
    {
        public int ToInt()
        {
            return Convert.ToInt32(enumValue);
        }

        public short ToShort()
        {
            return (short) enumValue.ToInt();
        }
    }

    extension(DayOfWeek dayOfWeek)
    {
        public bool Is(Day day)
        {
            return day.ToString() == dayOfWeek.ToString();
        }
    }

    extension(Hour hourA)
    {
        public int DiffInMinutes(Hour hourB)
        {
            var (first, second) = hourA > hourB ? (hourB, hourA) : (hourA, hourB);

            var firstHour = int.Parse(first.ToString().Substring(1, 2));
            var firstMinute = int.Parse(first.ToString().Substring(4, 2));

            var secondHour = int.Parse(second.ToString().Substring(1, 2));
            var secondMinute = int.Parse(second.ToString().Substring(4, 2));

            var hours = secondHour - firstHour;
            var minutes = secondMinute - firstMinute;

            return hours * 60 + minutes;
        }
    }
}
