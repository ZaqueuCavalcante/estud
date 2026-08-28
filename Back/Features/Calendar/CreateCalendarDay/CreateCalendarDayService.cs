using Estud.Back.Domain.Calendar;

namespace Estud.Back.Features.Calendar.CreateCalendarDay;

public class CreateCalendarDayService(EstudDbContext ctx) : IEstudService
{
    private const int MaxRangeDays = 366;

    private class Validator : AbstractValidator<CreateCalendarDayIn>
    {
        public Validator()
        {
            RuleFor(x => x.Date.Year).InclusiveBetween(1970, 2070).WithError(InvalidCalendarDayDate.I);
            RuleFor(x => x.EndDate!.Value.Year).InclusiveBetween(1970, 2070).WithError(InvalidCalendarDayDate.I)
                .When(x => x.EndDate != null);

            RuleFor(x => x).Must(x => x.EndDate == null || x.EndDate.Value.Date >= x.Date.Date)
                .WithError(InvalidCalendarDayRange.I);
            RuleFor(x => x).Must(x => x.EndDate == null || (x.EndDate.Value.Date - x.Date.Date).Days < MaxRangeDays)
                .WithError(InvalidCalendarDayRange.I);

            RuleFor(x => x.DayType).NotNull().WithError(InvalidCalendarDayType.I);
            RuleFor(x => x.DayType).IsInEnum().WithError(InvalidCalendarDayType.I);

            // Fim de semana é derivado da data, não é um override que alguém grava.
            RuleFor(x => x.DayType).NotEqual(DayType.Weekend).WithError(InvalidCalendarDayType.I);

            RuleFor(x => x.Description).MaximumLength(100).WithError(InvalidCalendarDayDescription.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateCalendarDayOut, EstudError>> Create(CreateCalendarDayIn data)
    {
        if (V.Run(data, out var error)) return error;
        var institutionId = ctx.RequestUser.InstitutionId;

        if (data.CampusId != null)
        {
            var campusExists = await ctx.Campi.AnyAsync(c => c.Id == data.CampusId && c.InstitutionId == institutionId);
            if (!campusExists) return CampusNotFound.I;
        }

        var start = DateOnly.FromDateTime(data.Date);
        var end = data.EndDate == null ? start : DateOnly.FromDateTime(data.EndDate.Value);

        // O conflito é só com o mesmo nível: um override de campus pode cair em
        // cima de um dia que a instituição já customizou — é justamente o ponto.
        var taken = await ctx.CalendarDays.AsNoTracking()
            .Where(d => d.InstitutionId == institutionId && d.CampusId == data.CampusId)
            .AnyAsync(d => d.Date >= start && d.Date <= end);
        if (taken) return CalendarDayAlreadyExists.I;

        var days = new List<CalendarDay>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            days.Add(new CalendarDay(institutionId, data.CampusId, date, data.DayType!.Value, data.Description));
        }

        ctx.AddRange(days);
        await ctx.SaveChangesAsync();

        return new CreateCalendarDayOut
        {
            Ids = days.ConvertAll(d => d.Id),
            Total = days.Count,
        };
    }
}
