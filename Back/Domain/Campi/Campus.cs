using Estud.Back.Domain.Classrooms;

namespace Estud.Back.Domain.Campi;

public class Campus
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string Name { get; set; }
    public BrazilState State { get; set; }
    public string City { get; set; }

    public List<Classroom> Classrooms { get; set; } = [];
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

    /// <summary>
    /// Minutos abertos na interseção do dia com a janela do turno. <br/>
    /// Dia sem nenhuma janela fica fechado — é a leitura literal de "ausência de linha
    /// significa fechado". As janelas de um mesmo dia nunca se sobrepõem (garantido no
    /// update das janelas), então somá-las não conta minuto em dobro.
    /// </summary>
    public int MinutesOpenIn(Day day, Shift shift)
    {
        var shiftStart = shift.StartInMinutes;
        var shiftEnd = shift.EndInMinutes;

        // Minutos de interseção entre [janela) e [turno), ambos em minutos a partir
        // da meia-noite. Janela e turno disjuntos dão 0.
        return OpeningHours
            .Where(h => h.Day == day)
            .Sum(h => Math.Max(
                Math.Min(h.End.ToMinutes(), shiftEnd) - Math.Max(h.Start.ToMinutes(), shiftStart), 0));
    }
}
