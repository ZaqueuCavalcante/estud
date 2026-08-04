namespace Estud.Back.Domain.Campi;

public class Campus
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string Name { get; set; }
    public BrazilState State { get; set; }
    public string City { get; set; }

    public List<OpeningHour> OpeningHours { get; set; } = [];

    private Campus() { }

    public Campus(int institutionId, string name, BrazilState state, string city)
    {
        InstitutionId = institutionId;
        Name = name;
        State = state;
        City = city;
        OpeningHours = DefaultOpeningHours();
    }

    public static List<OpeningHour> DefaultOpeningHours() =>
    [
        new(Day.Monday, Hour.H07_00, Hour.H22_00),
        new(Day.Tuesday, Hour.H07_00, Hour.H22_00),
        new(Day.Wednesday, Hour.H07_00, Hour.H22_00),
        new(Day.Thursday, Hour.H07_00, Hour.H22_00),
        new(Day.Friday, Hour.H07_00, Hour.H22_00),
    ];

    public void Update(string name, BrazilState state, string city)
    {
        Name = name;
        State = state;
        City = city;
    }
}
