using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using System;
using System.Threading.Tasks;

namespace Hmm.Core;

/// <summary>
/// Helper class providing common deactivation logic for entities implementing IActivatable.
/// Eliminates duplicate deactivation code across managers (AuthorManager, TagManager, ContactManager).
/// </summary>
public static class DeactivationHelper
{
    /// <summary>
    /// Deactivates an entity by setting its IsActivated property to false.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from Entity and implements IActivatable</typeparam>
    /// <param name="repository">The repository used to retrieve and update the entity</param>
    /// <param name="id">The ID of the entity to deactivate</param>
    /// <param name="entityName">The display name of the entity type for error messages (e.g., "author", "tag")</param>
    /// <param name="commitAction">Optional action to commit the transaction after update (e.g., UnitOfWork.CommitAsync)</param>
    /// <returns>ProcessingResult indicating success or failure of the deactivation operation</returns>
    public static async Task<ProcessingResult<Unit>> DeactivateAsync<T>(
        IRepository<T> repository,
        int id,
        string entityName,
        Func<Task> commitAction = null)
        where T : Entity, IActivatable
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        try
        {
            var entityResult = await repository.GetEntityAsync(id);
            if (!entityResult.Success)
            {
                return ProcessingResult<Unit>.NotFound($"Cannot find {entityName} with id: {id}");
            }

            var entity = entityResult.Value;
            if (!entity.IsActivated)
            {
                return ProcessingResult<Unit>.Ok(Unit.Value, $"{entityName} with id {id} is already deactivated");
            }

            entity.IsActivated = false;
            var updatedResult = await repository.UpdateAsync(entity);

            if (!updatedResult.Success)
            {
                return ProcessingResult<Unit>.Fail(updatedResult.ErrorMessage ?? $"Failed to deactivate {entityName}", updatedResult.ErrorType);
            }

            if (commitAction != null)
            {
                await commitAction();
            }

            return ProcessingResult<Unit>.Ok(Unit.Value, $"{entityName} with id {id} has been deactivated");
        }
        catch (Exception ex)
        {
            return ProcessingResult<Unit>.FromException(ex);
        }
    }

    /// <summary>
    /// Deactivates an entity using IEntityLookup for retrieval and IRepository for update.
    /// Use this overload when the lookup service is separate from the repository.
    /// </summary>
    /// <typeparam name="T">The entity type that inherits from Entity and implements IActivatable</typeparam>
    /// <param name="lookup">The entity lookup service used to retrieve the entity</param>
    /// <param name="repository">The repository used to update the entity</param>
    /// <param name="id">The ID of the entity to deactivate</param>
    /// <param name="entityName">The display name of the entity type for error messages (e.g., "author", "tag")</param>
    /// <param name="commitAction">Optional action to commit the transaction after update (e.g., UnitOfWork.CommitAsync)</param>
    /// <returns>ProcessingResult indicating success or failure of the deactivation operation</returns>
    public static async Task<ProcessingResult<Unit>> DeactivateAsync<T>(
        Hmm.Utility.Dal.Query.IEntityLookup lookup,
        IRepository<T> repository,
        int id,
        string entityName,
        Func<Task> commitAction = null)
        where T : Entity, IActivatable
    {
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        try
        {
            var entityResult = await lookup.GetEntityAsync<T>(id);
            if (!entityResult.Success)
            {
                return ProcessingResult<Unit>.NotFound($"Cannot find {entityName} with id: {id}");
            }

            var entity = entityResult.Value;
            if (!entity.IsActivated)
            {
                return ProcessingResult<Unit>.Ok(Unit.Value, $"{entityName} with id {id} is already deactivated");
            }

            entity.IsActivated = false;
            var updatedResult = await repository.UpdateAsync(entity);

            if (!updatedResult.Success)
            {
                return ProcessingResult<Unit>.Fail(updatedResult.ErrorMessage ?? $"Failed to deactivate {entityName}", updatedResult.ErrorType);
            }

            if (commitAction != null)
            {
                await commitAction();
            }

            return ProcessingResult<Unit>.Ok(Unit.Value, $"{entityName} with id {id} has been deactivated");
        }
        catch (Exception ex)
        {
            return ProcessingResult<Unit>.FromException(ex);
        }
    }
}
