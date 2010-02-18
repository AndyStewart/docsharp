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
            var documentDb = new DocSharp(DbName);
            Assert.IsTrue(File.Exists(DbName));
            documentDb .Dispose();
        }
        
        [Test]
        public void Should_store_document()
        {
            var documentDb = new DocSharp(DbName);
            var document = documentDb.Store(new TestDocument() { Data = "Hello"});
            documentDb.Dispose();

            var db2 = new DocSharp(DbName);
            var document2 = db2.Load<TestDocument>(document.Id);
            db2.Dispose();

            Assert.AreEqual(document2.Data.Data, document.Data.Data);
        }

        [Test]
        public void Should_delete_document()
        {
            var documentDb = new DocSharp(DbName);
            var document = documentDb.Store(new TestDocument() { Data = "Hello" });
            documentDb.Delete(document.Id);
            documentDb.Dispose();


            using (var documentDb2 = new DocSharp(DbName))
            {
                var foundDocument = documentDb2.Load<TestDocument>(document.Id);
                Assert.IsNull(foundDocument);
            }
        }

        [Test]
        public void Should_update_an_existing_document()
        {
            var documentDb = new DocSharp(DbName);
            var document = documentDb.Store(new TestDocument() { Data = "Hello" });
            document.Data.Data = "Updated";
            documentDb.Update(document);
            documentDb.Dispose();


            using(var documentDb2 = new DocSharp(DbName))
            {
                var foundDocument = documentDb2.Load<TestDocument>(document.Id);
                Assert.AreEqual(foundDocument.Data.Data, "Updated");
            }
        }


        [Test]
        public void Should_store_and_retrieve_multiple_documents()
        {
            var documentDb = new DocSharp(DbName);
            var newDocument1 = documentDb.Store(new TestDocument { Data = "Hello" });
            var newDocument2 = documentDb.Store(new TestDocument { Data = "Hello2" });
            var newDocument3 = documentDb.Store(new TestDocument { Data = "Hello3" });
            documentDb.Dispose();

            var db2 = new DocSharp(DbName);
            var storedDocument1 = db2.Load<TestDocument>(newDocument1.Id);
            Assert.AreEqual(storedDocument1.Data.Data, newDocument1.Data.Data);

            var storedDocument2 = db2.Load<TestDocument>(newDocument2.Id);
            Assert.AreEqual(storedDocument2.Data.Data, newDocument2.Data.Data);

            var storedDocument3 = db2.Load<TestDocument>(newDocument3.Id);
            Assert.AreEqual(storedDocument3.Data.Data, newDocument3.Data.Data);
            db2.Dispose();
        }

        [Test] // 1000 =  14 secs -- target records to han13,241,930
        public void Should_store_1000_document()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                var startTime = DateTime.Now;
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new TestDocument() { Data = "Hello" });
                }
                Console.WriteLine(DateTime.Now.Subtract(startTime).ToString());
            }
        }
    }

    public class TestDocument
    {
        public Guid Id { get; set; }
        public string Data { get; set; }
    }
}