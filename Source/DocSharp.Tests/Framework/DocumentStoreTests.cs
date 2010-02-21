using DocSharp.Framework;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests.Framework
{
    [TestFixture]
    public class DocumentStoreTests : BaseTest
    {
        [Test]
        public void Should_store_map_Id_to_document_Id()
        {
            using (var docSharp = new DocSharp(DbName))
            {
                var documentStore = new DocumentStore(docSharp);
                documentStore.Database = DbName;
                documentStore.AddMap(new CompanyMap());
                var documentId = docSharp.Store(new Company { Name = "Company NAme"});

                var mapper = new DocumentMapper(documentStore);
                var companyFound = mapper.Load<Company>(documentId.Id);
                Assert.AreEqual(companyFound.Id, documentId.Id);
            }
        }
        
    }
}