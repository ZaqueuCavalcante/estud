using Estud.Back.Domain.Periods;

namespace Estud.Back.Features.Periods.CreateAcademicPeriod;

public class CreateAcademicPeriodService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<CreateAcademicPeriodOut, EstudError>> Create(CreateAcademicPeriodIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodResult = AcademicPeriod.New(institutionId, data.Name, data.StartAt, data.EndAt);
        if (periodResult.HasError(out var error, out var period)) return error;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.InstitutionId == institutionId && p.Name == period.Name);
        if (periodExists) return AcademicPeriodAlreadyExists.I;

        await ctx.SaveChangesAsync(period);

        return new CreateAcademicPeriodOut { Id = period.Id };
    }
}
