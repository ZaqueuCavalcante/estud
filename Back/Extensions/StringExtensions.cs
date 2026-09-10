using System.Text;
using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Back.Extensions;

public static class StringExtensions
{
    private static JsonSerializerSettings _settings = new()
    {
        Converters = [new StringEnumConverter()],
    };

    extension(string? text)
    {
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text);
        }

        public bool HasValue()
        {
            return !string.IsNullOrEmpty(text);
        }

        public bool IsIn(params string[] others)
        {
            if (text.IsEmpty())
                return true;

            foreach (var other in others)
            {
                if (other.Contains(text!, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool IsValidPhoneNumber()
        {
            if (text.IsEmpty()) return false;
            return Regex.IsMatch(text!, @"^\d{10,11}$");
        }
    }

    extension(StringValues text)
    {
        public bool HasValue()
        {
            return !string.IsNullOrEmpty(text);
        }
    }

    extension(string value)
    {
        public string OnlyNumbers()
        {
            if (value.HasValue())
            {
                return new string(value.Where(char.IsDigit).ToArray());
            }

            return "";
        }

        public string ToSnakeCase()
        {
            if (value.IsEmpty()) { return ""; }

            var startUnderscores = Regex.Match(value, @"^_+");
            return startUnderscores + Regex.Replace(value, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        }

        public string ToBase64()
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public bool IsValidEmail()
        {
            if (value.IsEmpty()) return false;
            return Regex.IsMatch(value, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
        }

        public string AddQueryString(object obj)
        {
            return QueryHelpers.AddQueryString(value, ConvertObjectToDictionary(obj));
        }

        public string GetSqlSpanName()
        {
            var comparer = StringComparison.InvariantCultureIgnoreCase;
            var insert = value.Contains("INSERT", comparer);
            var update = value.Contains("UPDATE", comparer);
            var delete = value.Contains("DELETE", comparer);
            var select = value.Contains("SELECT", comparer);

            var builder = new StringBuilder();

            if (insert) builder.Append("INSERT ");
            if (update) builder.Append("UPDATE ");
            if (delete) builder.Append("DELETE");

            if (!insert && !update && !delete && select) builder.Append("SELECT");

            return builder.ToString().Trim();
        }

        public string ToNormalizedName()
        {
            var withoutAccents = value.Trim()
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(withoutAccents).Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }
    }

    extension(decimal value)
    {
        public string Format()
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }

    extension(int value)
    {
        public string MinutesToString()
        {
            var hours = value / 60;
            var minutes = value % 60;

            if (hours == 0 && minutes == 0) return "0";
            if (hours == 0) return $"{minutes}min";
            if (minutes == 0) return $"{hours}h";

            return $"{hours}h e {minutes}min";
        }
    }

    extension(object obj)
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(obj, _settings);
        }
    }

    private static Dictionary<string, string?> ConvertObjectToDictionary(object obj)
    {
        if (obj == null) return [];

        Dictionary<string, string?> dictionary = [];
        PropertyInfo[] properties = obj.GetType().GetProperties();

        foreach (PropertyInfo property in properties)
        {
            string propertyName = property.Name;
            object propertyValue = property.GetValue(obj)!;

            if (propertyValue != null)
            {
                var valueAsString = propertyValue.ToString();

                if (property.PropertyType == typeof(DateTime))
                    valueAsString = ((DateTime)propertyValue).ToString("yyyy-MM-ddTHH:mm:sszzz");

                if (property.PropertyType == typeof(DateOnly))
                    valueAsString = ((DateOnly)propertyValue).ToString("yyyy-MM-dd");

                dictionary.Add(propertyName, valueAsString);
            }
        }

        return dictionary;
    }
}
