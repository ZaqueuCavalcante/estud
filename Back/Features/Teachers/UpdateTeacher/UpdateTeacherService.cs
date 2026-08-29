namespace Estud.Back.Features.Teachers.UpdateTeacher;

public class UpdateTeacherService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<UpdateTeacherIn>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithError(InvalidTeacherName.I);
            RuleFor(x => x.Name).MaximumLength(100).WithError(InvalidTeacherName.I);
            RuleFor(x => x.Email).Must(x => x.IsValidEmail()).WithError(InvalidEmail.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Update(int teacherId, UpdateTeacherIn data)
    {
        if (V.Run(data, out var error)) return error;

        var teacher = await ctx.Teachers.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.InstitutionId == ctx.RequestUser.InstitutionId && t.Id == teacherId);
        if (teacher == null) return TeacherNotFound.I;

        var email = data.Email.ToLowerInvariant();
        var emailUsed = await ctx.Users.AnyAsync(u => u.Email == email && u.Id != teacher.UserId);
        if (emailUsed) return EmailAlreadyUsed.I;

        teacher.Update(data.Name, email);

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
