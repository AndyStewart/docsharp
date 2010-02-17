using System;
using System.IO;
using Microsoft.Isam.Esent.Interop;

namespace DocSharp
{
    public class StorageEngine : IDisposable
    {
        private readonly string _pathToDatabase;
        private readonly Instance instance;

        public StorageEngine(string pathToDatabase)
        {
            _pathToDatabase = pathToDatabase;

            instance = new Instance(pathToDatabase);
            instance.Parameters.CircularLog = true;
            instance.Init();

            if (!File.Exists(_pathToDatabase))
                DatabaseSchema.Generate(instance, _pathToDatabase);
        }

        public void Delete()
        {
            File.Delete(_pathToDatabase);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Api.JetTerm2(instance, TermGrbit.Abrupt);
        }

        public StorageSession CreateSession()
        {
            return new StorageSession(instance, _pathToDatabase);
        }
    }
}