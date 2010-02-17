using System;
using System.IO;
using NUnit.Framework;

namespace DocSharp.Tests
{
    [TestFixture]
    public class DocumentDatabaseTests : BaseTest
    {
        [Test]
        public void Should_Create_document_db()
        {
            var documentDb = new DocumentDatabase(DbName);
            Assert.IsTrue(File.Exists(DbName));
        }
        
        [Test]
        public void Should_store_document()
        {
            var documentDb = new DocumentDatabase(DbName);
            var document = documentDb.Store<TestDocument>(new TestDocument() { Data = "Hello"});

            var db2 = new DocumentDatabase(DbName);
            var document2 = db2.Load<TestDocument>(document.Id);

            Assert.AreEqual(document2.Data.Data, document.Data.Data);
        }

        [Test]
        public void Should_store_1million_document()
        {
            var startTime = DateTime.Now;

            var documentDb = new DocumentDatabase(DbName);

            for (int i = 0; i < 1000000; i++)
            {
                var document = documentDb.Store<TestDocument>(new TestDocument() { Data = "Hello" });    
            }

            Console.WriteLine(DateTime.Now.Subtract(startTime).ToString());

//            var db2 = new DocumentDatabase(DbName);
//            var document2 = db2.Load<TestDocument>(document.Id);
//
//            Assert.AreEqual(document2.Data.Data, document.Data.Data);
        }
    }

    public class TestDocument
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
    }
}