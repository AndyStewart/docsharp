using System;

namespace DocSharp
{
    public class Document<T> : Document
    {
        public new T Data { get; set; }
        public new Type Type
        {
            get
            {
                return typeof (T);
            }
        }
    }

    public class Document
    {
        public Guid Id { get; set; }
        public object Data { get; set; }
        public Type Type { get; set; }
    }
}