using System;
using System.Collections.Concurrent;
using SolaceSharp.Utils;

namespace SolaceSharp.Serialisation
{
    public class JsonSerializerRegistry : ISerializerRegistry
    {
        private ConcurrentDictionary<Type, ISerializer> _serialisers = new ConcurrentDictionary<Type, ISerializer>();

        public ISerializer<T> GetSerializer<T>()
        {
            if (_serialisers.TryGetValueAs(typeof(T), out ISerializer<T> serializer))
            {
                return serializer;
            }
            serializer = new JsonSerializer<T>();
            _serialisers[typeof(T)] = serializer;
            return serializer;
        }
    }
}
