namespace Estud.Back.Features.Campi.GetCampusOccupancy;

public class GetCampusOccupancyOut : IApiDto<GetCampusOccupancyOut>
{
    public int CampusId { get; set; }

    /// <summary>
    /// Nome do campus
    /// </summary>
    public string Campus { get; set; }

    /// <summary>
    /// Quantidade de salas do campus
    /// </summary>
    public int TotalClassrooms { get; set; }

    /// <summary>
    /// Ocupação de minutos geral do campus, em porcentagem (0 a 100).
    /// Conta sala, e não assento: é o sala-minuto ocupado pelas turmas sobre o
    /// sala-minuto que as salas oferecem no horário de funcionamento do campus.
    /// </summary>
    public decimal OverallUsedMinutesRate { get; set; }

    /// <summary>
    /// Ocupação de assentos geral do campus, em porcentagem (0 a 100).
    /// Conta assento, e não sala: é o assento-minuto ocupado pelas turmas sobre o
    /// assento-minuto que as salas oferecem no horário de funcionamento do campus.
    /// Sala cheia de horário e vazia de gente derruba esta taxa sem mexer na de minutos.
    /// </summary>
    public decimal OverallUsedCapacityRate { get; set; }

    /// <summary>
    /// Quantidade de células em que o campus abre. É o denominador dos indicadores
    /// por turno — as células fechadas não são folga, são horário que não existe.
    /// </summary>
    public int OpenCells { get; set; }

    /// <summary>
    /// Uma célula por combinação de dia da semana e turno (6 dias x 3 turnos)
    /// </summary>
    public List<CampusOccupancyCellOut> Cells { get; set; } = [];

    public static IEnumerable<(string, GetCampusOccupancyOut)> GetExamples() =>
    [
        ("Exemplo", new GetCampusOccupancyOut
        {
            CampusId = 1,
            Campus = "Campus Agreste",
            TotalClassrooms = 2,
            OverallUsedMinutesRate = 27.5M,
            OverallUsedCapacityRate = 18.4M,
            OpenCells = 15,
            Cells =
            [
                new CampusOccupancyCellOut
                {
                    Day = Day.Monday,
                    Shift = Shift.Morning,
                    Open = true,
                    OpenMinutes = 300,
                    AvailableMinutes = 600,
                    UsedMinutes = 480,
                    UsedMinutesRate = 80M,
                    UsedCapacity = 15000,
                    UsedCapacityRate = 62.5M,
                    Classrooms =
                    [
                        new CampusOccupancyClassroomOut
                        {
                            Id = 1,
                            Name = "Sala 05",
                            UsedMinutes = 300,
                            AvailableMinutes = 300,
                            UsedMinutesRate = 100M,
                            UsedCapacityRate = 80M,
                        },
                        new CampusOccupancyClassroomOut
                        {
                            Id = 2,
                            Name = "Sala 06",
                            UsedMinutes = 180,
                            AvailableMinutes = 300,
                            UsedMinutesRate = 60M,
                            UsedCapacityRate = 45M,
                        },
                    ],
                },
            ],
        }),
    ];
}

public class CampusOccupancyCellOut
{
    public Day Day { get; set; }
    public Shift Shift { get; set; }

    /// <summary>
    /// Se o campus abre neste dia e turno
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Minutos que o campus abre no turno. É a janela do turno recortada pelo
    /// horário de funcionamento — sem multiplicar pelas salas.
    /// </summary>
    public int OpenMinutes { get; set; }

    /// <summary>
    /// Minutos disponíveis na célula: salas do campus x minutos abertos do turno
    /// </summary>
    public int AvailableMinutes { get; set; }

    /// <summary>
    /// Minutos ocupados por turmas alocadas nas salas do campus
    /// </summary>
    public int UsedMinutes { get; set; }

    /// <summary>
    /// Ocupação da célula, em porcentagem (0 a 100)
    /// </summary>
    public decimal UsedMinutesRate { get; set; }

    /// <summary>
    /// Assento-minuto ocupado na célula: cada turma conta alunos x minutos em aula.
    /// É a medida de movimento do turno — gente pelo tempo que ela fica —, e é o
    /// numerador do <see cref="UsedCapacityRate"/>.
    /// </summary>
    public int UsedCapacity { get; set; }

    /// <summary>
    /// Ocupação de assentos da célula, em porcentagem (0 a 100).
    /// É o assento-minuto ocupado pelas turmas sobre o assento-minuto que as salas
    /// do campus oferecem no turno — conta a capacidade de cada sala, e não a média
    /// das taxas, pra sala grande pesar mais que sala pequena.
    /// </summary>
    public decimal UsedCapacityRate { get; set; }

    /// <summary>
    /// Detalhamento da célula por sala
    /// </summary>
    public List<CampusOccupancyClassroomOut> Classrooms { get; set; } = [];
}

public class CampusOccupancyClassroomOut
{
    public int Id { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Minutos disponíveis
    /// </summary>
    public int AvailableMinutes { get; set; }

    /// <summary>
    /// Minutos ocupados na sala dentro da janela do turno
    /// </summary>
    public int UsedMinutes { get; set; }

    /// <summary>
    /// Ocupação de minutos da sala no turno, em porcentagem (0 a 100)
    /// </summary>
    public decimal UsedMinutesRate { get; set; }

    /// <summary>
    /// Ocupação de assentos da sala no turno, em porcentagem (0 a 100)
    /// </summary>
    public decimal UsedCapacityRate { get; set; }
}
