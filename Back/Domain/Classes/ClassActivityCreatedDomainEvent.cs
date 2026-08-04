namespace Estud.Back.Domain.Classes;

[DomainEvent("Atividade criada")]
public record ClassActivityCreatedDomainEvent(string Uid) : IDomainEvent;
