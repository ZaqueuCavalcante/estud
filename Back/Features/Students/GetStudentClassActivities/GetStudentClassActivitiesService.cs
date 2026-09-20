using Estud.Back.Storage;
using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Students.GetStudentClassActivities;

public class GetStudentClassActivitiesService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    public async Task<OneOf<GetStudentClassActivitiesOut, EstudError>> Get(int classId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classOk = await ctx.Classes.AsNoTracking().AnyAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (!classOk) return ClassNotFound.I;

        var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var gradeRule = await ctx.InstitutionConfigs.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId)
            .Select(c => c.GradeRule)
            .FirstAsync();

        var activities = await ctx.ClassActivities.AsNoTracking()
            .Where(a => a.ClassId == classId)
            .OrderBy(a => a.Note)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new
            {
                Activity = a,
                Work = a.Works.FirstOrDefault(w => w.StudentId == studentId),
            })
            .ToListAsync();

        var worksIds = activities.Where(x => x.Work != null).Select(x => x.Work!.Id).ToList();
        var entries = await ctx.ClassActivityWorkEntries.AsNoTracking()
            .Include(e => e.User)
            .Where(e => worksIds.Contains(e.ClassActivityWorkId))
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var items = activities.ConvertAll(x => x.Activity.ToGetStudentClassActivitiesItemOut(
            x.Work,
            entries.FindAll(e => e.ClassActivityWorkId == x.Work?.Id)
        ));

        foreach (var entry in items.SelectMany(i => i.WorkEntries).Where(e => e.UserPhoto.HasValue()))
            entry.UserPhoto = storage.GetPublicUrl(StorageContainer.ProfilePhotos, entry.UserPhoto!);

        var notes = gradeRule.NoteTypes
            .Union(items.Select(i => i.Note))
            .Order()
            .Select(note =>
            {
                var performance = ClassGrade.Performance(items
                    .Where(i => i.Note == note)
                    .Select(i => (i.Weight, i.Value)));

                return new GetStudentClassActivitiesNoteOut
                {
                    Note = note,
                    Performance = performance.HasValue ? Math.Round(performance.Value, 1, MidpointRounding.AwayFromZero) : null,
                };
            })
            .ToList();

        return new GetStudentClassActivitiesOut
        {
            Notes = notes,
            Activities = items,
        };
    }
}
