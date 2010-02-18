using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DocSharp.Storage;

namespace DocSharp
{
    public class DocSharp : IDisposable
    {
        private StorageEngine storageEngine;

        public DocSharp(string dbName)
        {
            storageEngine = new StorageEngine(dbName);
        }

        public void Dispose()
        {
            storageEngine.Dispose();
        }

        public Document<T> Store<T>(T value)
        {
            var document = new Document<T> { Id = Guid.NewGuid(), Data = value };
            using (var session = storageEngine.CreateSession())
            {
                session.Insert(document);

                var indexFound = indexes.FirstOrDefault(q => q.AppliesTo(typeof(T)));
                if (indexFound != null)
                {
                    var index = indexFound;
                    index.Add(document.Id, value);
                }
            }
            return document;
        }

        public Document<T> Load<T>(Guid id)
        {
            using (var session = storageEngine.CreateSession())
            {
                return session.Load<T>(id);
            }
        }

        public void Delete(Guid id)
        {
            using (var session = storageEngine.CreateSession())
            {
                session.Delete(id);
            }
        }

        public void Update<T>(Document<T> document)
        {
            using (var session = storageEngine.CreateSession())
            {
                session.Update(document);
            }
        }

        public IList<Document<T>> Query<T>(Expression<Func<T, bool>> whereClause)
        {
            using (var session = storageEngine.CreateSession())
            {
                var indexFound = indexes.FirstOrDefault(q => q.AppliesTo(typeof(T)));
                if (indexFound != null)
                    return findByIndex((Index) indexFound, whereClause);

                return session.Query(whereClause.Compile());
            }
        }

        public IQueryable<Document<T>> Query<T>()
        {
            
            using (var session = storageEngine.CreateSession())
            {
                return new DocumentQuery<T>(session);
            }
        }

        private IList<Document<T>> findByIndex<T>(Index index, Expression<Func<T, bool>> clause)
        {
            var documentsFound = new List<Document<T>>();

            var methodExpression = (MethodCallExpression)clause.Body;
            foreach (var list in index.Find(methodExpression))
                documentsFound.Add(Load<T>(list));


            return documentsFound;
        }

        private IList<Index> indexes = new List<Index>();
        public void Index<T>(Expression<Func<T, object>> columnToIndex)
        {
            var index = new Index(typeof(T));
            index.Rule(GetProperty(columnToIndex));
            indexes.Add(index);
        }

        public virtual PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = getMemberExpression(expression);
            return (PropertyInfo) memberExpression.Member;
        }

        private MemberExpression getMemberExpression<TModel, T1>(Expression<Func<TModel, T1>> expression)
        {
            MemberExpression memberExpression = null;
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression) expression.Body;
                memberExpression = body.Operand as MemberExpression;
            }
            else if (expression.Body.NodeType == ExpressionType.MemberAccess) memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null) throw new ArgumentException("Not a member access", "member");
            return memberExpression;
        }
    }

    public class DocumentQuery<T> : IQueryable<Document<T>>
    {
        private DocumentQueryProvider provider;
        private ConstantExpression expression;

        public DocumentQuery(DocumentQueryProvider queryProvider)
        {
            provider = queryProvider;
            expression = Expression.Constant(this);
        }

        public IEnumerator<Document<T>> GetEnumerator()
        {
            return ((IEnumerable<Document<T>>)provider.Execute(this.expression)).GetEnumerator();
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

    public class DocumentQueryProvider : IQueryProvider
    {
        private Expression expression;

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            this.expression = expression;
            return (IQueryable<TElement>) new DocumentQuery<Document<TElement>>(this);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}