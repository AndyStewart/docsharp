using System;
using System.IO;
using Microsoft.Isam.Esent.Interop;

namespace DocSharp.Storage
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
            instance.Parameters.Recovery = true;
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
            instance.Close();
            instance.Dispose();
        }

        public StorageSession CreateSession()
        {
            return new StorageSession(instance, _pathToDatabase);
        }
    }
}