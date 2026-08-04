namespace Estud.Back.Errors;

public class CampusNotFound : EstudError
{
    public static readonly CampusNotFound I = new();
    public override string Code { get; set; } = nameof(CampusNotFound);
    public override string Message { get; set; } = "Campus não encontrado.";
}

public class InvalidCampusName : EstudError
{
    public static readonly InvalidCampusName I = new();
    public override string Code { get; set; } = nameof(InvalidCampusName);
    public override string Message { get; set; } = "Nome de campus inválido.";
}

public class InvalidCampusCity : EstudError
{
    public static readonly InvalidCampusCity I = new();
    public override string Code { get; set; } = nameof(InvalidCampusCity);
    public override string Message { get; set; } = "Cidade do campus inválida.";
}

public class InvalidBrazilState : EstudError
{
    public static readonly InvalidBrazilState I = new();
    public override string Code { get; set; } = nameof(InvalidBrazilState);
    public override string Message { get; set; } = "Estado inválido.";
}

public class InvalidCampusList : EstudError
{
    public static readonly InvalidCampusList I = new();
    public override string Code { get; set; } = nameof(InvalidCampusList);
    public override string Message { get; set; } = "Lista de campus inválida.";
}

public class InvalidOpeningHour : EstudError
{
    public static readonly InvalidOpeningHour I = new();
    public override string Code { get; set; } = nameof(InvalidOpeningHour);
    public override string Message { get; set; } = "Horário de funcionamento inválido.";
}

public class OverlappingOpeningHours : EstudError
{
    public static readonly OverlappingOpeningHours I = new();
    public override string Code { get; set; } = nameof(OverlappingOpeningHours);
    public override string Message { get; set; } = "Há horários de funcionamento sobrepostos no mesmo dia.";
}

public class InvalidOpeningHoursList : EstudError
{
    public static readonly InvalidOpeningHoursList I = new();
    public override string Code { get; set; } = nameof(InvalidOpeningHoursList);
    public override string Message { get; set; } = "Lista de horários de funcionamento inválida.";
}
