using System;
using System.Buffers;
using System.Text.Json;

namespace SolaceSharp.Serialisation
{
    internal class JsonSerializer<T> : ISerializer<T>
    {
        private static readonly JsonWriterOptions JsonWriterOpts = new JsonWriterOptions() { Indented = false, SkipValidation = true, };

        public void Serialise(IBufferWriter<byte> buffer, T source)
        {
            using (var writer = new Utf8JsonWriter(buffer, JsonWriterOpts))
            {
                JsonSerializer.Serialize(writer, source);
            }
        }

        public T Deserialise(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return default;
            }

            var reader = new Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize<T>(ref reader);
        }
    }
}
