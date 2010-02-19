using System.IO;
using NUnit.Framework;

namespace DocSharp.Tests
{
    public class BaseTest
    {
        protected const string DbDirectory = @".\TestDb\";
        protected const string DbName = DbDirectory + @"DocDb.esb";

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(DbDirectory))
                Directory.Delete(DbDirectory, true);

            Directory.CreateDirectory(DbDirectory);
        }
    }
}