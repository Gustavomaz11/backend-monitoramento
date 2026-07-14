namespace SafeNavigation.Application.Abstractions;

public interface ICurrentActor
{
    Guid? ActorId { get; }
    string? ActorType { get; }
}
