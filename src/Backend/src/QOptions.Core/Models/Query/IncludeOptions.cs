using QOptions.Models.Common;

namespace QOptions.Models.Query;

/// <summary>
/// Represents including options
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class IncludeOptions<TEntity> where TEntity : class, IEntity
{
    public IncludeOptions()
    {
        IncludeModels = new List<string>();
    }

    public IEnumerable<string> IncludeModels { get; set; }
}