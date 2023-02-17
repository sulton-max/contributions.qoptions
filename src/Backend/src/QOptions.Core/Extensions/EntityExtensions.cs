using System;
using System.Collections.Generic;
using System.Linq;
using QOptions.Core.Models.Common;

namespace QOptions.Core.Extensions
{
    /// <summary>
    /// Provides extensions specifically for entities
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Checks whether given type is entity
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if given type is entity, otherwise false</returns>
        /// <returns><see langword="true" /> if given type is entity; <see langword="false" /> otherwise.</returns>
        public static bool IsEntity(this Type type)
        {
            return type.InheritsOrImplements(typeof(IQueryableEntity));
        }

        /// <summary>
        /// Gets direct child entities from a type
        /// </summary>
        /// <param name="type">Type to get direct child entities</param>
        /// <returns>Set of direct child entities</returns>
        /// <exception cref="ArgumentException">If type is null</exception>
        public static IEnumerable<Type> GetDirectChildEntities(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException();

            if (!type.IsEntity())
                throw new ArgumentException();

            // Get children
            var result = type.GetProperties().Where(x => x.PropertyType.IsClass && x.PropertyType.IsEntity()).Select(x => x.PropertyType).ToList();

            return result.Distinct();
        }
    }
}