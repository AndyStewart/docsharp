using System;
using System.Linq.Expressions;
using System.Reflection;
using DocSharp.Tests.Framework;

namespace DocSharp.Framework
{
    public class DocumentMap<T> : DocumentMap
    {
        /// <summary>
        /// Property to map as Id
        /// </summary>
        /// <param name="id"></param>
        public void Id(Expression<Func<T, object>> id)
        {
            IdentityProperty = GetProperty(id);
        }

        public virtual PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = getMemberExpression(expression);
            return (PropertyInfo)memberExpression.Member;
        }

        private MemberExpression getMemberExpression<TModel, T1>(Expression<Func<TModel, T1>> expression)
        {
            MemberExpression memberExpression = null;
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression.Body;
                memberExpression = body.Operand as MemberExpression;
            }
            else if (expression.Body.NodeType == ExpressionType.MemberAccess) memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null) throw new ArgumentException("Not a member access", "member");
            return memberExpression;
        }
    }
}