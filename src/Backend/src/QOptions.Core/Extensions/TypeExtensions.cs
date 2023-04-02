using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using QOptions.Core.Models.Attributes;
using QOptions.Core.Models.Query;
using System.Collections;

namespace QOptions.Core.Extensions
{
    /// <summary>
    /// Provides extensions for type information
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Checks if type is simple
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if type is simple, otherwise false</returns>
        public static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(bool?);
        }

        // public static bool IsNullable(this Type type)
        // {
        //     return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) || Nullable.GetUnderlyingType(type) != null;
        // }

        /// <summary>
        /// Gets appropriate search method for a type
        /// </summary>
        /// <param name="type">Type in request</param>
        /// <param name="searchComparing">Determines whether to use search comparing methods</param>
        /// <returns>Method info of the compare method</returns>
        /// <exception cref="ArgumentException">If type is not primitive</exception>
        /// <exception cref="ArgumentNullException">If type is null</exception>
        /// <exception cref="InvalidOperationException">If not method found</exception>
        public static MethodInfo GetCompareMethod(this Type type, bool searchComparing = false)
        {
            if (type == null)
                throw new ArgumentNullException();

            if (!type.IsSimpleType())
                throw new ArgumentException("Not a primitive type");

            var methodName = type == typeof(string) && searchComparing ? "Contains" : "Equals";
            return type.GetMethod(methodName, new[] { type }) ?? throw new InvalidOperationException("Method not found");
        }

        /// <summary>
        /// Gets value in appropriate type in boxed format
        /// </summary>
        /// <param name="filter">Filter value</param>
        /// <param name="type">Type in request</param>
        /// <returns>Boxed filter value in its type</returns>
        /// <exception cref="ArgumentException">If type is not primitive</exception>
        /// <exception cref="ArgumentNullException">If filter or type is null</exception>
        /// <exception cref="InvalidOperationException">if no parse method found</exception>
        public static object GetValue(this QueryFilter filter, Type type)
        {
            if (filter == null || type == null)
                throw new ArgumentNullException();

            if (!type.IsSimpleType())
                throw new ArgumentException("Not a primitive type");

            // Return string or parsed value
            if (type == typeof(string))
            {
                return filter.Value;
            }
            else
            {
                // Create specific expression based on type
                var parameter = Expression.Parameter(typeof(string));
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
                var parseMethod = underlyingType.GetMethod("Parse", new[] { typeof(string) }) ??
                                  throw new InvalidOperationException($"Method not found to parse value for type {type.FullName}");
                var argument = Expression.Constant(filter.Value);
                var methodCaller = Expression.Call(parseMethod, argument);
                var returnConverter = Expression.Convert(methodCaller, typeof(object));
                var function = Expression.Lambda<Func<string, object>>(returnConverter, parameter).Compile();
                return function.Invoke(filter.Value);
            }
        }

        public static IEnumerable<PropertyInfo> GetSearchableProperties(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException();

            return type.GetProperties().Where(x => x.PropertyType.IsSimpleType() && Attribute.IsDefined(x, typeof(SearchablePropertyAttribute)));
        }

        public static IEnumerable<PropertyInfo> GetEncryptedProperties(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException();


            return type.GetProperties().Where(x => x.PropertyType == typeof(string) && Attribute.IsDefined(x, typeof(EncryptedPropertyAttribute)));
        }

        private static bool IsCollection(this Type type)
        {
            return type.GetInterfaces()
            .Any(x => new[]
            {
                nameof(IEnumerable),
                nameof(ICollection),
                nameof(IList),
            }.Contains(x.Name));
        }

        private static IEnumerable<Type> GetCollectionUnderlyingType(this Type type)
        {
            if (null == type)
                throw new ArgumentNullException(nameof(type));

            return type.GetGenericArguments().ToList();
        }

        public static Type GetUnderlyingType(Type type)
        {
            var underlyingType = type.IsCollection() ? type.GetCollectionUnderlyingType().First() : type;
            underlyingType = Nullable.GetUnderlyingType(underlyingType) ?? underlyingType;

            return underlyingType;
        }
    }
}