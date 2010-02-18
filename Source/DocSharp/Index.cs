using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocSharp
{
    public class Index
    {
        private readonly Type _type;

        public Index(Type type)
        {
            _type = type;
        }

        Dictionary<Guid, object> indexItems = new Dictionary<Guid, object>();
        private PropertyInfo propertyInfo;

        public bool AppliesTo(Type type)
        {
            return type == _type;
        }

        public void Add(Guid guid, object value)
        {
            indexItems.Add(guid, propertyInfo.GetValue(value, null));
        }

        public void Rule(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
        }

        public IEnumerable<Guid> Find(MethodCallExpression value)
        {
            return indexItems.Where(q => Find1(value, q)).Select(q => q.Key);
        }

        private bool Find1(MethodCallExpression value, KeyValuePair<Guid, object> q)
        {
            var a = (bool) value.Method.Invoke(q.Value, value.Arguments.Cast<ConstantExpression>().Select(c => c.Value).ToArray());
            return a;
        }
    }
}