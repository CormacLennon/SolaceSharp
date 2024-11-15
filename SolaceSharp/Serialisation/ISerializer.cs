using System;
using System.Buffers;

namespace SolaceSharp.Serialisation
{
    public interface ISerializer
    {
    }

    public interface ISerializer<T> : ISerializer
    {
        void Serialise(IBufferWriter<byte> buffer, T source);
        T Deserialise(ReadOnlySpan<byte> buffer);
    }
}
