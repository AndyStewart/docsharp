using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests
{
    [TestFixture]
    public class DocSharpTests : BaseTest
    {
        [Test]
        public void Import_Paf_File()
        {
            var sqlCOnnection = new SqlConnection(@"Data Source=.\sql2005;Initial Catalog=PAF;Integrated Security=True");
            sqlCOnnection.Open();
            var command = new SqlCommand("Select Top 1000000 FullPostcode, SingleLineAddress FROM ExpandedAddress", sqlCOnnection);
            var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            var count = 0;
                
                using (var documentDb2 = new DocSharp(DbName))
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(count);

                        var address = new Address()
                                            {
                                                SingleLineAddress = (string) reader["SingleLineAddress"],
                                                PostCode = (string) reader["FullPostcode"]
                                            };
                        documentDb2.Store(address);
                        count++;
                    }
                }
            reader.Close();

            using (var documentDb2 = new DocSharp(DbName))
            {
                var docFound = new Document<Address>();
                Console.WriteLine(documentDb2.All<Address>().Count());
                Timer(() => docFound = documentDb2.All<Address>().First(q => q.Data.PostCode == "CH46 6HU"));
                Timer(() => docFound = documentDb2.All<Address>().First(q => q.Data.PostCode == "CH46 6HU"));
                Timer(() => docFound = documentDb2.All<Address>().First(q => q.Data.PostCode == "CH46 6HU"));
                Console.Write(docFound.Data.SingleLineAddress);
            }
    }

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
        public void Test_speed_of_insert()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                Timer(() =>
                          {
                              for (int i = 0; i < 1000; i++)
                              {
                                  documentDb.Store(new Company {Name = "Company Name " + i});
                              }
                          });
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

    public class Address
    {
        public string SingleLineAddress { get; set; }

        public string PostCode { get; set; }
    }
}