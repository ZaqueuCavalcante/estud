using Estud.Back.Storage;

namespace Estud.Back.Features.Teachers.CreateLessonPlanImage;

public class CreateLessonPlanImageService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    private const long MaxSizeInBytes = 5 * 1024 * 1024;
    private static readonly TimeSpan UploadUrlExpiration = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, string> FileExtensions = new()
    {
        ["image/png"] = "png",
        ["image/jpeg"] = "jpg",
        ["image/webp"] = "webp",
    };

    private class Validator : AbstractValidator<CreateLessonPlanImageIn>
    {
        public Validator()
        {
            RuleFor(x => x.ContentType).Must(x => x != null && FileExtensions.ContainsKey(x)).WithError(InvalidLessonPlanImageContentType.I);
            RuleFor(x => x.SizeInBytes).InclusiveBetween(1, MaxSizeInBytes).WithError(InvalidLessonPlanImageSize.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateLessonPlanImageOut, EstudError>> Create(int lessonId, CreateLessonPlanImageIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var lesson = await ctx.ClassLessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId && l.Class.InstitutionId == institutionId);
        if (lesson == null) return ClassLessonNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == lesson.ClassId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var path = $"{institutionId}/{lesson.ClassId}/{lesson.Id}/{Ulid.NewUlid()}.{FileExtensions[data.ContentType]}";
        var container = StorageContainer.LessonPlanImages;

        return new CreateLessonPlanImageOut
        {
            UploadUrl = await storage.CreatePreSignedUrlForUpload(container, path, data.ContentType, data.SizeInBytes, UploadUrlExpiration),
            PublicUrl = storage.GetPublicUrl(container, path),
        };
    }
}
