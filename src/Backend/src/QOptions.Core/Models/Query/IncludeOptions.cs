using System.Collections.Generic;
using QOptions.Core.Models.Common;

namespace QOptions.Core.Models.Query;

/// <summary>
/// Represents including options
/// </summary>
/// <typeparam name="TEntity">Query source type</typeparam>
public class IncludeOptions<TEntity> where TEntity : class, IQueryableEntity
{
    public IncludeOptions()
    {
        IncludeModels = new List<string>();
    }

    public IncludeOptions(string memberName)
    {
        IncludeModels = new List<string>
        {
            memberName
        };
    }

    public List<string> IncludeModels { get; set; }
}