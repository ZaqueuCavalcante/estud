namespace Estud.Back.Extensions;

public static class GuidExtensions
{
    extension(Guid guid)
    {
        public int ToHashCode()
        {
            var justNumbers = guid.ToString().OnlyNumbers();
            return int.Parse(justNumbers[^8..]);
        }
    }
}
