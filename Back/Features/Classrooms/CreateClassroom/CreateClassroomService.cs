using Estud.Back.Domain.Classrooms;

namespace Estud.Back.Features.Classrooms.CreateClassroom;

public class CreateClassroomService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<CreateClassroomIn>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithError(InvalidClassroomName.I);
            RuleFor(x => x.Name).MaximumLength(50).WithError(InvalidClassroomName.I);

            RuleFor(x => x.Capacity).GreaterThan(0).WithError(InvalidClassroomCapacity.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateClassroomOut, EstudError>> Create(CreateClassroomIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;

        var campusOk = await ctx.Campi.AnyAsync(c => c.InstitutionId == institutionId && c.Id == data.CampusId);
        if (!campusOk) return CampusNotFound.I;

        var classroom = new Classroom(institutionId, data.CampusId, data.Name, data.Capacity);
        await ctx.SaveChangesAsync(classroom);

        return new CreateClassroomOut { Id = classroom.Id };
    }
}
