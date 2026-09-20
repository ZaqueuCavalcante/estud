using Estud.Back.Storage;

namespace Estud.Back.Features.Students.GetStudentClassActivity;

public class GetStudentClassActivityService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    public async Task<OneOf<GetStudentClassActivityOut, EstudError>> Get(int classId, int activityId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classOk = await ctx.Classes.AsNoTracking().AnyAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (!classOk) return ClassNotFound.I;

        var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var activity = await ctx.ClassActivities.AsNoTracking()
            .Where(a => a.Id == activityId && a.ClassId == classId)
            .Select(a => new
            {
                Activity = a,
                Work = a.Works.FirstOrDefault(w => w.StudentId == studentId),
            })
            .FirstOrDefaultAsync();
        if (activity == null) return ClassActivityNotFound.I;

        var workId = activity.Work?.Id ?? 0;
        var entries = await ctx.ClassActivityWorkEntries.AsNoTracking()
            .Include(e => e.User)
            .Where(e => e.ClassActivityWorkId == workId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var result = activity.Activity.ToGetStudentClassActivityOut(activity.Work, entries);

        foreach (var entry in result.WorkEntries.FindAll(e => e.UserPhoto.HasValue()))
            entry.UserPhoto = storage.GetPublicUrl(StorageContainer.ProfilePhotos, entry.UserPhoto!);

        return result;
    }
}
