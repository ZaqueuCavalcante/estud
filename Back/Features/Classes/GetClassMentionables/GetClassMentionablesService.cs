namespace Estud.Back.Features.Classes.GetClassMentionables;

public class GetClassMentionablesService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetClassMentionablesOut, EstudError>> Get(int classId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;

        var classOk = await ctx.Classes.AsNoTracking().AnyAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (!classOk) return ClassNotFound.I;

        if (ctx.RequestUser.Type == UserType.Teacher)
        {
            var teacherId = await ctx.GetTeacherId(institutionId, userId);
            var assigned = await ctx.ClassTeachers.AsNoTracking().AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
            if (!assigned) return TeacherNotAssignedToClass.I;
        }
        else
        {
            var studentId = await ctx.GetStudentId(institutionId, userId);
            var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (!enrolled) return StudentNotEnrolledInClass.I;
        }

        var items = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.ClassId == classId)
            .OrderBy(cs => cs.Student!.Name)
            .Select(cs => new GetClassMentionablesItemOut
            {
                Id = cs.Student!.UserId,
                Name = cs.Student.Name,
            })
            .ToListAsync();

        return new GetClassMentionablesOut { Items = items };
    }
}
