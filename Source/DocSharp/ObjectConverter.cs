using System.IO;
using Newtonsoft.Json;

namespace DocSharp
{
    public class ObjectConverter
    {
        private static JsonSerializer serializer;

        static ObjectConverter()
        {
            serializer = JsonSerializer.Create(new JsonSerializerSettings());
        }

        public static T ToObject<T>(string jsonData)
        {
            var dataReader = new StringReader(jsonData);
            return (T)serializer.Deserialize(dataReader, typeof(T));
        }

        public static string ToJson(object data)
        {
            var writer= new StringWriter();
            serializer.Serialize(writer, data);
            return writer.ToString();
        }


    }
}