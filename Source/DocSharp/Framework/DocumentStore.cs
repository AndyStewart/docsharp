using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DocSharp.Tests.Framework;

namespace DocSharp.Framework
{
    public class DocumentStore
    {
        public DocSharp DocSharp { get; set; }
        private List<DocumentMap> maps = new List<DocumentMap>();

        public DocumentStore(DocSharp docSharp)
        {
            DocSharp = docSharp;
        }

        public void AddMap<T>(IMap<T> map)
        {
            var documentMap = new DocumentMap<T>();
            documentMap.Type = typeof(T);
            map.Map(documentMap);
            maps.Add(documentMap);
        }

        public string Database { get; set; }

        public DocumentMap GetMap<T>()
        {
            foreach (var map in maps)
            {
                if (map.Type == typeof(T))
                    return map;
            }
            return null;
        }
    }

}