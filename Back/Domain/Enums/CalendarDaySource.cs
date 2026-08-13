namespace Estud.Back.Domain.Enums;

/// <summary>
/// De onde veio o tipo efetivo de um dia do calendário.
/// </summary>
public enum CalendarDaySource
{
    /// <summary>
    /// Nenhum nível se manifestou sobre o dia: é um dia letivo comum.
    /// </summary>
    [Description("Padrão")]
    Default = 0,

    /// <summary>
    /// Sábado ou domingo, derivado da própria data.
    /// </summary>
    [Description("Fim de semana")]
    Weekend = 1,

    /// <summary>
    /// Feriado nacional, válido para todas as instituições.
    /// </summary>
    [Description("Nacional")]
    Global = 2,

    /// <summary>
    /// Override da instituição, válido para todos os campi dela.
    /// </summary>
    [Description("Instituição")]
    Institution = 3,

    /// <summary>
    /// Override do campus, válido só naquele campus.
    /// </summary>
    [Description("Campus")]
    Campus = 4,
}
