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

                var mapper = new DocumentSession(documentStore);
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

                

                var mapper = new DocumentSession(documentStore);
                var company = new Company() { Name = "Company 1" };
                mapper.Store(company);
                Assert.AreNotEqual(Guid.Empty, company.Id);
            }
        }

        [Test]
        public void Should_update_stored_entity()
        {
            using (var docSharp = new DocSharp(DbName))
            {
                var documentStore = new DocumentStore(docSharp);
                documentStore.Database = DbName;
                documentStore.AddMap(new CompanyMap());


                var mapper = new DocumentSession(documentStore);
                var company = new Company { Name = "Company 1" };
                mapper.Store(company);
                company.Name = "Company 2";
                mapper.SaveChanges();
                Assert.AreEqual("Company 2", mapper.Load<Company>(company.Id).Name);
            }
        }

        [Test]
        public void Should_update_retrieved_entity()
        {
            using (var docSharp = new DocSharp(DbName))
            {
                var documentStore = new DocumentStore(docSharp);
                documentStore.Database = DbName;
                documentStore.AddMap(new CompanyMap());


                var session1 = documentStore.OpenSession();
                var company = new Company {Name = "Company 1"};
                session1.Store(company);
                var companyId = company.Id;

                var session2 = documentStore.OpenSession();
                var companyFound = session2.Load<Company>(companyId);
                companyFound.Name = "New Name";
                session2.SaveChanges();

                Assert.AreEqual("New Name", session2.Load<Company>(companyId).Name);
            }
        }
    }
}