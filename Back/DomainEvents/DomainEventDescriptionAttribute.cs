namespace Estud.Back.DomainEvents;

[AttributeUsage(AttributeTargets.Class)]
public class DomainEventAttribute(string description) : Attribute
{
    public string Description { get; set; } = description;
}
