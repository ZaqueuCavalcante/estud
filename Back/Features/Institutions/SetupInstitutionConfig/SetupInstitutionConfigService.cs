namespace Estud.Back.Features.Institutions.SetupInstitutionConfig;

public class SetupInstitutionConfigService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<SetupInstitutionConfigIn>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithError(InvalidInstitutionName.I);
            RuleFor(x => x.Name).MaximumLength(100).WithError(InvalidInstitutionName.I);

            RuleFor(x => x.NoteLimit).InclusiveBetween(0.00M, 10.00M).WithError(InvalidNoteLimit.I);

            RuleFor(x => x.FrequencyLimit).InclusiveBetween(0.00M, 100.00M).WithError(InvalidFrequencyLimit.I);

            RuleFor(x => x.GradeRule).IsInEnum().WithError(InvalidClassGradeRule.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<SetupInstitutionConfigOut, EstudError>> Setup(SetupInstitutionConfigIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;

        var institution = await ctx.Institutions
            .Include(x => x.Config)
            .FirstAsync(x => x.Id == institutionId);

        institution.Rename(data.Name.Trim());
        institution.Config.Setup(data.NoteLimit, data.FrequencyLimit, data.GradeRule);

        await ctx.SaveChangesAsync();

        return institution.ToSetupInstitutionConfigOut();
    }
}
