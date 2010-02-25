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
            var methodCallExpression = expression as MethodCallExpression;
            
//            var lambdaExpression = expression as LambdaExpression;
            return (bool) executeExpression(methodCallExpression.Arguments[1], document);
//            if (lambdaExpression == null)
//            {
//                var unary = (expression as UnaryExpression);
//                lambdaExpression = unary.Operand as LambdaExpression;
//            }
//
//            if (lambdaExpression != null)
//            {
//                var binary = lambdaExpression.Body as BinaryExpression;
//                if (binary != null)
//                    return executeBinaryExpression(binary, document);
//
//                var methodCallExpression= lambdaExpression.Body as MethodCallExpression;
//                if (methodCallExpression!= null)
//                    return executeMethodExpression(methodCallExpression, document);
//
//            }
//            return false;
        }

        private static object executeExpression(Expression expression, Document document)
        {
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
                return constantExpression.Value;

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
                return executeMemberExpression(memberExpression, document);

            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null)
                return executeBinaryExpression(binaryExpression, document);

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
                return executeMethodExpression(methodCallExpression, document);

            var unary = (expression as UnaryExpression);
            if (unary != null)
                return executeUnary(unary, document);

            var lambda = (expression as LambdaExpression);
            if (lambda != null)
                return executeLambda(lambda, document);

            return null;
        }

        private static object executeLambda(LambdaExpression lambda, Document document)
        {
            return executeExpression(lambda.Body, document);
        }

        private static object executeUnary(UnaryExpression unary, Document document)
        {
            return executeExpression(unary.Operand, document);
        }

        private static bool executeMethodExpression(MethodCallExpression methodCallExpression, Document document)
        {
            var argumentValues = methodCallExpression.Arguments.Select(q => executeExpression(q, document)).ToArray();
            var value = executeExpression(methodCallExpression.Object, document);
            return (bool)methodCallExpression.Method.Invoke(value, argumentValues);
        }

        private static bool executeBinaryExpression(BinaryExpression binary, Document document)
        {
            var leftValue = executeExpression(binary.Left, document);
            var rightValue = executeExpression(binary.Right, document);

            if (binary.NodeType == ExpressionType.LessThan)
                return Double.Parse(leftValue.ToString()) < Double.Parse(rightValue.ToString());

            if (binary.NodeType == ExpressionType.GreaterThan)
                return Double.Parse(leftValue.ToString()) > Double.Parse(rightValue.ToString());

            if (binary.NodeType == ExpressionType.OrElse)
                return (bool)leftValue || (bool)rightValue;

            if (binary.NodeType == ExpressionType.AndAlso)
                return (bool)leftValue && (bool)rightValue;

            return rightValue.Equals(leftValue);
        }



        private static object executeMemberExpression(MemberExpression memberExpression, Document document)
        {
            var property = memberExpression.Member as PropertyInfo;
            if (property != null)
                return property.GetValue(document.LooseData, null);

            var field = memberExpression.Member as FieldInfo;
            if (field != null)
                return field.GetValue(executeExpression(memberExpression.Expression, document));

            return executeExpression(memberExpression.Expression, document);
            
        }
    }
}