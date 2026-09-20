using Estud.Back.Domain.Students;

namespace Estud.Back.Domain.Classes;

/// <summary>
/// Entrega de uma atividade feita por um aluno
/// </summary>
public class ClassActivityWork
{
    public int Id { get; set; }
    public int ClassActivityId { get; set; }
    public int StudentId { get; set; }
    public EstudStudent Student { get; set; }
    public decimal Note { get; set; }
    public ClassActivityWorkStatus Status { get; set; }
    public DateTime? LastEntryAt { get; set; }
    public List<ClassActivityWorkEntry> Entries { get; set; } = [];

    private ClassActivityWork() { }

    public ClassActivityWork(
        int classActivityId,
        int studentId
    ) {
        ClassActivityId = classActivityId;
        StudentId = studentId;
        Note = 0;
        Status = ClassActivityWorkStatus.Pending;
    }

    public OneOf<EstudSuccess, EstudError> AddEntry(int userId, ClassActivityWorkEntryType type, string? content)
    {
        if (Status == ClassActivityWorkStatus.Finalized) return ClassActivityWorkAlreadyFinalized.I;

        AddEntry(new ClassActivityWorkEntry(Id, userId, type, content));

        if (Status == ClassActivityWorkStatus.Pending) Status = ClassActivityWorkStatus.Review;

        return EstudSuccess.I;
    }

    public OneOf<EstudSuccess, EstudError> AddTeacherEntry(
        int userId,
        string? content,
        decimal? note,
        ClassActivityWorkStatus? status
    ) {
        if (content.IsEmpty() && note == null && status == null) return InvalidClassActivityWorkEntry.I;
        if (note is < 0 or > 10) return new InvalidStudentClassNote();

        if (content.HasValue())
        {
            AddEntry(new ClassActivityWorkEntry(Id, userId, ClassActivityWorkEntryType.Comment, content));
        }

        if (note != null && note != Note)
        {
            var change = new ClassActivityWorkNoteChange { FromNote = Note, ToNote = note.Value };
            AddEntry(new ClassActivityWorkEntry(Id, userId, ClassActivityWorkEntryType.NoteChange, metadata: change));
            Note = note.Value;
        }

        if (status != null && status != Status)
        {
            var change = new ClassActivityWorkStatusChange { FromStatus = Status, ToStatus = status.Value };
            AddEntry(new ClassActivityWorkEntry(Id, userId, ClassActivityWorkEntryType.StatusChange, metadata: change));
            Status = status.Value;
        }

        return EstudSuccess.I;
    }

    private void AddEntry(ClassActivityWorkEntry entry)
    {
        Entries.Add(entry);
        LastEntryAt = entry.CreatedAt;
    }
}
