using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DocSharp.Linq;
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
                if (queryCache.ContainsKey((typeof(T))))
                {
                    var list = queryCache[typeof (T)];
                    list.Add(document);
                }
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
                foreach (KeyValuePair<Type, IList> listCache in queryCache)
                {
                    Document docFound = null;
                    var list = listCache.Value;
                    foreach(Document document in list)
                    {
                        if (document.Id == id)
                            docFound = document;
                    }

                    if (docFound != null)
                        list.Remove(docFound);
                }

                session.Delete(id);
            }
        }

        public void Update(Document strongDocument)
        {
            using (var session = storageEngine.CreateSession())
            {
                session.Update(strongDocument);
                if (queryCache.ContainsKey(strongDocument.Type))
                {
                    var cache = queryCache[strongDocument.Type];
                    foreach (Document doc in cache)
                    {
                        if (doc.Id == strongDocument.Id)
                            doc.LooseData = strongDocument.LooseData;
                    }
                    cache.Add(strongDocument);
                }
            }
        }

        public IList<Document<T>> Query<T>(Expression<Func<T, bool>> whereClause)
        {
            using (var session = storageEngine.CreateSession())
            {
                var indexFound = indexes.FirstOrDefault(q => q.AppliesTo(typeof(T)));
                if (indexFound != null)
                    return findByIndex(indexFound, whereClause);

                return session.Query(whereClause.Compile());
            }
        }

        Dictionary<Type,IList> queryCache = new Dictionary<Type, IList>();
        public IQueryable<Document<T>> All<T>()
        {
            using (var session = storageEngine.CreateSession())
            {
                if (queryCache.ContainsKey(typeof(T)))
                    return (IQueryable<Document<T>>) queryCache[typeof (T)].AsQueryable();
                
                var resultOfQuery = session.Query<T>(q => q != null);
                queryCache.Add(typeof(T), (IList) resultOfQuery);
                return resultOfQuery.AsQueryable();
            }
        }

        public IQueryable<Document<T>> Query<T>()
        {
            return new DocumentQuery<Document<T>>(new DocumentQueryProvider(storageEngine));
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
}