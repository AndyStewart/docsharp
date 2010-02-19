using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DocSharp.Linq
{
    public class DocumentQuery<T> : IQueryable<T>
    {
        private DocumentQueryProvider provider;
        private Expression expression;

        public DocumentQuery(DocumentQueryProvider queryProvider)
        {
            provider = queryProvider;
            expression = Expression.Constant(this);
        }

        public DocumentQuery(DocumentQueryProvider queryProvider, Expression expression1)
        {
            expression = expression1;
            provider = queryProvider;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return provider.Execute<IEnumerable<T>>(expression).GetEnumerator();
            //return ((IEnumerable<T>)provider.Execute(expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression
        {
            get { return expression; }
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public IQueryProvider Provider
        {
            get { return provider; }
        }
    }
}