using System;
using System.Reflection;
using DocSharp.Framework;

namespace DocSharp.Tests.Framework
{
    public class DocumentMap
    {
        public Type Type { get; set; }
        public PropertyInfo IdentityProperty { get; protected set; }
    }

    public interface IMap<T>
    {
        void Map(DocumentMap<T> map);
    }
}