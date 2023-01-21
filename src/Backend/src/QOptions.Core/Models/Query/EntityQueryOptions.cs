using QOptions.Models.Common;

namespace QOptions.Models.Query;

/// <summary>
/// Represents queryable entities source query options
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class EntityQueryOptions<TEntity> : QueryOptions<TEntity>, IEntityQueryOptions<TEntity> where TEntity : class, IEntity
{
    public IncludeOptions<TEntity>? IncludeOptions { get; set; }
}