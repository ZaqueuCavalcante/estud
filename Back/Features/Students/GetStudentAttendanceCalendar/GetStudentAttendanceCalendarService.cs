using Estud.Back.Domain.Calendar;

namespace Estud.Back.Features.Students.GetStudentAttendanceCalendar;

public class GetStudentAttendanceCalendarService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetStudentAttendanceCalendarOut> Get(GetStudentAttendanceCalendarIn data)
    {
        var year = data.Year ?? DateTime.UtcNow.Year;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        // O aluno pode ter turmas em campi diferentes, então o fundo do calendário
        // dele é o nível da instituição. O recorte por campus já veio antes: as
        // aulas só existem nos dias letivos do campus de cada turma.
        var calendar = await ctx.GetCalendarResolver(campusId: null, start, end);

        // Turmas em que o aluno está matriculado
        var classIds = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.StudentId == studentId && cs.Status == StudentClassStatus.Matriculado)
            .Select(cs => cs.ClassId)
            .ToListAsync();

        // Aulas do aluno no ano, com a frequência dele quando já lançada (null = ainda não lançada)
        var lessons = await ctx.ClassLessons.AsNoTracking()
            .Where(l => classIds.Contains(l.ClassId) && l.Date >= start && l.Date <= end)
            .Select(l => new
            {
                l.Date,
                Present = l.Attendances
                    .Where(a => a.StudentId == studentId)
                    .Select(a => (bool?)a.Present)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var presenceByDate = lessons
            .GroupBy(l => l.Date)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Present).ToList());

        var items = new List<GetStudentAttendanceCalendarItemOut>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            items.Add(new GetStudentAttendanceCalendarItemOut
            {
                Date = date.ToDateTime(TimeOnly.MinValue),
                Status = ResolveStatus(date, calendar, presenceByDate),
            });
        }

        return new GetStudentAttendanceCalendarOut
        {
            Year = year,
            Total = items.Count,
            Items = items,
        };
    }

    private static StudentDayAttendanceStatus ResolveStatus(
        DateOnly date,
        CalendarResolver calendar,
        Dictionary<DateOnly, List<bool?>> presenceByDate
    ) {
        // Dia sem aula para a instituição (fim de semana, feriado, férias, recesso)
        if (!calendar.IsSchoolDay(date)) return StudentDayAttendanceStatus.NoClass;

        // Dia letivo em que o aluno não tem nenhuma aula agendada
        if (!presenceByDate.TryGetValue(date, out var presences)) return StudentDayAttendanceStatus.NoClass;

        // Aula(s) do aluno sem frequência lançada ainda (futura ou pendente)
        var recorded = presences.Where(p => p.HasValue).ToList();
        if (recorded.Count == 0) return StudentDayAttendanceStatus.Undefined;

        // Falta em qualquer aula do dia já lançada conta como falta
        if (recorded.Any(p => p == false)) return StudentDayAttendanceStatus.Absent;

        return StudentDayAttendanceStatus.Present;
    }
}
