using ProtoBuf;
using SolaceSharp.Serialisation;
using SolaceSharp.V2.ByExamples;
using System;
using System.Buffers;

namespace SolaceSharp.Examples
{
    class ExampleSerializerRegistry : ISerializerRegistry
    {
        private ISerializerRegistry _jsonregistry = new JsonSerializerRegistry();

        private ISerializer<Ping> _pingSerialiser = new PingSerializer();

        public ISerializer<T> GetSerializer<T>()
        {
            if (typeof(T) == typeof(Ping))
                return (ISerializer<T>)_pingSerialiser;

            return _jsonregistry.GetSerializer<T>();
        }
    }

    class PingSerializer : ISerializer<Ping>
    {
        public Ping Deserialise(ReadOnlySpan<byte> buffer)
        {
            return Serializer.Deserialize<Ping>(buffer);
        }

        public void Serialise(IBufferWriter<byte> buffer, Ping source)
        {
            Serializer.Serialize(buffer, source);
        }
    }
}
