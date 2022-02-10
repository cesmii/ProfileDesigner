using System;
using System.Linq.Expressions;

namespace CESMII.ProfileDesigner.Data.Extensions
{
    //credit: https://www.puresourcecode.com/dotnet/net-core/universal-predicatebuilder-for-expression/
    public static class ExpressionExtension
    {
        public static Expression<Func<T, bool>> AndExtension<T>(this Expression<Func<T, bool>> expr1,
               Expression<Func<T, bool>> expr2)
        {
            if (expr2 == null && expr1 != null)
                return expr1;

            if (expr1 == null && expr2 != null)
                return expr2;

            var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, secondBody), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> OrExtension<T>(this Expression<Func<T, bool>> expr1,
               Expression<Func<T, bool>> expr2)
        {
            if (expr2 == null && expr1 != null)
                return expr1;

            if (expr1 == null && expr2 != null)
                return expr2;

            var secondBody = expr2.Body.Replace(expr2.Parameters[0], expr1.Parameters[0]);
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, secondBody), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> NotExtension<T>(this Expression<Func<T, bool>> expression)
        {
            var negated = Expression.Not(expression.Body);
            return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
        }

        public static Expression Replace(this Expression expression, Expression searchEx,
               Expression replaceEx)
        {
            return new ReplaceVisitor(searchEx, replaceEx).Visit(expression);
        }

        internal class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression from, to;
            public ReplaceVisitor(Expression from, Expression to)
            {
                this.from = from;
                this.to = to;
            }
            public override Expression Visit(Expression node)
            {
                return node == from ? to : base.Visit(node);
            }
        }
    }
}
