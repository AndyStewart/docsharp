using System.Linq;
using NUnit.Framework;

namespace DocSharp.Tests
{
    [TestFixture]
    public class DocumetQueryTests : BaseTest
    {
        [Test]
        public void Should_return_first_result()
        {
            using (var documentDb = new DocSharp(DbName))
            {
                var insertedDoc = documentDb.Store(new Company { Name = "Company Name " });
                documentDb.Store(new Company { Name = "Company Name " });
                var result = documentDb.Query<Company>().First(q => q.Data.Name == "Company Name");
                Assert.AreEqual(insertedDoc.Id, result.Id);
            }
        }
    }
}