using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests
{
    [TestFixture]
    public class DocSharpTests : BaseTest
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

            documentDb.Store(new Company() { Name = "My Company 221 " });
            
            var document = documentDb.Store(new Company() { Name = "My Company"});

            documentDb.Dispose();

            var db2 = new DocSharp(DbName);
            var document2 = db2.Load<Company>(document.Id);
            db2.Dispose();

            Assert.AreEqual(document2.Data.Name, document.Data.Name);
        }

        [Test]
        public void Should_delete_document()
        {
            var documentDb = new DocSharp(DbName);
            var document = documentDb.Store(new Company() { Name = "My Company" });
            documentDb.Delete(document.Id);
            documentDb.Dispose();


            using (var documentDb2 = new DocSharp(DbName))
            {
                var foundDocument = documentDb2.Load<Company>(document.Id);
                Assert.IsNull(foundDocument);
            }
        }

        [Test]
        public void Should_update_an_existing_document()
        {
            var documentDb = new DocSharp(DbName);
            var document = documentDb.Store(new Company { Name = "My Company" });
            document.Data.Name = "Updated";
            documentDb.Update(document);
            documentDb.Dispose();


            using(var documentDb2 = new DocSharp(DbName))
            {
                var foundDocument = documentDb2.Load<Company>(document.Id);
                Assert.AreEqual(foundDocument.Data.Name, "Updated");
            }
        }

        [Test]
        public void Should_store_and_retrieve_multiple_documents()
        {
            var documentDb = new DocSharp(DbName);
            var newDocument1 = documentDb.Store(new Company { Name = "My Company" });
            var newDocument2 = documentDb.Store(new Company { Name = "My Company2" });
            var newDocument3 = documentDb.Store(new Company { Name = "My Company3" });
            documentDb.Dispose();

            var db2 = new DocSharp(DbName);
            var storedDocument1 = db2.Load<Company>(newDocument1.Id);
            Assert.AreEqual(storedDocument1.Data.Name, newDocument1.Data.Name);

            var storedDocument2 = db2.Load<Company>(newDocument2.Id);
            Assert.AreEqual(storedDocument2.Data.Name, newDocument2.Data.Name);

            var storedDocument3 = db2.Load<Company>(newDocument3.Id);
            Assert.AreEqual(storedDocument3.Data.Name, newDocument3.Data.Name);
            db2.Dispose();
        }

        [Test]
        public void Should_query_for_object()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "My Company 1" });
                documentDb.Store(new Company { Name = "My Company 2" });
                documentDb.Store(new Company { Name = "My Company 3" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith"});
            }

            using (var documentDb = new DocSharp(DbName))
            {
                var documentsFound = documentDb.Query<Company>(q => q.Name.Contains("3"));
                Assert.AreEqual(1, documentsFound.Count);
            }
        }

        [Test]
        public void Should_Retrieve_all_objects_of_a_type()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "My Company 1" });
                documentDb.Store(new Company { Name = "My Company 2" });
                documentDb.Store(new Company { Name = "My Company 3" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith2" });
            }
            
            using (var documentDb = new DocSharp(DbName))
            {
                var startTime = DateTime.Now;
                Assert.AreEqual(documentDb.All<Company>().Count(), 3);
                Assert.AreEqual(documentDb.All<Contact>().Count(), 2);
                Console.WriteLine("Time - " + DateTime.Now.Subtract(startTime).TotalMilliseconds);
            }
        }

        [Test]
        public void Should_load_all_into_memory()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "My Company 1" });
                documentDb.Store(new Company { Name = "My Company 2" });
                documentDb.Store(new Company { Name = "My Company 3" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith2" });
            }

            using (var documentDb = new DocSharp(DbName))
            {
                Timer(() => documentDb.All<Company>().Count());
                var numberOfCompaniesFound = 0;
                var timeTake1 = Timer(() => numberOfCompaniesFound = documentDb.All<Company>().Count());
                Assert.IsTrue(timeTake1 < 50, "Second Query should be from memory");
                Assert.AreEqual(3, numberOfCompaniesFound, "Should return correct result set");
            }
        }

        [Test]
        public void Should_keep_memory_cache_in_sync_when_new_document_added()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "My Company 3 " + i });    
                }
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith2" });
            }

            using (var documentDb = new DocSharp(DbName))
            {
                Timer(() => documentDb.All<Company>().Count());
                var numberOfCompaniesFound = 0;
                documentDb.Store(new Company { Name = "My Company 3" });
                var timeTake1 = Timer(() => numberOfCompaniesFound  = documentDb.All<Company>().Count());

                Assert.IsTrue(timeTake1 < 50, "Second Query should be from memory");
                Assert.AreEqual(1001, numberOfCompaniesFound, "Should return correct result set");
            }
        }

        [Test]
        public void Should_keep_memory_cache_in_sync_when_document_updated()
        {
            Document<Company> doc;
            using (var documentDb = new DocSharp(DbName))
            {
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "My Company 3 " + i });
                }
                doc = documentDb.Store(new Company { Name = "My Company 3" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith2" });
            }

            using (var documentDb = new DocSharp(DbName))
            {
                Timer(() => documentDb.All<Company>().Count());
                var numberOfCompaniesFound = 0;
                doc.Data.Name = "Company 2";
                documentDb.Update(doc);

                var timeTake1 = Timer(() => numberOfCompaniesFound = documentDb.All<Company>().Count());

                Assert.IsTrue(timeTake1 < 50, "Second Query should be from memory");
                Assert.AreEqual("Company 2", documentDb.All<Company>().First(q => q.Id == doc.Id).Data.Name, "Should update company correctly");
            }
        }

        [Test]
        public void Should_keep_memory_cache_in_sync_when_document_deleted()
        {
            Document<Company> doc;
            using (var documentDb = new DocSharp(DbName))
            {
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "My Company 3 " + i });
                }
                doc = documentDb.Store(new Company { Name = "My Company 3" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith2" });
            }

            using (var documentDb = new DocSharp(DbName))
            {
                Timer(() => documentDb.All<Company>().Count());
                var numberOfCompaniesFound = 0;
                doc.Data.Name = "Company 2";
                documentDb.Delete(doc.Id);

                var timeTake1 = Timer(() => numberOfCompaniesFound = documentDb.All<Company>().Count());

                Assert.IsTrue(timeTake1 < 50, "Second Query should be from memory");
                Assert.AreEqual(1000, documentDb.All<Company>().Count(), "Should of removed deleted object from cache");
            }
        }

        [Test]
        public void Test_Speed_of_look_up_by_id()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                for (int i = 0; i < 5000; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name" });
                }

                var document = documentDb.Store(new Contact { FirstName = "Andrew N" });

                for (int i = 0; i < 5000; i++)
                {
                    documentDb.Store(new Company { Name = "Andrew N" });
                }

                var startTime = DateTime.Now;
                var documentsFound = documentDb.Load<Contact>(document.Id);
                var timeQueryTaken = DateTime.Now.Subtract(startTime);
                Console.WriteLine("Load by Id Time - " + DateTime.Now.Subtract(startTime));
                Assert.AreEqual(documentsFound.Data.FirstName, "Andrew N");
                Assert.IsTrue(timeQueryTaken.TotalMilliseconds <= 500);
            }
        }

        [Test]
        public void Test_speed_of_query()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name " + i });
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Contact { FirstName = "Andrew", Surname = "Stewart" });
                }

                for (int i = 0; i < 500; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name World" + i });
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name " + i });
                }

                Timer(() => documentDb.Query<Company>(q => q.Name.Contains("World")));
                Timer(() => documentDb.Query<Company>(q => q.Name.Contains("World")));

                var startTime = DateTime.Now;
                var documentsFound = documentDb.Query<Company>(q => q.Name.Contains("World"));
                var timeQueryTaken = DateTime.Now.Subtract(startTime);
                Console.WriteLine("Query Time (ms)- " + DateTime.Now.Subtract(startTime).TotalMilliseconds);
                Assert.AreEqual(500, documentsFound.Count);
                Assert.IsTrue(timeQueryTaken.TotalMilliseconds <= 3000);
            }
        }

        [Test]
        public void Test_speed_of_query_with_index()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Index<Company>(q => q.Name);

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name " + i});
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Contact { FirstName = "Andrew", Surname = "Stewart"});
                }

                for (int i = 0; i < 500; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name World" + i });
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Contact { FirstName = "Bob", Surname = "Smith" });
                }

                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company { Name = "Company Name " + i });
                }

                var startTime = DateTime.Now;
                var documentsFound = documentDb.Query<Company>(q => q.Name.Contains("World"));
                var timeQueryTaken = DateTime.Now.Subtract(startTime);
                Console.WriteLine("Query Time (ms)- " + DateTime.Now.Subtract(startTime).TotalMilliseconds);
                Assert.AreEqual(500, documentsFound.Count);
                Assert.IsTrue(timeQueryTaken.TotalMilliseconds <= 500);
            }
        }

        [Test, Ignore] // 1000 =  14 secs -- target records to han13,241,930
        public void Should_store_1000_document()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                var startTime = DateTime.Now;
                for (int i = 0; i < 1000; i++)
                {
                    documentDb.Store(new Company() { Name = "Hello" });
                }
                Console.WriteLine(DateTime.Now.Subtract(startTime).ToString());
            }
        }
    }
}