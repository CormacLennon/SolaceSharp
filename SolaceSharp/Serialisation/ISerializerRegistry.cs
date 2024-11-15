
namespace SolaceSharp.Serialisation
{
    public interface ISerializerRegistry
    {
        ISerializer<T> GetSerializer<T>();
    }
}
