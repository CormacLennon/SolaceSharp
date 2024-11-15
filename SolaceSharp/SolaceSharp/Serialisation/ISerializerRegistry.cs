using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolaceSharp.Serialisation
{
    public interface ISerializerRegistry
    {
        ISerializer<T> GetSerializer<T>();
    }
}
