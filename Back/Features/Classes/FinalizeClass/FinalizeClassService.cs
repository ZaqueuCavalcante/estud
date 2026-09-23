using Estud.Back.Domain.Classes;
using Estud.Back.Domain.Students;

namespace Estud.Back.Features.Classes.FinalizeClass;

public class FinalizeClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Finalize(int classId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        if (@class.Status == ClassStatus.Finalized) return ClassAlreadyFinalized.I;
        if (@class.Status != ClassStatus.Started) return ClassMustBeStarted.I;

        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var students = await ctx.ClassStudents
            .Where(cs => cs.ClassId == classId && cs.Status == StudentClassStatus.Matriculado)
            .ToListAsync();

        var works = (await ctx.ClassActivities.AsNoTracking()
            .Where(a => a.ClassId == classId)
            .SelectMany(a => a.Works.Select(w => new { w.StudentId, NoteType = a.Note, a.Weight, w.Note }))
            .ToListAsync())
            .ToLookup(w => w.StudentId, w => (w.NoteType, w.Weight, w.Note));

        var attendances = await ctx.ClassLessonAttendances.AsNoTracking()
            .Where(a => a.ClassId == classId)
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, Presences = g.Count(a => a.Present), Total = g.Count() })
            .ToDictionaryAsync(x => x.StudentId);

        foreach (var student in students)
        {
            var studentWorks = works[student.StudentId].ToList();

            foreach (var (type, note) in config.GradeRule.NotesByType(studentWorks))
            {
                ctx.Add(new StudentClassNote(classId, student.StudentId, type, Math.Round(note, 2, MidpointRounding.AwayFromZero)));
            }

            var average = Math.Round(config.GradeRule.Average(studentWorks), 1, MidpointRounding.AwayFromZero);

            // Turma sem nenhuma chamada lançada não pode reprovar todo mundo por falta.
            var frequency = attendances.TryGetValue(student.StudentId, out var attendance) && attendance.Total > 0
                ? Math.Round((decimal)attendance.Presences / attendance.Total * 100, 1, MidpointRounding.AwayFromZero)
                : 100;

            student.Finalize(average, frequency, config);
        }

        @class.Status = ClassStatus.Finalized;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}
