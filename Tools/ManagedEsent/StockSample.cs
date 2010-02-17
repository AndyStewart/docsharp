﻿//-----------------------------------------------------------------------
// <copyright file="StockSample.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace CsStockSample
{
    /// <summary>
    /// Create a sample database containing some stock prices and
    /// perform some basic queries.
    /// </summary>
    public class StockSample
    {
        /// <summary>
        /// Name of the table that will store the prices.
        /// </summary>
        private const string TableName = "stocks";

        /// <summary>
        /// Columnid of the stock symbol.
        /// </summary>
        private static JET_COLUMNID columnidSymbol;

        /// <summary>
        /// Columnid of the company name.
        /// </summary>
        private static JET_COLUMNID columnidName;

        /// <summary>
        /// Columnid of the stock price.
        /// </summary>
        private static JET_COLUMNID columnidPrice;

        /// <summary>
        /// Columnid of the number of shares owned.
        /// </summary>
        private static JET_COLUMNID columnidShares;

        /// <summary>
        /// Called on program startup.
        /// </summary>
        /// <param name="args">Arguments to the program. Ignored.</param>
        public static void Main(string[] args)
        {
            Console.WriteLine();

            const string DatabaseName = "stocksample.edb";

            // First create the database. A real application would probably
            // check for the database first and only create it if needed.
            // Checking for the database can be done by calling JetAttachDatabase
            // and seeing if a JET_ERR.DatabaseNotFound error is thrown.
            // (Check the Error property of the EsentErrorException).
            CreateDatabase(DatabaseName);

            // Now the database has been created we can attach to it
            // An instance object is disposable so the underlying esent resource
            // can be freed. The disposable objects wrapped around esent resources
            // _must_ be disposed properly. Esent resources must be freed in a
            // specific order, which the finalizer doesn't necessarily respect.
            using (var instance = new Instance("stocksample"))
            {
                // Creating an Instance object doesn't call JetInit. 
                // This is done to allow some parameters to be set
                // before the instance is initialized.

                // Circular logging is very useful, causing logfiles that are
                // no longer needed are automatically deleted. Most applications
                // should use it.
                instance.Parameters.CircularLog = true;

                // Initialize the instance. This creates the logs and temporary database.
                // If logs are present in the log directory then recovery will run
                // automatically.
                instance.Init();

                // Create a disposable wrapper around the JET_SESID.
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;

                    // The database only has to be attached once per instance, but each
                    // session has to open the database. Redundant JetAttachDatabase calls
                    // are safe to make though.
                    // Here we use the fact that Instance, Session and Table object all have
                    // implicit conversions to the underlying JET_* types. This allows the
                    // disposable wrappers to be used with any APIs that expect the JET_*
                    // structures.
                    Api.JetAttachDatabase(session, DatabaseName, AttachDatabaseGrbit.None);
                    Api.JetOpenDatabase(session, DatabaseName, null, out dbid, OpenDatabaseGrbit.None);

                    // Create a disposable wrapper around the JET_TABLEID.
                    using (var table = new Table(session, dbid, TableName, OpenTableGrbit.None))
                    {
                        // Load the columnids from the table. This should be done each time the database is attached
                        // as an offline defrag (esentutl /d) can change the name => columnid mapping.
                        IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(session, table);
                        columnidSymbol = columnids["symbol"];
                        columnidName = columnids["name"];
                        columnidPrice = columnids["price"];
                        columnidShares = columnids["shares_owned"];

                        // Dump records by the primary index, this will be stock symbol order
                        Console.WriteLine("** Populating the table");
                        PopulateTable(session, table);
                        DumpByIndex(session, table, null);

                        // The shares index is sparse, it only contains records where the
                        // shares_owned column is non null.
                        Console.WriteLine("** Owned shares");
                        DumpByIndex(session, table, "shares");

                        // Use the price index to find stocks sorted by price
                        Console.WriteLine("** Sorted by price");
                        DumpByIndex(session, table, "price");

                        // Seek and update an existing record
                        Console.WriteLine("** Updating owned shares");
                        BuyShares(session, table, "SBUX", 50);
                        BuyShares(session, table, "MSFT", 100);
                        DumpByIndex(session, table, "shares");

                        // Delete a record
                        Console.WriteLine("** Deleting EBAY");
                        DeleteStock(session, table, "EBAY");
                        DumpByIndex(session, table, "name");

                        // Create an index range over a prefix
                        Console.WriteLine("** Company names starting with 'app'");
                        DumpByNamePrefix(session, table, "app");

                        // An empty index range
                        Console.WriteLine("** Company names starting with 'xyz'");
                        DumpByNamePrefix(session, table, "xyz");

                        // An index range over multiple records
                        Console.WriteLine("** Company names starting with 'm'");
                        DumpByNamePrefix(session, table, "m");

                        // Move to the start of an index
                        Console.WriteLine("** Lowest price");
                        Api.JetSetCurrentIndex(session, table, "price");
                        Api.JetMove(session, table, JET_Move.First, MoveGrbit.None);
                        PrintOneRow(session, table);
                        Console.WriteLine();

                        // Move to the end of an index
                        Console.WriteLine("** Highest price");
                        Api.JetSetCurrentIndex(session, table, "price");
                        Api.JetMove(session, table, JET_Move.Last, MoveGrbit.None);
                        PrintOneRow(session, table);
                        Console.WriteLine();

                        // Create a range between two values
                        Console.WriteLine("** Prices fom $10-$20");
                        DumpPriceRange(session, table, 1000, 2000);

                        // Use intersect indexes to find records that meet multiple criteria
                        Console.WriteLine("** Select by name and price");
                        IntersectIndexes(session, table, "Apple", "Pear", 1000, 2000);
                    }
                }
            }
        }

        /// <summary>
        /// Dump all records from the index.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump.</param>
        /// <param name="index">The index to use.</param>
        private static void DumpByIndex(JET_SESID sesid, JET_TABLEID tableid, string index)
        {
            Api.JetSetCurrentIndex(sesid, tableid, index);
            PrintAllRecords(sesid, tableid);
            Console.WriteLine();
        }

        /// <summary>
        /// Populate the stock table with a set of symbols and prices.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to use.</param>
        private static void PopulateTable(JET_SESID sesid, JET_TABLEID tableid)
        {
            // Create a disposable wrapper around JetBeginTransaction and JetCommitTransaction
            using (var transaction = new Transaction(sesid))
            {
                InsertOneStock(sesid, tableid, "SBUX", "Starbucks Corp.", 988, 0);
                InsertOneStock(sesid, tableid, "MSFT", "Microsoft Corp.", 1965, 200);
                InsertOneStock(sesid, tableid, "AAPL", "Apple Inc.", 9000, 0);
                InsertOneStock(sesid, tableid, "GOOG", "Google Inc.", 31017, 0);
                InsertOneStock(sesid, tableid, "M", "Macy's Inc.", 1062, 0);
                InsertOneStock(sesid, tableid, "MCD", "MacDonald's Corp.", 1062, 0);
                InsertOneStock(sesid, tableid, "GE", "General Electric Company", 1650, 0);
                InsertOneStock(sesid, tableid, "MMM", "3M Company", 5662, 100);
                InsertOneStock(sesid, tableid, "IBM", "International Business Machines Corp.", 8352, 150);
                InsertOneStock(sesid, tableid, "EBAY", "eBay Inc.", 1445, 0);

                // Commit the transaction at the end of the using block
                // If transaction.Commit isn't called then the transaction will 
                // automatically rollback when disposed (throwing away
                // the records that were just inserted).
                transaction.Commit(CommitTransactionGrbit.None);
            }
        }

        /// <summary>
        /// Insert information about one stock into the table.
        /// </summary>
        /// <remarks>The session must already be in a transaction.</remarks>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to insert into.</param>
        /// <param name="symbol">The stock symbol.</param>
        /// <param name="name">The name of the company.</param>
        /// <param name="price">The current price.</param>
        /// <param name="sharesOwned">Number of shares owned.</param>
        private static void InsertOneStock(
            JET_SESID sesid,
            JET_TABLEID tableid,
            string symbol,
            string name,
            int price,
            int sharesOwned)
        {
            // Prepare an update, set some columns and the save the update
            // First, create a disposable wrapper around JetPrepareUpdate and JetUpdate
            using (var update = new Update(sesid, tableid, JET_prep.Insert))
            {
                Api.SetColumn(sesid, tableid, columnidSymbol, symbol, Encoding.Unicode);
                Api.SetColumn(sesid, tableid, columnidName, name, Encoding.Unicode);
                Api.SetColumn(sesid, tableid, columnidPrice, price);

                // The column defaults to null. Only set it if we have some shares.
                if (0 != sharesOwned)
                {
                    Api.SetColumn(sesid, tableid, columnidShares, sharesOwned);
                }

                // Save the update at the end of the using block.
                // If update.Save isn't called then the update will 
                // be cancelled when disposed (and the record won't
                // be inserted).
                update.Save();
            }
        }

        /// <summary>
        /// Increment the number of shares.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to insert into.</param>
        /// <param name="symbol">The stock symbol to buy.</param>
        /// <param name="shares">The number of shares to buy.</param>
        private static void BuyShares(
            JET_SESID sesid,
            JET_TABLEID tableid,
            string symbol,
            int shares)
        {
            // Get the current value of the column, prepare an update,
            // set the column to the new value and the save the update
            using (var transaction = new Transaction(sesid))
            {
                SeekToSymbol(sesid, tableid, symbol);
                int currentShares = Api.RetrieveColumnAsInt32(sesid, tableid, columnidShares).GetValueOrDefault();
                int newShares = currentShares + shares;

                using (var update = new Update(sesid, tableid, JET_prep.Replace))
                {
                    Api.SetColumn(sesid, tableid, columnidShares, newShares);

                    // Save the update at the end of the using block.
                    // If update.Save isn't called then the update will 
                    // be cancelled when disposed (undoing the updates 
                    // to the record).
                    update.Save();
                }

                // Commit the transaction at the end of the using block
                // If transaction.Commit isn't called then the transaction will 
                // automatically rollback when disposed (throwing away
                // the changes to the record).
                transaction.Commit(CommitTransactionGrbit.None);
            }
        }

        /// <summary>
        /// Delete the stock with the given symbol.
        /// </summary>
        /// <param name="sesid">The session to ue.</param>
        /// <param name="tableid">The table cursor to use.</param>
        /// <param name="symbol">The symbol to delete.</param>
        private static void DeleteStock(JET_SESID sesid, JET_TABLEID tableid, string symbol)
        {
            // Seek to the record and delete it.
            using (var transaction = new Transaction(sesid))
            {
                SeekToSymbol(sesid, tableid, symbol);
                Api.JetDelete(sesid, tableid);

                // Commit the transaction at the end of the using block
                // If transaction.Commit isn't called then the transaction will 
                // automatically rollback when disposed (undeleting the record).
                transaction.Commit(CommitTransactionGrbit.None);
            }
        }

        /// <summary>
        /// Find the record with the given stock symbol.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump the records from.</param>
        /// <param name="symbol">The symbol to seek for.</param>
        private static void SeekToSymbol(JET_SESID sesid, JET_TABLEID tableid, string symbol)
        {
            // We need to be on the primary index (which is over the 'symbol' column)
            Api.JetSetCurrentIndex(sesid, tableid, null);
            Api.MakeKey(sesid, tableid, symbol, Encoding.Unicode, MakeKeyGrbit.NewKey);

            // This seek expects the record to be present. To test for a record
            // use TrySeek(), which won't throw an exception if the record isn't
            // found.
            Api.JetSeek(sesid, tableid, SeekGrbit.SeekEQ);
        }

        /// <summary>
        /// Dump all records from the index whose name has the given prefix.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump.</param>
        /// <param name="namePrefix">The prefix to use.</param>
        private static void DumpByNamePrefix(JET_SESID sesid, JET_TABLEID tableid, string namePrefix)
        {
            // Set up an index range on the name index.
            Api.JetSetCurrentIndex(sesid, tableid, "name");

            // First, seek to the beginning of the range
            Api.MakeKey(sesid, tableid, namePrefix, Encoding.Unicode, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(sesid, tableid, SeekGrbit.SeekGE))
            {
                // We have at least one record. Now create the end of the index range.
                Api.MakeKey(sesid, tableid, namePrefix, Encoding.Unicode, MakeKeyGrbit.NewKey | MakeKeyGrbit.SubStrLimit);
                Api.JetSetIndexRange(sesid, tableid, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive);
                
                // there are records in the range. we can now iterate through the range.
                // when the end of the range is hit we will get a 'no more records' error and
                // the range will be removed (so subsequent moves will go to the end of the table)
                PrintRecordsToEnd(sesid, tableid);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Dump all records from the index whose price is in the given range.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump.</param>
        /// <param name="low">The low end of the price range.</param>
        /// <param name="high">The high end of the price range.</param>
        private static void DumpPriceRange(JET_SESID sesid, JET_TABLEID tableid, int low, int high)
        {
            // Set up an index range on the name index.
            Api.JetSetCurrentIndex(sesid, tableid, "price");

            // First, seek to the beginning of the range
            Api.MakeKey(sesid, tableid, low, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(sesid, tableid, SeekGrbit.SeekGE))
            {
                // We have at least one record. Now create the (exclusive) end of the index range.
                Api.MakeKey(sesid, tableid, high, MakeKeyGrbit.NewKey);
                Api.JetSetIndexRange(sesid, tableid, SetIndexRangeGrbit.RangeUpperLimit);

                // there are records in the range. we can now iterate through the range.
                // when the end of the range is hit we will get a 'no more records' error and
                // the range will be removed (so subsequent moves will go to the end of the table)
                PrintRecordsToEnd(sesid, tableid);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Print records for companies whose name is between firstName and lastName and whose
        /// price is between lowPrice and highPrice. This is done by intersecting two different
        /// indexes, which can be an efficient way of finding records that meet multiple criteria.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to use.</param>
        /// <param name="fromName">The first name of the companies to dump.</param>
        /// <param name="toName">The last name of the companies to dump.</param>
        /// <param name="lowPrice">The lowest price to dump.</param>
        /// <param name="highPrice">The highest price to dump.</param>
        private static void IntersectIndexes(
            JET_SESID sesid,
            JET_TABLEID tableid,
            string fromName,
            string toName,
            int lowPrice,
            int highPrice)
        {
            // We need one cursor for each index
            JET_TABLEID nameIndex;
            Api.JetDupCursor(sesid, tableid, out nameIndex, DupCursorGrbit.None);
            Api.JetSetCurrentIndex(sesid, nameIndex, "name");

            JET_TABLEID priceIndex;
            Api.JetDupCursor(sesid, tableid, out priceIndex, DupCursorGrbit.None);
            Api.JetSetCurrentIndex(sesid, priceIndex, "price");

            // Set the index range on the name index
            Api.MakeKey(sesid, nameIndex, fromName, Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSeek(sesid, nameIndex, SeekGrbit.SeekLE);
            Api.MakeKey(sesid, nameIndex, toName, Encoding.Unicode, MakeKeyGrbit.NewKey);
            Api.JetSetIndexRange(sesid, nameIndex, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit);

            // Set the index range on the prices
            Api.MakeKey(sesid, priceIndex, lowPrice, MakeKeyGrbit.NewKey);
            Api.JetSeek(sesid, priceIndex, SeekGrbit.SeekLE);
            Api.MakeKey(sesid, priceIndex, highPrice, MakeKeyGrbit.NewKey);
            Api.JetSetIndexRange(sesid, priceIndex, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit);

            // This call will generate a set of bookmarks. The bookmarks are
            // returned in primary key (i.e. symbol) order.
            foreach (byte[] bookmark in Api.IntersectIndexes(sesid, nameIndex, priceIndex))
            {
                Api.JetGotoBookmark(sesid, tableid, bookmark, bookmark.Length);
                PrintOneRow(sesid, tableid);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Print all rows in the table.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump the records from.</param>
        private static void PrintAllRecords(JET_SESID sesid, JET_TABLEID tableid)
        {
            if (Api.TryMoveFirst(sesid, tableid))
            {
                PrintRecordsToEnd(sesid, tableid);
            }
        }

        /// <summary>
        /// Print records from the current point to the end of the table.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to dump the records from.</param>
        private static void PrintRecordsToEnd(JET_SESID sesid, JET_TABLEID tableid)
        {
                do
                {
                    PrintOneRow(sesid, tableid);
                }
                while (Api.TryMoveNext(sesid, tableid));
        }

        /// <summary>
        /// Print the current record.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">The table to use.</param>
        private static void PrintOneRow(JET_SESID sesid, JET_TABLEID tableid)
        {
            string symbol = Api.RetrieveColumnAsString(sesid, tableid, columnidSymbol);
            string name = Api.RetrieveColumnAsString(sesid, tableid, columnidName);

            // this column can't be null so we cast to an int
            var price = (int)Api.RetrieveColumnAsInt32(sesid, tableid, columnidPrice);

            // this column can be null so we keep the nullable type
            int? shares = Api.RetrieveColumnAsInt32(sesid, tableid, columnidShares);

            Console.Write("\t{0,-40} {1,-4} ${2,-6}", name, symbol, price / 100.0);
            if (shares.HasValue)
            {
                Console.Write(" {0} shares", shares);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Create a new database. Create the table, columns and indexes.
        /// </summary>
        /// <param name="database">Name of the database to create.</param>
        private static void CreateDatabase(string database)
        {
            using (var instance = new Instance("createdatabase"))
            {
                instance.Init();
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetCreateDatabase(session, database, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                    using (var transaction = new Transaction(session))
                    {
                        // A newly created table is opened exclusively. This is necessary to add
                        // a primary index to the table (a primary index can only be added if the table
                        // is empty and opened exclusively). Columns and indexes can be added to a 
                        // table which is opened normally.
                        JET_TABLEID tableid;
                        Api.JetCreateTable(session, dbid, TableName, 16, 100, out tableid);
                        CreateColumnsAndIndexes(session, tableid);
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

        /// <summary>
        /// Setup the meta-data for the given table.
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">
        /// The table to add the columns/indexes to. This table must be opened exclusively.
        /// </param>
        private static void CreateColumnsAndIndexes(JET_SESID sesid, JET_TABLEID tableid)
        {
            using (var transaction = new Transaction(sesid))
            {
                JET_COLUMNID columnid;

                // Stock symbol : text column
                var columndef = new JET_COLUMNDEF
                                      {
                                          coltyp = JET_coltyp.LongText,
                                          cp = JET_CP.Unicode
                                      };

                Api.JetAddColumn(sesid, tableid, "symbol", columndef, null, 0, out columnid);

                // Name of the company : text column
                Api.JetAddColumn(sesid, tableid, "name", columndef, null, 0, out columnid);

                // Current price, stored in cents : 32-bit integer
                columndef = new JET_COLUMNDEF
                    {
                        coltyp = JET_coltyp.Long,

                        // Be careful with ColumndefGrbit.ColumnNotNULL. Older versions of ESENT
                        // (e.g. Windows XP) do not support this grbit for tagged or variable columns
                        // (JET_coltyp.Text, JET_coltyp.LongText, JET_coltyp.Binary, JET_coltyp.LongBinary)
                        grbit = ColumndefGrbit.ColumnNotNULL
                    };

                Api.JetAddColumn(sesid, tableid, "price", columndef, null, 0, out columnid);

                // Number of shares owned (this column may be null) : 32-bit integer
                columndef.grbit = ColumndefGrbit.None;
                Api.JetAddColumn(sesid, tableid, "shares_owned", columndef, null, 0, out columnid);

                // The primary index is the stock symbol.
                string indexDef = "+symbol\0\0";
                Api.JetCreateIndex(sesid, tableid, "primary", CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);

                // An index on the company name
                // There should be only one company with a given name, so the index is unique.
                indexDef = "+name\0+symbol\0\0";
                Api.JetCreateIndex(sesid, tableid, "name", CreateIndexGrbit.IndexUnique, indexDef, indexDef.Length, 100);

                // An index on the price
                indexDef = "+price\0\0";
                Api.JetCreateIndex(sesid, tableid, "price", CreateIndexGrbit.None, indexDef, indexDef.Length, 100);

                // An index on the number of shares owned. This index is sparse.
                // We only want this index to contain entries for stocks that are actually owned so we use the IndexIgnoreAnyNull
                // option to prevent entries from being generated for any record where any of the columns in the index are null.
                // By setting the 'shares_owned' column to null we can hide records from this index.
                indexDef = "+name\0+shares_owned\0\0";
                Api.JetCreateIndex(sesid, tableid, "shares", CreateIndexGrbit.IndexIgnoreAnyNull, indexDef, indexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }
    }
}
