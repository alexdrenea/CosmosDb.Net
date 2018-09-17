using System;
using System.Linq.Expressions;

namespace CosmosDb.Domain.Helpers
{
    public static class PropertyHelpers
    {
        public static string GetName<T>(this Expression<Func<T, object>> exp)
        {
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
