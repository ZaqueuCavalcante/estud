using Estud.Back.Storage;

namespace Estud.Back.Features.Students.CreateClassActivityWorkFile;

public class CreateClassActivityWorkFileService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    private static readonly TimeSpan UploadUrlExpiration = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, (string Extension, long MaxSizeInBytes)> FileTypes = new()
    {
        ["image/png"] = ("png", 5 * 1024 * 1024),
        ["image/jpeg"] = ("jpg", 5 * 1024 * 1024),
        ["image/webp"] = ("webp", 5 * 1024 * 1024),
        ["application/pdf"] = ("pdf", 10 * 1024 * 1024),
    };

    private class Validator : AbstractValidator<CreateClassActivityWorkFileIn>
    {
        public Validator()
        {
            RuleFor(x => x.ContentType).Must(x => x != null && FileTypes.ContainsKey(x)).WithError(InvalidClassActivityWorkFileContentType.I);
            RuleFor(x => x.SizeInBytes)
                .Must((x, size) => FileTypes.TryGetValue(x.ContentType ?? "", out var type) && size >= 1 && size <= type.MaxSizeInBytes)
                .WithError(InvalidClassActivityWorkFileSize.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateClassActivityWorkFileOut, EstudError>> Create(int classActivityId, CreateClassActivityWorkFileIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var classActivity = await ctx.ClassActivities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == classActivityId);
        if (classActivity == null) return ClassActivityNotFound.I;
        if (!classActivity.AcceptsWorks()) return ClassActivityDoesNotAcceptWorks.I;

        var enrolled = await ctx.ClassStudents.AnyAsync(x => x.ClassId == classActivity.ClassId && x.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var path = $"{institutionId}/{classActivity.ClassId}/{classActivity.Id}/{studentId}/{Ulid.NewUlid()}.{FileTypes[data.ContentType].Extension}";
        var container = StorageContainer.ClassActivityWorkFiles;

        return new CreateClassActivityWorkFileOut
        {
            UploadUrl = await storage.CreatePreSignedUrlForUpload(container, path, data.ContentType, data.SizeInBytes, UploadUrlExpiration),
            PublicUrl = storage.GetPublicUrl(container, path),
        };
    }
}
