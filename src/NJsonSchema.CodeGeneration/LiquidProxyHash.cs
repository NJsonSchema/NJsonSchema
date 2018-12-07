//-----------------------------------------------------------------------
// <copyright file="LiquidProxyHash.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotLiquid;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NJsonSchema.CodeGeneration.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100337d8a0b73ac39048dc55d8e48dd86dcebd0af16aa514c73fbf5f283a8e94d7075b4152e5621e18d234bf7a5aafcb6683091f79d87b80c3be3e806f688e6f940adf92b28cedf1f8f69aa443699c235fa049204b56b83d94f599dd9800171f28e45ab74351acab17d889cd65961354d2f6405bddb9e896956e69e60033c2574f1")]

namespace NJsonSchema.CodeGeneration
{
    internal class LiquidProxyHash : Hash
    {
        private readonly IDictionary<string, PropertyInfo> _properties;

        public LiquidProxyHash(object obj)
        {
            Object = obj;
            _properties = obj?.GetType().GetRuntimeProperties()
                .ToDictionary(p => p.Name, p => p) ??
                    new Dictionary<string, PropertyInfo>();
        }

        public object Object { get; }

        public override bool Contains(object key)
        {
            return _properties.ContainsKey(key.ToString()) || base.Contains(key);
        }

        protected override object GetValue(string key)
        {
            if (_properties.ContainsKey(key))
            {
                var value = _properties[key].GetValue(Object);
                if (IsObject(value))
                {
                    return GetObjectValue(value);
                }

                return value;
            }
            else
            {
                return base.GetValue(key);
            }
        }

        private object GetObjectValue(object value)
        {
            if (value is IDictionary dictionary)
            {
                return CreateDictionaryHash(dictionary);
            }
            else if (value is IEnumerable enumerable)
            {
                if (enumerable.OfType<object>().All(i => !IsObject(i)))
                {
                    var list = new List<object>();
                    foreach (var item in enumerable)
                    {
                        list.Add(item);
                    }

                    return list;
                }
                else if (enumerable.OfType<object>().All(i => i is IDictionary))
                {
                    var list = new List<object>();
                    foreach (IDictionary item in enumerable)
                    {
                        list.Add(CreateDictionaryHash(item));
                    }

                    return list;
                }
                else
                {
                    var list = new List<LiquidProxyHash>();
                    foreach (var item in enumerable)
                    {
                        list.Add(new LiquidProxyHash(item));
                    }

                    return list;
                }
            }
            else
            {
                return new LiquidProxyHash(value);
            }
        }

        private object CreateDictionaryHash(IDictionary dictionary)
        {
            var hash = new Hash();
            foreach (var k in dictionary.Keys)
            {
                var v = dictionary[k];
                hash[k.ToString()] = IsObject(v) ? GetObjectValue(v) : v;
            }

            return hash;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is LiquidProxyHash hash && hash.Object != Object)
            {
                return false;
            }

            return base.Equals(obj);
        }

        private static bool IsObject(object value)
        {
            return value != null &&
                   (value.GetType().GetTypeInfo().IsClass || value.GetType().GetTypeInfo().IsInterface) &&
                   !(value is string);
        }
    }
}