using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace DocSharp
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
                transaction.Commit(CommitTransactionGrbit.None);
                transaction.Dispose();
            }

            if (table != null)
                table.Dispose();

            if (session != null)
                session.Dispose();
        }

        public void Insert<T>(Document<T> document)
        {
            using (var update = new Update(session, table, JET_prep.Insert))
            {
                Api.SetColumn(session, table, columnId, document.Id.ToString(), Encoding.Unicode);
                Api.SetColumn(session, table, columnCollectionName, getCollectionName<T>(), Encoding.Unicode);
                Api.SetColumn(session, table, columnData, ObjectConverter.ToJson(document.Data), Encoding.Unicode);
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

        public void Update<T>(Document<T> document)
        {
            Api.JetSetCurrentIndex(session, table, null);
            Api.MakeKey(session, table, document.Id.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(session, table, SeekGrbit.SeekEQ);
            using (var update = new Update(session, table, JET_prep.Replace))
            {
                Api.SetColumn(session, table, columnId, document.Id.ToString(), Encoding.Unicode);
                Api.SetColumn(session, table, columnData, ObjectConverter.ToJson(document.Data), Encoding.Unicode);
                update.Save();
            }
        }

        public IList<Document<T>> Query<T>(Func<T, bool> whereClause)
        {
            var listFound = new List<Document<T>>();
            Api.JetSetCurrentIndex(session, table, null);
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
    }
}