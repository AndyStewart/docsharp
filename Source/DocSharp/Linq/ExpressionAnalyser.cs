using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocSharp.Linq
{
    public class ExpressionAnalyser
    {
        public static bool Matches(Expression expression, Document document)
        {
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression == null)
            {
                var unary = (expression as UnaryExpression);
                lambdaExpression = unary.Operand as LambdaExpression;
            }

            if (lambdaExpression != null)
            {
                var binary = lambdaExpression.Body as BinaryExpression;
                if (binary != null)
                    return executeBinary(binary, document);

                var methodCallExpression= lambdaExpression.Body as MethodCallExpression;
                if (methodCallExpression!= null)
                {
                    var argumentValues = methodCallExpression.Arguments.Select(q => getValue(q, document)).ToArray();
                    var value = getValue(methodCallExpression.Object, document);
                    return (bool)methodCallExpression.Method.Invoke(value, argumentValues);
                }

            }
            return false;
        }

        private static bool executeBinary(BinaryExpression binary, Document document)
        {
            var leftValue = getValue(binary.Left, document);
            var rightValue = getValue(binary.Right, document);
            if (binary.NodeType == ExpressionType.LessThan)
                return Double.Parse(leftValue.ToString()) < Double.Parse(rightValue.ToString());

            if (binary.NodeType == ExpressionType.GreaterThan)
                return Double.Parse(leftValue.ToString()) > Double.Parse(rightValue.ToString());

            return rightValue.Equals(leftValue);
        }

        private static object getValue(Expression expression, Document document)
        {
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
                return constantExpression.Value;

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                var property = memberExpression.Member as PropertyInfo;
                return property.GetValue(document.LooseData, null);
            }
            return null;
        }
    }
}