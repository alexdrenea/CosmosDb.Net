using System;
using System.Linq.Expressions;

namespace CosmosDB.Net.Domain
{
    internal static class PropertyHelpers
    {
        internal static string GetName<T>(this Expression<Func<T, object>> exp)
        {
            if (exp == null) return string.Empty;
            MemberExpression body = exp.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}
