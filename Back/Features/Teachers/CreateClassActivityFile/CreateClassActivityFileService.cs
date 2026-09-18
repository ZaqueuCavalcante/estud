using Estud.Back.Storage;

namespace Estud.Back.Features.Teachers.CreateClassActivityFile;

public class CreateClassActivityFileService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    private static readonly TimeSpan UploadUrlExpiration = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, (string Extension, long MaxSizeInBytes)> FileTypes = new()
    {
        ["image/png"] = ("png", 5 * 1024 * 1024),
        ["image/jpeg"] = ("jpg", 5 * 1024 * 1024),
        ["image/webp"] = ("webp", 5 * 1024 * 1024),
        ["application/pdf"] = ("pdf", 10 * 1024 * 1024),
    };

    private class Validator : AbstractValidator<CreateClassActivityFileIn>
    {
        public Validator()
        {
            RuleFor(x => x.ContentType).Must(x => x != null && FileTypes.ContainsKey(x)).WithError(InvalidClassActivityFileContentType.I);
            RuleFor(x => x.SizeInBytes)
                .Must((x, size) => FileTypes.TryGetValue(x.ContentType ?? "", out var type) && size >= 1 && size <= type.MaxSizeInBytes)
                .WithError(InvalidClassActivityFileSize.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateClassActivityFileOut, EstudError>> Create(int classId, CreateClassActivityFileIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var exists = await ctx.Classes.AnyAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (!exists) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var path = $"{institutionId}/{classId}/{Ulid.NewUlid()}.{FileTypes[data.ContentType].Extension}";
        var container = StorageContainer.ClassActivityFiles;

        return new CreateClassActivityFileOut
        {
            UploadUrl = await storage.CreatePreSignedUrlForUpload(container, path, data.ContentType, data.SizeInBytes, UploadUrlExpiration),
            PublicUrl = storage.GetPublicUrl(container, path),
        };
    }
}
