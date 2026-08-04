namespace Estud.Back.Domain.Students;

[DomainEvent("Aluno criado")]
public record StudentCreatedDomainEvent(string Uid) : IDomainEvent;
