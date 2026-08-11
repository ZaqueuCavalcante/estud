using Estud.Back.Domain.Classrooms;

namespace Estud.Back.Domain.Classes;

/// <summary>
/// Representa um horário genérico. <br/>
/// Pode ser o horário das aulas de uma turma presencial. <br/>
/// Pode ser o horário das aulas de uma turma online (com <see cref="ClassroomId"/> nulo). <br/>
/// Pode ser o horário preferencial de um professor antes do início das aulas (com <see cref="ClassId"/> e <see cref="ClassroomId"/> nulos). <br/>
/// </summary>
public class Schedule
{
    public int Id { get; set; }
    public int? ClassId { get; set; }
    public int? TeacherId { get; set; }
    public int? ClassroomId { get; set; }
    public Day Day { get; set; }
    public Hour Start { get; set; }
    public Hour End { get; set; }

    public Class? Class { get; set; }
    public Classroom? Classroom { get; set; }

    private Schedule() {}

    private Schedule(
        Day day,
        Hour startAt,
        Hour endAt,
        int? teacherId = null,
        int? classroomId = null
    ) {
        Day = day;
        Start = startAt;
        End = endAt;
        TeacherId = teacherId;
        ClassroomId = classroomId;
    }

    public static OneOf<Schedule, EstudError> New(
        Day day,
        Hour startAt,
        Hour endAt,
        int? teacherId = null,
        int? classroomId = null
    ) {
        if (!day.IsValid()) return new InvalidDay();
        if (!startAt.IsValid()) return new InvalidHour();
        if (!endAt.IsValid()) return new InvalidHour();

        if (startAt == endAt || endAt < startAt)
            return new InvalidSchedule();

        return new Schedule(day, startAt, endAt, teacherId, classroomId);
    }

    /// <summary>
    /// Cria uma janela de horário sem dono — sem turma, professor nem sala.
    /// </summary>
    public static Schedule Window(Day day, Hour start, Hour end)
    {
        return new Schedule(day, start, end);
    }

    public int GetDiffInMinutes()
    {
        return Start.DiffInMinutes(End);
    }

    /// <summary>
    /// O pedaço deste horário que cai dentro do outro, no mesmo dia.
    /// </summary>
    public Schedule? Intersect(Schedule other)
    {
        if (Day != other.Day) return null;

        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;

        if (start >= end) return null;

        return new Schedule(Day, start, end, TeacherId, ClassroomId) { ClassId = ClassId };
    }

    public bool Conflict(Schedule other)
    {
        if (Day != other.Day)
            return false;

        if (Start == other.Start || End == other.End)
            return true;

        if (Start < other.Start && other.Start < End)
            return true;

        if (Start < other.End && other.End < End)
            return true;

        if (other.Start < Start && Start < other.End)
            return true;

        if (other.Start < End && End < other.End)
            return true;

        return false;
    }
}
