namespace Mark.Auditing.Abstractions;

public interface IMayHaveCreator<out TCreator>
{
    /// <summary>
    /// Reference to the creator.
    /// </summary>
    TCreator? Creator { get; }
}

/// <summary>
/// Standard interface for an entity that MAY have a creator.
/// </summary>
public interface IMayHaveCreator
{
    /// <summary>
    /// Id of the creator.
    /// </summary>
    Guid? CreatorId { get; }
}