using System;
using System.Linq.Expressions;
using System.Reflection;

namespace DocSharp.Linq
{
    public class ExpressionAnalyser
    {
        public static bool Matches(Expression expression, Document document)
        {
            var unary = (expression as UnaryExpression);
            var lambdaExpression = unary.Operand as LambdaExpression;
            var binary = lambdaExpression.Body as BinaryExpression;
            var left = binary.Left as MemberExpression;
            var member = left.Member as PropertyInfo;
            var objectValue = member.GetValue(document.LooseData, null);
            var right = binary.Right as ConstantExpression;
            return right.Value.Equals(objectValue);
        }
    }
}