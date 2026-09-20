namespace Estud.Back.Features.Students.CreateClassActivityWorkComment;

public class CreateClassActivityWorkCommentService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<CreateClassActivityWorkCommentIn>
    {
        public Validator()
        {
            RuleFor(x => x.Content).NotEmpty().WithError(InvalidClassActivityWorkContent.I);
            RuleFor(x => x.Content).MaximumLength(10000).WithError(InvalidClassActivityWorkContent.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateClassActivityWorkCommentOut, EstudError>> Create(int classActivityId, CreateClassActivityWorkCommentIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classActivity = await ctx.ClassActivities.AsNoTracking()
            .Where(x => x.Id == classActivityId).FirstOrDefaultAsync();
        if (classActivity == null) return new ClassActivityNotFound();
        if (!classActivity.AcceptsWorks()) return ClassActivityDoesNotAcceptWorks.I;

        var classesIds = await ctx.ClassStudents.Where(x => x.StudentId == studentId).Select(x => x.ClassId).ToListAsync();
        if (!classesIds.Contains(classActivity.ClassId)) return new StudentNotEnrolledInClass();

        var work = await ctx.ClassActivityWorks.FirstOrDefaultAsync(w => w.ClassActivityId == classActivityId && w.StudentId == studentId);
        if (work == null) return ClassActivityWorkNotFound.I;

        var result = work.AddEntry(userId, ClassActivityWorkEntryType.Comment, data.Content);
        if (result.IsError) return result.Error;

        await ctx.SaveChangesAsync();

        return new CreateClassActivityWorkCommentOut { Id = work.Id };
    }
}
