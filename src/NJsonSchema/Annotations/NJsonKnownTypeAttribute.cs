using System;

namespace NJsonSchema.Annotations
{
    /// <summary>Specifies types that should be recognized when serializing or deserializing a given type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class NJsonKnownTypeAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.Serialization.NJsonKnownTypeAttribute"></see> class with the name of a method that returns an <see cref="T:System.Collections.IEnumerable"></see> of known types.</summary>
        /// <param name="methodName">The name of the method that returns an <see cref="T:System.Collections.IEnumerable"></see> of types used when serializing or deserializing data.</param>
        public NJsonKnownTypeAttribute(string methodName)
        {
            MethodName = methodName;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.Serialization.NJsonKnownTypeAttribute"></see> class with the specified type.</summary>
        /// <param name="type">The <see cref="T:System.Type"></see> that is included as a known type when serializing or deserializing data.</param>
        public NJsonKnownTypeAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.Serialization.NJsonKnownTypeAttribute"></see> class and automatically detect subtypes.</summary>
        public NJsonKnownTypeAttribute()
        {

        }

        /// <summary>Gets the name of a method that will return a list of types that should be recognized during serialization or deserialization.</summary>
        /// <returns>A <see cref="T:System.String"></see> that contains the name of the method on the type defined by the <see cref="T:System.Runtime.Serialization.NJsonKnownTypeAttribute"></see> class.</returns>
        public string MethodName { get; }

        /// <summary>Gets the type that should be recognized during serialization or deserialization.</summary>
        /// <returns>The <see cref="T:System.Type"></see> that is used during serialization or deserialization.</returns>
        public Type Type { get; }
    }
}
