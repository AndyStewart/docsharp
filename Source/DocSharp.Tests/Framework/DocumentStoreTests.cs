using System;
using DocSharp.Framework;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests.Framework
{
    [TestFixture]
    public class DocumentStoreTests : BaseTest
    {
        [Test]
        public void Should_Load_entity_back_with_document_Id_mapped_to_Id()
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

        [Test]
        public void Should_map_Entity_Id_to_document_during_store()
        {
            using (var docSharp = new DocSharp(DbName))
            {
                var documentStore = new DocumentStore(docSharp);
                documentStore.Database = DbName;
                documentStore.AddMap(new CompanyMap());

                

                var mapper = new DocumentMapper(documentStore);
                var company = new Company() { Name = "Company 1" };
                mapper.Store(company);
                Assert.AreNotEqual(Guid.Empty, company.Id);
            }
        }
        
    }
}