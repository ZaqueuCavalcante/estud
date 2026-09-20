using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Students.GetStudentClassActivities;

public static class GetStudentClassActivitiesMapper
{
    extension(ClassActivity activity)
    {
        public GetStudentClassActivitiesItemOut ToGetStudentClassActivitiesItemOut(ClassActivityWork? work, List<ClassActivityWorkEntry> entries)
        {
            return new()
            {
                Id = activity.Id,
                ClassId = activity.ClassId,
                Note = activity.Note,
                Title = activity.Title,
                Description = activity.Description,
                Type = activity.ActivityType,
                Status = activity.Status,
                Weight = activity.Weight,
                CreatedAt = activity.CreatedAt,
                DueDate = activity.DueDate,
                DueHour = activity.DueHour,
                WorkStatus = activity.GetWorkStatus(work),
                WorkEntries = entries.ConvertAll(e => e.ToGetStudentClassActivitiesWorkEntryOut()),
                Value = work?.Note ?? 0,
                PonderedValue = (work?.Note ?? 0) * activity.Weight / 100M,
            };
        }
    }

    extension(ClassActivityWorkEntry entry)
    {
        public GetStudentClassActivitiesWorkEntryOut ToGetStudentClassActivitiesWorkEntryOut()
        {
            return new()
            {
                Id = entry.Id,
                UserId = entry.UserId,
                User = entry.User?.Name,
                UserPhoto = entry.User?.ProfilePhoto,
                Type = entry.Type,
                Content = entry.Content,
                Metadata = entry.Metadata,
                CreatedAt = entry.CreatedAt,
            };
        }
    }
}
