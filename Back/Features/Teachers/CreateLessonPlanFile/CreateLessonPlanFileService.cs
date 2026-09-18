using Estud.Back.Storage;

namespace Estud.Back.Features.Teachers.CreateLessonPlanFile;

public class CreateLessonPlanFileService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    private static readonly TimeSpan UploadUrlExpiration = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, (string Extension, long MaxSizeInBytes)> FileTypes = new()
    {
        ["image/png"] = ("png", 5 * 1024 * 1024),
        ["image/jpeg"] = ("jpg", 5 * 1024 * 1024),
        ["image/webp"] = ("webp", 5 * 1024 * 1024),
        ["application/pdf"] = ("pdf", 10 * 1024 * 1024),
    };

    private class Validator : AbstractValidator<CreateLessonPlanFileIn>
    {
        public Validator()
        {
            RuleFor(x => x.ContentType).Must(x => x != null && FileTypes.ContainsKey(x)).WithError(InvalidLessonPlanFileContentType.I);
            RuleFor(x => x.SizeInBytes)
                .Must((x, size) => FileTypes.TryGetValue(x.ContentType ?? "", out var type) && size >= 1 && size <= type.MaxSizeInBytes)
                .WithError(InvalidLessonPlanFileSize.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateLessonPlanFileOut, EstudError>> Create(int lessonId, CreateLessonPlanFileIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var lesson = await ctx.ClassLessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId && l.Class.InstitutionId == institutionId);
        if (lesson == null) return ClassLessonNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == lesson.ClassId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var path = $"{institutionId}/{lesson.ClassId}/{lesson.Id}/{Ulid.NewUlid()}.{FileTypes[data.ContentType].Extension}";
        var container = StorageContainer.LessonPlanFiles;

        return new CreateLessonPlanFileOut
        {
            UploadUrl = await storage.CreatePreSignedUrlForUpload(container, path, data.ContentType, data.SizeInBytes, UploadUrlExpiration),
            PublicUrl = storage.GetPublicUrl(container, path),
        };
    }
}
