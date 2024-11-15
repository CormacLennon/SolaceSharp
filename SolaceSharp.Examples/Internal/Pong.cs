
using System.Text.Json.Serialization;

namespace SolaceSharp.Examples
{
    public class Pong
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }
}
