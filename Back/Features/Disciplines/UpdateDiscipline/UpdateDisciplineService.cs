namespace Estud.Back.Features.Disciplines.UpdateDiscipline;

public class UpdateDisciplineService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<UpdateDisciplineIn>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithError(InvalidDisciplineName.I);
            RuleFor(x => x.Name).MaximumLength(50).WithError(InvalidDisciplineName.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<UpdateDisciplineOut, EstudError>> Update(int disciplineId, UpdateDisciplineIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;
        var discipline = await ctx.Disciplines.FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == disciplineId);
        if (discipline == null) return DisciplineNotFound.I;

        discipline.Update(data.Name);
        await ctx.SaveChangesAsync();

        return discipline.ToUpdateDisciplineOut();
    }
}
