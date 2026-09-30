namespace Estud.Back.Domain.Classes;

[DomainEvent("Atividade publicada")]
public record ClassActivityPublishedDomainEvent(string Uid) : IDomainEvent;
