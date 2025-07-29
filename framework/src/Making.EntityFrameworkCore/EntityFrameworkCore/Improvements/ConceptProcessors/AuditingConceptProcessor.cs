using Making.EntityFrameworkCore.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements.ConceptProcessors;

/// <summary>
/// Concept processor for handling auditing concerns
/// Automatically sets creation and modification timestamps and user information
/// </summary>
public class AuditingConceptProcessor : ConceptProcessorBase
{
    public override int Order => 200; // Execute after multi-tenancy but before soft delete

    protected override IEnumerable<EntityEntry> GetRelevantEntries(List<EntityEntry> entries)
    {
        return entries.Where(entry =>
            IsInStates(entry, EntityState.Added, EntityState.Modified) &&
            (ImplementsInterface<IHasCreationTime>(entry) ||
             ImplementsInterface<IHasModificationTime>(entry) ||
             ImplementsInterface<IHasCreationUser>(entry) ||
             ImplementsInterface<IHasModificationUser>(entry)));
    }

    protected override void ProcessRelevantEntries(IEnumerable<EntityEntry> entries, IServiceProvider serviceProvider)
    {
        var currentUserAccessor = serviceProvider.GetService<ICurrentUserAccessor>();
        var clock = serviceProvider.GetService<IClock>() ?? SystemClock.Instance;
        var now = clock.UtcNow;

        foreach (var entry in entries)
        {
            ProcessAuditingForEntry(entry, currentUserAccessor, now);
        }
    }

    private static void ProcessAuditingForEntry(EntityEntry entry, ICurrentUserAccessor? currentUserAccessor, DateTime now)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                ProcessCreationAuditing(entry, currentUserAccessor, now);
                break;

            case EntityState.Modified:
                ProcessModificationAuditing(entry, currentUserAccessor, now);
                break;
        }
    }

    private static void ProcessCreationAuditing(EntityEntry entry, ICurrentUserAccessor? currentUserAccessor, DateTime now)
    {
        // Set creation time
        if (entry.Entity is IHasCreationTime hasCreationTime)
        {
            hasCreationTime.CreationTime = now;
        }

        // Set creation user
        if (entry.Entity is IHasCreationUser hasCreationUser && currentUserAccessor != null)
        {
            if (currentUserAccessor.UserId.HasValue)
            {
                hasCreationUser.CreatorId = currentUserAccessor.UserId.Value;
            }
            hasCreationUser.CreatorName = currentUserAccessor.UserName;
        }
    }

    private static void ProcessModificationAuditing(EntityEntry entry, ICurrentUserAccessor? currentUserAccessor, DateTime now)
    {
        // Don't update creation properties on modification
        if (entry.Entity is IHasCreationTime)
        {
            entry.Property(nameof(IHasCreationTime.CreationTime)).IsModified = false;
        }

        if (entry.Entity is IHasCreationUser)
        {
            entry.Property(nameof(IHasCreationUser.CreatorId)).IsModified = false;
            entry.Property(nameof(IHasCreationUser.CreatorName)).IsModified = false;
        }

        // Set modification time
        if (entry.Entity is IHasModificationTime hasModificationTime)
        {
            hasModificationTime.LastModificationTime = now;
        }

        // Set modification user
        if (entry.Entity is IHasModificationUser hasModificationUser && currentUserAccessor != null)
        {
            hasModificationUser.LastModifierId = currentUserAccessor.UserId;
            hasModificationUser.LastModifierName = currentUserAccessor.UserName;
        }
    }
}