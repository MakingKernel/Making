namespace Mark.Auditing.Abstractions;

public interface ISoftDelete
{
    /// <summary>
    /// Used to mark an Entity as 'Deleted'.
    /// </summary>
    bool IsDeleted { get; }
}
