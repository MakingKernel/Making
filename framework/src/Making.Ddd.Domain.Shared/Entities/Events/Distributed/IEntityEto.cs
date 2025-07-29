namespace Making.Ddd.Domain.Shared.Entities.Events.Distributed;


public interface IEntityEto<TKey>
{
    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    TKey Id { get; set; }
}