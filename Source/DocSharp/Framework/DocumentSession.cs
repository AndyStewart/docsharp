using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DocSharp.Framework
{
    public class DocumentSession
    {
        private readonly DocumentStore store;
        private DocSharp docSharp;
        private List<Document> entities = new List<Document>();

        public DocumentSession(DocumentStore store)
        {
            this.store = store;
            docSharp= store.DocSharp;
        }

        public T Load<T>(Guid id)
        {
            var documentFound = docSharp.Load<T>(id);
            var map = store.GetMap<T>();
            map.IdentityProperty.SetValue(documentFound.Data, documentFound.Id, null);
            entities.Add(documentFound);
            return documentFound.Data;
        }

        public void Store<T>(T company)
        {
            var documentStored = docSharp.Store(company);
            entities.Add(documentStored);
            var map = store.GetMap<T>();
            map.IdentityProperty.SetValue(documentStored.Data, documentStored.Id, null);
        }

        public void SaveChanges()
        {
            foreach (var entity in entities)
            {
                docSharp.Update(entity);
            }
        }

        public IList<T> GetAll<T>()
        {
            var documents = store.DocSharp.All<T>();
            var list = new List<T>();
            foreach (var document in documents)
            {
                var map = store.GetMap<T>();
                map.IdentityProperty.SetValue(document.Data, document.Id, null);
                list.Add(document.Data);
            }
            return list;
        }

        public IQueryable<T> Query<T>()
        {
            throw new NotImplementedException("TODO");
        }
    }
}