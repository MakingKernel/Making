namespace Mark.Auditing.Abstractions;

/// <summary>
/// This interface can be implemented to store creation information (who and when created).
/// </summary>
public interface ICreationAuditedObject : IHasCreationTime, IMayHaveCreator
{

}

/// <summary>
/// Adds navigation property (object reference) to <see cref="ICreationAuditedObject"/> interface.
/// </summary>
/// <typeparam name="TCreator">Type of the user</typeparam>
public interface ICreationAuditedObject<out TCreator> : ICreationAuditedObject, IMayHaveCreator<TCreator>
{

}
