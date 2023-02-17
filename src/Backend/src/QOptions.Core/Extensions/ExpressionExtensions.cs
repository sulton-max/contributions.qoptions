using System;
using System.Linq.Expressions;

namespace QOptions.Core.Extensions
{
    /// <summary>
    /// Extends <see cref="Expression"/> base and other types
    /// </summary>
    public static class ExpressionExtensions
    {
        public static MemberExpression GetMemberExpression<TModel, TKey>(this Expression<Func<TModel, TKey>> keySelector) where TModel : class
        {
            MemberExpression memberExpression;
            switch (keySelector.Body.NodeType)
            {
                case ExpressionType.Convert:
                    memberExpression = ((UnaryExpression)keySelector.Body).Operand as MemberExpression;
                    break;
                case ExpressionType.MemberAccess:
                    memberExpression = keySelector.Body as MemberExpression;
                    break;
                default:
                    throw new ArgumentException("Not a member access", nameof(keySelector));
            }

            return memberExpression;
        }
    }
}