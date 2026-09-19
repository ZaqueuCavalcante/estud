namespace Estud.Back.Domain.Classes;

[DomainEvent("Atividade alterada")]
public record ClassActivityUpdatedDomainEvent(string Uid) : IDomainEvent;
