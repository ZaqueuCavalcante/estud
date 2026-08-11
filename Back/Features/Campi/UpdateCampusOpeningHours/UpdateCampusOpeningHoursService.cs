using Estud.Back.Domain.Campi;

namespace Estud.Back.Features.Campi.UpdateCampusOpeningHours;

public class UpdateCampusOpeningHoursService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<UpdateCampusOpeningHoursIn>
    {
        public Validator()
        {
            RuleFor(x => x.Days).NotNull().WithError(InvalidOpeningHoursList.I);

            RuleFor(x => x.Days)
                .Must(days => days == null || days.Select(d => d.Day).Distinct().Count() == days.Count)
                .WithError(InvalidOpeningHoursList.I);

            RuleForEach(x => x.Days).ChildRules(day =>
            {
                day.RuleFor(d => d.Day).IsInEnum().WithError(InvalidOpeningHoursList.I);
                day.RuleFor(d => d.Windows).NotNull().WithError(InvalidOpeningHoursList.I);

                day.RuleForEach(d => d.Windows).ChildRules(window =>
                {
                    window.RuleFor(w => w.Start).IsInEnum().WithError(InvalidOpeningHour.I);
                    window.RuleFor(w => w.End).IsInEnum().WithError(InvalidOpeningHour.I);
                    window.RuleFor(w => w.End).Must((w, end) => end > w.Start).WithError(InvalidOpeningHour.I);
                });
            });
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Update(int campusId, UpdateCampusOpeningHoursIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;

        var campus = await ctx.Campi
            .Include(x => x.OpeningHours)
            .FirstOrDefaultAsync(x => x.Id == campusId && x.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var newHours = new List<OpeningHour>();
        foreach (var day in data.Days)
        {
            var shifts = new HashSet<Shift>();

            foreach (var window in day.Windows)
            {
                var hourResult = OpeningHour.New(day.Day, window.Start, window.End);
                if (hourResult.IsError) return hourResult.Error;
                var hour = hourResult.Success;

                // Duas janelas do mesmo dia se sobrepondo tornariam a soma de
                // minutos abertos maior que o dia — o cálculo soma as janelas.
                if (newHours.Any(hour.Overlaps)) return OverlappingOpeningHours.I;

                // A grade é lida por turno, então cada janela precisa caber
                // inteira dentro de um deles — nada de atravessar a fronteira.
                var shift = ShiftOf(hour);
                if (shift == null) return OpeningHourOutsideShift.I;

                // E cada turno do dia comporta no máximo uma janela.
                if (!shifts.Add(shift.Value)) return MultipleOpeningHoursInShift.I;

                newHours.Add(hour);
            }
        }

        // Replace-all: a semana vem inteira, então o que não veio some.
        ctx.OpeningHours.RemoveRange(campus.OpeningHours);
        campus.OpeningHours = newHours;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }

    /// <summary>
    /// O turno que contém a janela inteira, ou null se ela não cabe em nenhum —
    /// seja por cruzar a fronteira entre dois turnos, seja por cair fora de todos.
    /// </summary>
    private static Shift? ShiftOf(OpeningHour hour)
    {
        foreach (var shift in Enum.GetValues<Shift>())
        {
            if (hour.Start >= shift.StartAtHour && hour.End <= shift.EndAtHour) return shift;
        }

        return null;
    }
}
