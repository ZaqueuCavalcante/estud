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
            .Include(c => c.OpeningHours)
            .FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var newHours = new List<OpeningHour>();
        foreach (var day in data.Days)
        {
            foreach (var window in day.Windows)
            {
                var hour = new OpeningHour(day.Day, window.Start, window.End);

                // Duas janelas do mesmo dia se sobrepondo tornariam a soma de
                // minutos abertos maior que o dia — o cálculo soma as janelas.
                if (newHours.Any(hour.Overlaps)) return OverlappingOpeningHours.I;

                newHours.Add(hour);
            }
        }

        // Replace-all: a semana vem inteira, então o que não veio some.
        ctx.OpeningHours.RemoveRange(campus.OpeningHours);
        campus.OpeningHours = newHours;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
