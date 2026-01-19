namespace GameStore.Common.Events;

public class GameCriadoEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; }
    public string EventName => nameof(GameCriadoEvent);
    
    public Guid GameId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public DateTime OccurredOn { get; set; }
}

public class GameAtualizadoEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; }
    public string EventName => nameof(GameAtualizadoEvent);
    
    public Guid GameId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public DateTime OccurredOn { get; set; }
}

public class GameRemovidoEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; }
    public string EventName => nameof(GameRemovidoEvent);
    
    public Guid GameId { get; set; }
    public DateTime OccurredOn { get; set; }
}
