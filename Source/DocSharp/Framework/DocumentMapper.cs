using System;

namespace DocSharp.Framework
{
    public class DocumentMapper
    {
        private readonly DocumentStore store;
        private DocSharp docSharp;

        public DocumentMapper(DocumentStore store)
        {
            this.store = store;
            docSharp= store.DocSharp;
        }

        public T Load<T>(Guid id)
        {
            var documentFound = docSharp.Load<T>(id);
            var map = store.GetMap<T>();
            map.IdentityProperty.SetValue(documentFound.Data, documentFound.Id, null);
            return documentFound.Data;
        }

        public void Store<T>(T company)
        {
            var documentStored = docSharp.Store(company);
            var map = store.GetMap<T>();
            map.IdentityProperty.SetValue(documentStored.Data, documentStored.Id, null);
        }
    }
}