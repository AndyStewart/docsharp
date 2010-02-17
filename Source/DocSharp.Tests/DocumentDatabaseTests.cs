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
            var documentDb = new DocumentDatabase("DocDb.esb");
            Assert.IsTrue(File.Exists("DocDb.esb"));
        }
    }
}