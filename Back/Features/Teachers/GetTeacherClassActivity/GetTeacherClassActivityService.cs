using Estud.Back.Storage;

namespace Estud.Back.Features.Teachers.GetTeacherClassActivity;

public class GetTeacherClassActivityService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    public async Task<OneOf<GetTeacherClassActivityOut, EstudError>> Get(int classId, int activityId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var activity = await ctx.ClassActivities.AsNoTracking()
            .Include(a => a.Works).ThenInclude(w => w.Student).ThenInclude(s => s.User)
            .Include(a => a.Works).ThenInclude(w => w.Entries).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(a => a.Id == activityId && a.ClassId == classId);
        if (activity == null) return ClassActivityNotFound.I;

        var result = activity.ToGetTeacherClassActivityOut();

        foreach (var work in result.Works)
        {
            if (work.StudentPhoto.HasValue())
                work.StudentPhoto = storage.GetPublicUrl(StorageContainer.ProfilePhotos, work.StudentPhoto!);

            foreach (var entry in work.Entries.FindAll(e => e.UserPhoto.HasValue()))
                entry.UserPhoto = storage.GetPublicUrl(StorageContainer.ProfilePhotos, entry.UserPhoto!);
        }

        return result;
    }
}
