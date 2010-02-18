using System;

namespace DocSharp
{
    public class DocSharp : IDisposable
    {
        private StorageEngine storageEngine;

        public DocSharp(string dbName)
        {
            storageEngine = new StorageEngine(dbName);
        }

        public void Dispose()
        {
            storageEngine.Dispose();
        }

        public Document<T> Store<T>(T value)
        {
            var document = new Document<T>() { Id = Guid.NewGuid(), Data = value };
            using (var session = storageEngine.CreateSession())
            {
                session.Insert(document);
            }
            return document;
        }

        public Document<T> Load<T>(Guid id)
        {
            using (var session = storageEngine.CreateSession())
            {
                return session.Load<T>(id);
            }
        }

        public void Delete(Guid id)
        {
            using (var session = storageEngine.CreateSession())
            {
                session.Delete(id);
            }
        }

        public void Update<T>(Document<T> document)
        {
            using (var session = storageEngine.CreateSession())
            {
                session.Update(document);
            }
        }
    }
}