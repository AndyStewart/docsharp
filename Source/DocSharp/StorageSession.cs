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
                Api.SetColumn(session, table, columnData, ObjectConverter.ToJson(document.Data), Encoding.Unicode);
                update.Save();
            }
        }

        public Document<T> Load<T>(Guid guid)
        {
            Api.JetSetCurrentIndex(session, table, null);
            Api.MakeKey(session, table, guid.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(session, table, SeekGrbit.SeekEQ);

            var documentFound = new Document<T>();
            documentFound.Id = new Guid(Api.RetrieveColumnAsString(session, table, columnId));
            documentFound.Data = ObjectConverter.ToObject<T>(Api.RetrieveColumnAsString(session, table, columnData));
            return documentFound;
        }
    }
}