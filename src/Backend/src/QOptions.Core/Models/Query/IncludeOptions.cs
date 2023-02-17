using System.Collections.Generic;
using QOptions.Core.Models.Common;

namespace QOptions.Core.Models.Query
{
    /// <summary>
    /// Represents including options
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class IncludeOptions<TEntity> where TEntity : class, IQueryableEntity
    {
        public IncludeOptions()
        {
            IncludeModels = new List<string>();
        }
        
        public IncludeOptions(List<string> includeModels)
        {
            IncludeModels = includeModels;
        }

        public List<string> IncludeModels { get; set; }
    }
}