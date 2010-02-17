using System;

namespace DocSharp
{
    public class Document<T>
    {
        public Guid Id { get; set; }

        public T Data { get; set; }
    }
}