using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Classes.UpdateClassSchedules;

public class UpdateClassSchedulesService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Update(int classId, UpdateClassSchedulesIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes
            .Include(c => c.Teachers)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        if (@class.Status is ClassStatus.Started or ClassStatus.Finalized) return ClassAlreadyStarted.I;

        var schedulesResult = data.Schedules.ConvertAll(x => (x.Day, x.Start, x.End, x.TeacherId, x.ClassroomId)).ToSchedules();
        if (schedulesResult.IsError) return schedulesResult.Error;
        var schedules = schedulesResult.Success;

        var teacherIds = @class.Teachers.Select(t => t.Id).ToList();
        var targetTeacherIds = schedules.Where(s => s.TeacherId != null).Distinct().Select(s => s.TeacherId!.Value).ToList();
        if (!targetTeacherIds.IsSubsetOf(teacherIds)) return InvalidScheduleTeacher.I;

        var teacherSchedulesResult = await ValidateTeacherSchedules(institutionId, classId, teacherIds, schedules);
        if (teacherSchedulesResult.IsError) return teacherSchedulesResult.Error;

        ctx.Schedules.RemoveRange(@class.Schedules);
        @class.Schedules = schedules;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }

    private async Task<OneOf<EstudSuccess, EstudError>> ValidateTeacherSchedules(
        int institutionId,
        int classId,
        List<int> teacherIds,
        List<Schedule> schedules)
    {
        const string sql = @"
            SELECT
                s.*
            FROM
                estud.classes c
            INNER JOIN
                estud.classes__teachers ct ON ct.class_id = c.id
            INNER JOIN
                estud.schedules s ON s.class_id = c.id
            WHERE
                c.institution_id = {0}
                    AND
                c.id <> {1}
                    AND
                c.status <> {2}
                    AND
                ct.teacher_id = ANY({3})
            GROUP BY
                s.id
        ";
        var teacherSchedules = await ctx.Schedules
            .FromSqlRaw(sql, institutionId, classId, ClassStatus.Finalized.ToInt(), teacherIds.ToArray())
            .AsNoTracking().ToListAsync();

        // Conflito de agenda por professor
        foreach (var teacherId in teacherIds)
        {
            var slotsForTeacher = schedules.Where(s => s.TeacherId == teacherId).ToList();
            if (slotsForTeacher.Count == 0) continue;

            var otherSchedules = teacherSchedules.Where(x => x.TeacherId == teacherId).ToList();

            if (slotsForTeacher.Any(x => otherSchedules.Any(x.Conflict))) return TeacherScheduleConflict.I;
        }

        return EstudSuccess.I;
    }
}
