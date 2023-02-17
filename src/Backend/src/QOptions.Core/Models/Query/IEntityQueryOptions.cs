﻿using QOptions.Core.Models.Common;

namespace QOptions.Core.Models.Query
{
    /// <summary>
    /// Defines properties for queryable entities source query options
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IEntityQueryOptions<TEntity> : IQueryOptions<TEntity> where TEntity : class, IQueryableEntity
    {
        /// <summary>
        /// Requested include model options
        /// </summary>
        IncludeOptions<TEntity> IncludeOptions { get; set; }
    }
}