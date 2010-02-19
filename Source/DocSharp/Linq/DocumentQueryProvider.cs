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
            using (var session = _engine.CreateSession())
            {
                var type = expression.Type;
                var collectionType = type.GetGenericArguments()[0].GetGenericArguments()[0];
                var allObjects = session.All(collectionType);
                return allObjects;
            }
        }

//        public TResult Execute<TResult>(Expression expression)
//        {
//            var result = ExecuteInternal(expression);
//            if (typeof(TResult).IsAssignableFrom(typeof(Document)))
//            {
//                return (TResult)(object)((result.IsFirstCall) ? result.Documents.First() : result.Documents.FirstOrDefault());
//            }
//            return (TResult)result.Documents;
//        }

        public TResult Execute<TResult>(Expression expression)
        {
            using (var session = _engine.CreateSession())
            {
                var type = typeof (TResult).GetGenericArguments()[0];
                var a = session.All1(type);
                return (TResult) a;
                return Activator.CreateInstance<TResult>();
            } 
        }
    }
}