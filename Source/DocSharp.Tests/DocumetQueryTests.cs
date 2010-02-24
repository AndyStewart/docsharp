using System.Linq;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests
{
    [TestFixture]
    public class DocumetQueryTests : BaseTest
    {
        [Test]
        public void Should_return_first_result_with_string()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                var insertedDoc = documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name" });
                var result = documentDb.Query<Company>().First(q => q.Data.Name == "Company Name ");
                Assert.AreEqual(insertedDoc.Id, result.Id);
            }
        }

        [Test]
        public void Should_return_first_result_with_number()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                var insertedDoc = documentDb.Store(new Company { Name = "Company Name ", Phone= 123 });
                documentDb.Store(new Company { Name = "Company Name" });
                var result = documentDb.Query<Company>().First(q => q.Data.Phone == 123);
                Assert.AreEqual(insertedDoc.Id, result.Id);
            }
        }

        [Test]
        public void Should_return_list_of_companies()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                var result = documentDb.Query<Company>().Where(q => q.Data.Name == "Company Name ").ToList();
                Assert.AreEqual(result.Count, 3);
            }
        }

        [Test]
        public void Should_return_count_of_companies()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                var result = documentDb.Query<Company>().Count(q => q.Data.Name == "Company Name ");
                Assert.AreEqual(result, 3);
            }
        }

        [Test]
        public void Shouldnt_find_any_entites_that_match_any_query()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                Assert.IsFalse(documentDb.Query<Company>().Any(q => q.Data.Name == "Funky Dood"));
            }
        }

        [Test]
        public void Should_find_any_entites_that_match_any_query()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                Assert.IsTrue(documentDb.Query<Company>().Any(q => q.Data.Name == "Company Name "));
            }
        }

        [Test]
        public void Shouldnt_find_all_entites_that_match_any_query()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                Assert.IsFalse(documentDb.Query<Company>().All(q => q.Data.Name == "Funky Dood"));
            }
        }

        [Test]
        public void Should_find_all_entites_that_match_any_query()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                documentDb.Store(new Company { Name = "Company Name ", Phone = 123 });
                documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                Assert.IsTrue(documentDb.Query<Company>().All(q => q.Data.Name == "Company Name "));
            }
        }
    }
}