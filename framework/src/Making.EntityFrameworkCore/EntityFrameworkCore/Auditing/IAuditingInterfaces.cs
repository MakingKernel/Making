namespace Making.EntityFrameworkCore.EntityFrameworkCore.Auditing;

/// <summary>
/// Interface for entities that track creation time.
/// </summary>
public interface IHasCreationTime
{
    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    DateTime CreationTime { get; set; }
}

/// <summary>
/// Interface for entities that track modification time.
/// </summary>
public interface IHasModificationTime
{
    /// <summary>
    /// Gets or sets the last modification time.
    /// </summary>
    DateTime? LastModificationTime { get; set; }
}

/// <summary>
/// Interface for entities that track creation user.
/// </summary>
public interface IHasCreationUser
{
    /// <summary>
    /// ID of the user who created this entity.
    /// </summary>
    Guid CreatorId { get; set; }

    /// <summary>
    /// Name of the user who created this entity.
    /// </summary>
    string? CreatorName { get; set; }
}

/// <summary>
/// Interface for entities that track modification user.
/// </summary>
public interface IHasModificationUser
{
    /// <summary>
    /// ID of the user who last modified this entity.
    /// </summary>
    Guid? LastModifierId { get; set; }

    /// <summary>
    /// Name of the user who last modified this entity.
    /// </summary>
    string? LastModifierName { get; set; }
}

/// <summary>
/// Interface for entities that support deletion user tracking.
/// </summary>
public interface IHasDeletionUser
{
    /// <summary>
    /// ID of the user who deleted this entity.
    /// </summary>
    Guid DeleterId { get; set; }

    /// <summary>
    /// Name of the user who deleted this entity.
    /// </summary>
    string? DeleterName { get; set; }
}

/// <summary>
/// Abstraction for accessing current user information.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user name.
    /// </summary>
    string? UserName { get; }
}

/// <summary>
/// Abstraction for time operations.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}

/// <summary>
/// System clock implementation.
/// </summary>
public class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    
    private SystemClock() { }
    
    public DateTime UtcNow => DateTime.UtcNow;
}