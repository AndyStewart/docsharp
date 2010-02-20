using System;

namespace DocSharp
{
    public class Document<T> : Document
    {
        public  T Data
        {
            get
            {
                return (T)LooseData;
            }
            set
            {
                LooseData = value;
            }
        }

        public override Type Type
        {
            get
            {
                return typeof (T);
            }
            set
            {
                
            }
        }
    }

    public class Document
    {
        public Guid Id { get; set; }
        public virtual object LooseData { get; set; }
        public virtual Type Type { get; set; }
    }
}