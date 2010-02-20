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
                return session.Find<TResult>(expression);
            } 
        }
    }
}