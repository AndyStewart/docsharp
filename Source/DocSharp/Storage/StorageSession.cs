using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DocSharp.Linq;
using Microsoft.Isam.Esent.Interop;

namespace DocSharp.Storage
{
    public class StorageSession : IDisposable
    {
        private Session session;
        private Table table;
        private Transaction transaction;
        private JET_COLUMNID columnId;
        private JET_COLUMNID columnData;
        private JET_COLUMNID columnCollectionName;

        public StorageSession(Instance instance, string dbName)
        {
            session = new Session(instance);
            JET_DBID dbid;
            Api.JetAttachDatabase(session, dbName, AttachDatabaseGrbit.None);
            Api.JetOpenDatabase(session, dbName, null, out dbid, OpenDatabaseGrbit.None);
            table = new Table(session, dbid, "Documents", OpenTableGrbit.None);
            transaction = new Transaction(session);

            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(session, table);
            columnId = columnids["Id"];
            columnCollectionName = columnids["collection_name"];
            columnData = columnids["data"];
        }

        public void Dispose()
        {
            if (transaction != null)
            {
                //transaction.Commit(CommitTransactionGrbit.None); - Safe but slow
                transaction.Commit(CommitTransactionGrbit.LazyFlush); // Used to increase performance, we'll mull this over
                transaction.Dispose();
            }

            if (table != null)
                table.Dispose();

            if (session != null)
                session.Dispose();
        }

        public void Insert<T>(Document<T> strongDocument)
        {
            using (var update = new Update(session, table, JET_prep.Insert))
            {
                Api.SetColumn(session, table, columnId, strongDocument.Id.ToString(), Encoding.Unicode);
                Api.SetColumn(session, table, columnCollectionName, getCollectionName<T>(), Encoding.Unicode);
                Api.SetColumn(session, table, columnData, ObjectConverter.ToJson(strongDocument.Data), Encoding.Unicode);
                update.Save();
            }
        }

        public Document<T> Load<T>(Guid guid)
        {
            Api.JetSetCurrentIndex(session, table, null);
            Api.MakeKey(session, table, guid.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);

            if (Api.TrySeek(session, table, SeekGrbit.SeekEQ))
            {
                return readDocument<T>();
            }
            return null;
        }

        private Document<T> readDocument<T>()
        {
            var typeName = Api.RetrieveColumnAsString(session, table, columnCollectionName);
            if (getCollectionName<T>() != typeName)
                return null;

            var documentFound = new Document<T>();
            documentFound.Id = new Guid(Api.RetrieveColumnAsString(session, table, columnId));
            documentFound.Data = ObjectConverter.ToObject<T>(Api.RetrieveColumnAsString(session, table, columnData));
            return documentFound;
        }


        private void populateDocument(Document strongDocument)
        {
            strongDocument.Id = new Guid(Api.RetrieveColumnAsString(session, table, columnId));
            strongDocument.LooseData = ObjectConverter.ToObject(Api.RetrieveColumnAsString(session, table, columnData), strongDocument.Type);
        }

        private string getCollectionName<T>()
        {
            return typeof (T).Namespace + typeof (T).Name;
        }

        public void Delete(Guid guid)
        {
            Api.JetSetCurrentIndex(session, table, null);
            Api.MakeKey(session, table, guid.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(session, table, SeekGrbit.SeekEQ);
            Api.JetDelete(session, table);
        }

        public void Update(Document strongDocument)
        {
            Api.JetSetCurrentIndex(session, table, null);
            Api.MakeKey(session, table, strongDocument.Id.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(session, table, SeekGrbit.SeekEQ);
            using (var update = new Update(session, table, JET_prep.Replace))
            {
                Api.SetColumn(session, table, columnId, strongDocument.Id.ToString(), Encoding.Unicode);
                Api.SetColumn(session, table, columnData, ObjectConverter.ToJson(strongDocument.LooseData), Encoding.Unicode);
                update.Save();
            }
        }

        public IList<Document<T>> Query<T>(Func<T, bool> whereClause)
        {
            var listFound = new List<Document<T>>();
            Api.JetSetCurrentIndex(session, table, "by_collection_name");
            Api.MakeKey(session, table, getCollectionName<T>(), Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.TrySeek(session, table, SeekGrbit.SeekEQ);
            if (Api.TryMoveFirst(session, table))
            {
                do
                {
                    var documentFound = readDocument<T>();
                    if (documentFound != null && whereClause.Invoke(documentFound.Data))
                        listFound.Add(documentFound);
                }
                while (Api.TryMoveNext(session, table));
            }
            return listFound;
        }

        /// <summary>
        /// Return all documents of a certain type
        /// </summary>
        /// <param name="collectionType"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IList All(Type collectionType, UnaryExpression expression)
        {
            var genericDocumentType = typeof (Document<>).MakeGenericType(collectionType);
            var listFoundType = typeof(List<>).MakeGenericType(genericDocumentType );
            var listFound = (IList)Activator.CreateInstance(listFoundType);
            var collectionName = collectionType.Namespace + collectionType.Name;

            Api.JetSetCurrentIndex(session, table, "by_collection_name");
            Api.MakeKey(session, table, collectionName, Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(session, table, SeekGrbit.SeekEQ);
            if (Api.TryMoveFirst(session, table))
            {
                do
                {
                    var a = typeof(Document<>).MakeGenericType(collectionType);
                    var newStrongDocument = (Document)Activator.CreateInstance(a);

                    populateDocument(newStrongDocument);
                    if ((bool)expression.Method.Invoke(newStrongDocument, null))
                        listFound.Add(newStrongDocument);
                }
                while (Api.TryMoveNext(session, table));
            }
            return listFound;
        }

        public TResult Find<TResult>(Expression expression)
        {
            var methodExpression = expression as MethodCallExpression;
            var expressionType = methodExpression.Arguments[0] as ConstantExpression;
            var queryable = expressionType.Value as IQueryable;
            Type queryType = queryable.ElementType;
            var collectionName = queryType.GetGenericArguments()[0].Namespace + queryType.GetGenericArguments()[0].Name;

            Api.JetSetCurrentIndex(session, table, "by_collection_name");
            Api.MakeKey(session, table, collectionName, Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.TrySeek(session, table, SeekGrbit.SeekEQ);
            if (Api.TryMoveFirst(session, table))
            {

                if (methodExpression.Method.Name == "First")
                {
                    do
                    {
                        var newStrongDocument = (Document) Activator.CreateInstance(typeof (TResult));
                        populateDocument(newStrongDocument);

                        if (ExpressionAnalyser.Matches(methodExpression, newStrongDocument))
                            return (TResult) (object) newStrongDocument;

                    } while (Api.TryMoveNext(session, table));
                }
                if (methodExpression.Method.Name == "Where")
                {
                    var collectionTypeType = typeof(List<>).MakeGenericType(queryType);
                    var collection = (IList)Activator.CreateInstance(collectionTypeType);
                    do
                    {
                        var newStrongDocument = (Document)Activator.CreateInstance(queryType);
                        populateDocument(newStrongDocument);
                        if (ExpressionAnalyser.Matches(methodExpression, newStrongDocument))
                            collection.Add(newStrongDocument);
                    }
                    while (Api.TryMoveNext(session, table));
                    return (TResult)collection;
                }

                if (methodExpression.Method.Name == "Count")
                {
                    int count = 0;
                    do
                    {
                        var newStrongDocument = (Document)Activator.CreateInstance(queryType);
                        populateDocument(newStrongDocument);
                        if (ExpressionAnalyser.Matches(methodExpression, newStrongDocument))
                            count++;
                    }
                    while (Api.TryMoveNext(session, table));
                    return (TResult)(object)count;
                }

                if (methodExpression.Method.Name == "Any")
                {
                    int count = 0;
                    do
                    {
                        var newStrongDocument = (Document)Activator.CreateInstance(queryType);
                        populateDocument(newStrongDocument);
                        if (ExpressionAnalyser.Matches(methodExpression.Arguments[1], newStrongDocument))
                            return (TResult) (object) true;
                    }
                    while (Api.TryMoveNext(session, table));
                    return (TResult)(object)false;
                }
                if (methodExpression.Method.Name == "All")
                {
                    do
                    {
                        var newStrongDocument = (Document)Activator.CreateInstance(queryType);
                        populateDocument(newStrongDocument);
                        if (!ExpressionAnalyser.Matches(methodExpression.Arguments[1], newStrongDocument))
                            return (TResult)(object)false;
                    }
                    while (Api.TryMoveNext(session, table));
                    return (TResult)(object)true;
                }
            }
            throw new NotImplementedException(methodExpression.Method.Name + " not yet supported");
        }
    }
}