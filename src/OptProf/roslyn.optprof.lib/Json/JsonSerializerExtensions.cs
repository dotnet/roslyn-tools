using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roslyn.optprof.json
{
    public static class JsonSerializerExtensions
    {
        public static T Deserialize<T>(this JsonSerializer serializer, TextReader reader)
        {
            return (T)serializer.Deserialize(reader, typeof(T));
        }
    }
}
