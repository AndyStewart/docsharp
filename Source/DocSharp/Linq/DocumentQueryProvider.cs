using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DocSharp.Storage;

namespace DocSharp.Linq
{
    public class DocumentQueryProvider : IQueryProvider
    {
        private readonly StorageEngine _engine;

        public DocumentQueryProvider(StorageEngine engine)
        {
            _engine = engine;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new DocumentQuery<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            using (var session = _engine.CreateSession())
            {
                var documentType = typeof (TResult).GetGenericArguments()[0];
                var collectionType = documentType.GetGenericArguments()[0];
                var methodExpression = expression as MethodCallExpression;
                var typeToFindExpression = methodExpression.Arguments[0] as ConstantExpression;
                var lambdaExpression = methodExpression.Arguments[1] as UnaryExpression;
                return (TResult)session.All(collectionType, lambdaExpression);

                //return (TResult)session.All(collectionType, TODO);
            } 
        }
    }
}