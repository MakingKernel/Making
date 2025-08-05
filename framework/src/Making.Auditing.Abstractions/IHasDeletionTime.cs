namespace Mark.Auditing.Abstractions;

public interface IHasDeletionTime : ISoftDelete
{
    /// <summary>
    /// Deletion time.
    /// </summary>
    DateTime? DeletionTime { get; }
}
