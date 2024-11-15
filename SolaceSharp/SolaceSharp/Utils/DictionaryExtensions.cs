﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolaceSharp.Utils
{
    internal static class DictionaryExtensions
    {
        public static bool TryGetValueAs<Key, Value, ValueAs>(this IDictionary<Key, Value> dictionary, Key key, out ValueAs valueAs) where ValueAs : Value
        {
            if (dictionary.TryGetValue(key, out Value value))
            {
                valueAs = (ValueAs)value;
                return true;
            }

            valueAs = default;
            return false;
        }
    }
}