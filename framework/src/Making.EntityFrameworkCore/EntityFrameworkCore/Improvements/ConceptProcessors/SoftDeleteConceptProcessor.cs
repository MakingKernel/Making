using Making.EntityFrameworkCore.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements.ConceptProcessors;

/// <summary>
/// Concept processor for handling soft delete concerns
/// Automatically sets deletion timestamps and prevents hard deletes
/// </summary>
public class SoftDeleteConceptProcessor : ConceptProcessorBase
{
    public override int Order => 300; // Execute after auditing

    protected override IEnumerable<EntityEntry> GetRelevantEntries(List<EntityEntry> entries)
    {
        return entries.Where(entry =>
            entry.State == EntityState.Deleted &&
            ImplementsInterface<ISoftDelete>(entry));
    }

    protected override void ProcessRelevantEntries(IEnumerable<EntityEntry> entries, IServiceProvider serviceProvider)
    {
        var currentUserAccessor = serviceProvider.GetService<ICurrentUserAccessor>();
        var clock = serviceProvider.GetService<IClock>() ?? SystemClock.Instance;
        var now = clock.UtcNow;

        foreach (var entry in entries)
        {
            ProcessSoftDeleteForEntry(entry, currentUserAccessor, now);
        }
    }

    private static void ProcessSoftDeleteForEntry(EntityEntry entry, ICurrentUserAccessor? currentUserAccessor, DateTime now)
    {
        if (entry.Entity is not ISoftDelete softDeleteEntity)
        {
            return;
        }

        // Convert hard delete to soft delete
        entry.State = EntityState.Modified;
        
        // Set soft delete properties
        softDeleteEntity.IsDeleted = true;
        softDeleteEntity.DeletionTime = now;

        // Set deletion auditing if supported
        if (entry.Entity is IHasDeletionUser hasDeletionUser && currentUserAccessor != null)
        {
            if (currentUserAccessor.UserId.HasValue)
            {
                hasDeletionUser.DeleterId = currentUserAccessor.UserId.Value;
            }
            hasDeletionUser.DeleterName = currentUserAccessor.UserName;
        }

        // Ensure we don't modify creation/modification properties during soft delete
        PreventModificationOfAuditingProperties(entry);
    }

    private static void PreventModificationOfAuditingProperties(EntityEntry entry)
    {
        // Don't update creation properties during soft delete
        if (entry.Entity is IHasCreationTime)
        {
            entry.Property(nameof(IHasCreationTime.CreationTime)).IsModified = false;
        }

        if (entry.Entity is IHasCreationUser)
        {
            entry.Property(nameof(IHasCreationUser.CreatorId)).IsModified = false;
            entry.Property(nameof(IHasCreationUser.CreatorName)).IsModified = false;
        }

        // Don't update modification properties during soft delete
        if (entry.Entity is IHasModificationTime)
        {
            entry.Property(nameof(IHasModificationTime.LastModificationTime)).IsModified = false;
        }

        if (entry.Entity is IHasModificationUser)
        {
            entry.Property(nameof(IHasModificationUser.LastModifierId)).IsModified = false;
            entry.Property(nameof(IHasModificationUser.LastModifierName)).IsModified = false;
        }
    }
}