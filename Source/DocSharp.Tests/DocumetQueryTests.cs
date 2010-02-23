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
    }
}