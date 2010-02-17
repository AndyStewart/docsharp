using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Newtonsoft.Json;

namespace DocSharp
{
    public class DocumentDatabase
    {
        private readonly string _pathToDatabase;

        public DocumentDatabase(string pathToDatabase)
        {
            _pathToDatabase = pathToDatabase;

            if (!File.Exists(_pathToDatabase))
                createDatabase();
        }

        private void createDatabase()
        {
            using (var instance = new Instance("createdatabase"))
            {
                instance.Init();
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetCreateDatabase(session, _pathToDatabase, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                    using (var transaction = new Transaction(session))
                    {
                        // A newly created table is opened exclusively. This is necessary to add
                        // a primary index to the table (a primary index can only be added if the table
                        // is empty and opened exclusively). Columns and indexes can be added to a 
                        // table which is opened normally.
                        JET_TABLEID tableid;
                        Api.JetCreateTable(session, dbid, "Documents", 16, 100, out tableid);
                        CreateDocumentsTable(session, tableid);
                        Api.JetCloseTable(session, tableid);

                        // Lazily commit the transaction. Normally committing a transaction forces the
                        // associated log records to be flushed to disk, so the commit has to wait for
                        // the I/O to complete. Using the LazyFlush option means that the log records
                        // are kept in memory and will be flushed later. This will preserve transaction
                        // atomicity (all operations in the transaction will either happen or be rolled
                        // back) but will not preserve durability (a crash after the commit call may
                        // result in the transaction updates being lost). Lazy transaction commits are
                        // considerably faster though, as they don't have to wait for an I/O.
                        transaction.Commit(CommitTransactionGrbit.LazyFlush);
                    }
                }
            }
        }

        private void CreateDocumentsTable(Session session, JET_TABLEID tableid)
        {
            JET_COLUMNID columnid;

            var guidColumn = new JET_COLUMNDEF
                                {
                                    cbMax = 255,
                                    coltyp = JET_coltyp.Text,
                                    cp = JET_CP.Unicode,
                                    grbit = ColumndefGrbit.ColumnTagged
                                };
            Api.JetAddColumn(session, tableid, "id", guidColumn, null, 0, out columnid);

            var textColumn = new JET_COLUMNDEF
                                {
                                    coltyp = JET_coltyp.LongText,
                                    grbit = ColumndefGrbit.ColumnTagged
                                };
            Api.JetAddColumn(session, tableid, "data", textColumn, null, 0, out columnid);

            const string indexDef = "+id\0\0";
            Api.JetCreateIndex(session, tableid, "by_id", CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length,
                               100);
        }


        public void Delete()
        {
            File.Delete(_pathToDatabase);
        }

        public Document<T> Store<T>(object document)
        {
            var newDocument = new Document<T>() {Id = Guid.NewGuid(), Data = (T) document };
            using (var instance = new Instance("stocksample"))
            {
                instance.Parameters.CircularLog = true;
                instance.Init();
                // Create a disposable wrapper around the JET_SESID.
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetAttachDatabase(session, _pathToDatabase, AttachDatabaseGrbit.None);
                    Api.JetOpenDatabase(session, _pathToDatabase, null, out dbid, OpenDatabaseGrbit.None);
                    using (var table = new Table(session, dbid, "Documents", OpenTableGrbit.None))
                    {
                        using (var transaction = new Transaction(session))
                        {

                            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(session, table);
                            var columnId = columnids["Id"];
                            var columnData = columnids["data"];

                            using (var update = new Update(session, table, JET_prep.Insert))
                            {
                                Api.SetColumn(session, table, columnId, newDocument.Id.ToString(), Encoding.Unicode);
                                Api.SetColumn(session, table, columnData, Serialize(newDocument.Data), Encoding.Unicode);
                                update.Save();
                            }
                            transaction.Commit(CommitTransactionGrbit.None);
                        }
                    }
                }
            }

            return newDocument;
        }

        private string Serialize(object document)
        {
            var serializer = JsonSerializer.Create(new JsonSerializerSettings());
            var data = new StringWriter();
            serializer.Serialize(data, document);
            return data.ToString();
        }

        public Document<T> Load<T>(Guid guid)
        {
            using (var instance = new Instance("stocksample"))
            {
                instance.Parameters.CircularLog = true;
                instance.Init();
                // Create a disposable wrapper around the JET_SESID.
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetAttachDatabase(session, _pathToDatabase, AttachDatabaseGrbit.None);
                    Api.JetOpenDatabase(session, _pathToDatabase, null, out dbid, OpenDatabaseGrbit.None);
                    using (var table = new Table(session, dbid, "Documents", OpenTableGrbit.None))
                    {
                        using (var transaction = new Transaction(session))
                        {
                            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(session, table);
                            var columnId = columnids["Id"];
                            var columnData = columnids["data"];

                            Api.JetSetCurrentIndex(session, table, null);
                            Api.MakeKey(session, table, guid.ToString(), Encoding.Unicode, MakeKeyGrbit.NewKey);
                            Api.JetSeek(session, table, SeekGrbit.SeekEQ);

                            var documentFound = new Document<T>();
                            documentFound.Id = new Guid(Api.RetrieveColumnAsString(session, table, columnId));
                            documentFound.Data= DeSerialize<T>(Api.RetrieveColumnAsString(session, table, columnData));
                            
                            return documentFound;
                        }
                    }
                }
            }
        }

        private T DeSerialize<T>(string data)
        {
            var serializer = JsonSerializer.Create(new JsonSerializerSettings());
            var dataReader = new StringReader(data);
            return (T)serializer.Deserialize(dataReader, typeof(T));
        }
    }
}