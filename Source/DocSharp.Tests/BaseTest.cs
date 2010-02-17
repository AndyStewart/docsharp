using System.IO;
using NUnit.Framework;

namespace DocSharp.Tests
{
    public class BaseTest
    {
        protected string DbName = "DocDb.esb";

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(DbName))
                File.Delete(DbName);
        }
    }
}