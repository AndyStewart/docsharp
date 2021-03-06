using Microsoft.Isam.Esent.Interop;

namespace DocSharp.Storage
{
    public class DatabaseSchema
    {
        public static void Generate(Instance instance, string pathToDatabase)
        {
            using (var session = new Session(instance))
            {
                JET_DBID dbid;
                Api.JetCreateDatabase(session, pathToDatabase, null, out dbid, CreateDatabaseGrbit.None);
                using (var transaction = new Transaction(session))
                {
                    JET_TABLEID tableid;
                    Api.JetCreateTable(session, dbid, "Documents", 16, 100, out tableid);
                    CreateDocumentsTable(session, tableid);
                    Api.JetCloseTable(session, tableid);
                    transaction.Commit(CommitTransactionGrbit.None);
                }
                Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
            }
        }

        private static void CreateDocumentsTable(Session session, JET_TABLEID tableid)
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

            var collectionColumn = new JET_COLUMNDEF
                                       {
                                           cbMax = 1000,
                                           coltyp = JET_coltyp.Text,
                                           cp = JET_CP.Unicode,
                                           grbit = ColumndefGrbit.ColumnTagged
                                       };
            Api.JetAddColumn(session, tableid, "collection_name", collectionColumn, null, 0, out columnid);

            const string colectionIndexDef = "+collection_name\0\0";
            Api.JetCreateIndex(session, tableid, "by_collection_name", CreateIndexGrbit.None, colectionIndexDef, colectionIndexDef.Length, 100);

            var textColumn = new JET_COLUMNDEF
                                 {
                                     coltyp = JET_coltyp.LongText,
                                     grbit = ColumndefGrbit.ColumnTagged
                                 };
            Api.JetAddColumn(session, tableid, "data", textColumn, null, 0, out columnid);

            const string idIndexDef = "+id\0\0";
            Api.JetCreateIndex(session, tableid, "by_id", CreateIndexGrbit.IndexPrimary, idIndexDef, idIndexDef.Length, 100);
        }
    }
}