namespace Mark.Auditing.Abstractions;

public interface IHasCreationTime
{
    /// <summary>
    /// Creation time.
    /// </summary>
    DateTime CreationTime { get; }
}
