namespace Estud.Back.Domain.Teachers;

[DomainEvent("Professor criado")]
public record TeacherCreatedDomainEvent(string Uid) : IDomainEvent;
