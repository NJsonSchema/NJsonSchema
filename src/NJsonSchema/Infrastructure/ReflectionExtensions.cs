//-----------------------------------------------------------------------
// <copyright file="ReflectionExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NJsonSchema.Annotations;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reflection.</summary>
    public static class ReflectionExtensions
    {
        /// <summary>Determines whether the specified property name exists.</summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
        public static bool HasProperty(this object obj, string propertyName)
        {
            return obj?.GetType().GetRuntimeProperty(propertyName) != null;
        }

        /// <summary>Determines whether the specified property name exists.</summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="defaultValue">Default value if the property does not exist.</param>
        /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
        public static T TryGetPropertyValue<T>(this object obj, string propertyName, T defaultValue = default(T))
        {
            var property = obj?.GetType().GetRuntimeProperty(propertyName);
            return property == null ? defaultValue : (T)property.GetValue(obj);
        }

        /// <summary>Tries to get the first object of the given type name.</summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="typeName">Type of the attribute.</param>
        /// <param name="typeNameStyle">The type name style.</param>
        /// <returns>May return null.</returns>
        public static T TryGetByObjectType<T>(this IEnumerable<T> attributes, string typeName, TypeNameStyle typeNameStyle = TypeNameStyle.FullName)
        {
            return attributes.FirstOrDefault(a => a.GetType().FullName == typeName);
        }

        /// <summary>Finds the first common base of the given types.</summary>
        /// <param name="types">The types.</param>
        /// <returns>The common base type.</returns>
        public static Type FindCommonBaseType(this IEnumerable<Type> types)
        {
            var baseType = types.First();
            while (baseType != typeof(object) && baseType != null)
            {
                if (types.All(t => baseType.GetTypeInfo().IsAssignableFrom(t.GetTypeInfo())))
                    return baseType;

                baseType = baseType.GetTypeInfo().BaseType;
            }
            return typeof(object);
        }

        /// <summary>Tries to get the first object which is assignable to the given type name.</summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="typeName">Type of the attribute.</param>
        /// <param name="typeNameStyle">The type name style.</param>
        /// <returns>May return null (not found).</returns>
        public static T TryGetIfAssignableTo<T>(this IEnumerable<T> attributes, string typeName, TypeNameStyle typeNameStyle = TypeNameStyle.FullName)
        {
            return attributes != null ?
                attributes.FirstOrDefault(a => a.GetType().IsAssignableTo(typeName, typeNameStyle)) :
                default(T);
        }

        /// <summary>Checks whether the given type is assignable to the given type name.</summary>
        /// <param name="type">The type.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="typeNameStyle">The type name style.</param>
        /// <returns></returns>
        public static bool IsAssignableTo(this Type type, string typeName, TypeNameStyle typeNameStyle)
        {
            if (typeNameStyle == TypeNameStyle.Name && type.Name == typeName)
                return true;

            if (typeNameStyle == TypeNameStyle.FullName && type.FullName == typeName)
                return true;

            return type.InheritsFrom(typeName, typeNameStyle);
        }

        /// <summary>Checks whether the given type inherits from the given type name.</summary>
        /// <param name="type">The type.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="typeNameStyle">The type name style.</param>
        /// <returns>true if the type inherits from typeName.</returns>
        public static bool InheritsFrom(this Type type, string typeName, TypeNameStyle typeNameStyle)
        {
            var baseType = type.GetTypeInfo().BaseType;
            while (baseType != null)
            {
                if (typeNameStyle == TypeNameStyle.Name && baseType.Name == typeName)
                    return true;
                if (typeNameStyle == TypeNameStyle.FullName && baseType.FullName == typeName)
                    return true;

                baseType = baseType.GetTypeInfo().BaseType;
            }
            return false;
        }

        /// <summary>Gets the type of the array item.</summary>
        public static Type GetEnumerableItemType(this Type type)
        {
            var jsonSchemaAttribute = type.GetTypeInfo().GetCustomAttribute<JsonSchemaAttribute>();
            if (jsonSchemaAttribute?.ArrayItem != null)
                return jsonSchemaAttribute.ArrayItem;

            var genericTypeArguments = GetGenericTypeArguments(type);
            var itemType = genericTypeArguments.Length == 0 ? type.GetElementType() : genericTypeArguments[0];
            if (itemType == null)
            {
#if !LEGACY
                foreach (var iface in type.GetTypeInfo().ImplementedInterfaces)
#else
                foreach (var iface in type.GetTypeInfo().GetInterfaces())
#endif
                {
                    itemType = GetEnumerableItemType(iface);
                    if (itemType != null)
                        return itemType;
                }
            }
            return itemType;
        }

        /// <summary>Gets the generic type arguments of a type.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The type arguments.</returns>
        public static Type[] GetGenericTypeArguments(this Type type)
        {
#if !LEGACY

            var genericTypeArguments = type.GenericTypeArguments;
            while (type != null && type != typeof(object) && genericTypeArguments.Length == 0)
            {
                type = type.GetTypeInfo().BaseType;
                if (type != null)
                    genericTypeArguments = type.GenericTypeArguments;
            }
            return genericTypeArguments;

#else

            var genericTypeArguments = type.GetGenericArguments();
            while (type != null && type != typeof(object) && genericTypeArguments.Length == 0)
            {
                type = type.GetTypeInfo().BaseType;
                if (type != null)
                    genericTypeArguments = type.GetGenericArguments();
            }
            return genericTypeArguments;

#endif
        }

        internal static string GetSafeTypeName(Type type)
        {
#if !LEGACY
            if (type.IsConstructedGenericType)
                return type.Name.Split('`').First() + "Of" + string.Join("And", type.GenericTypeArguments.Select(GetSafeTypeName));
#else
            if (type.IsGenericType)
                return type.Name.Split('`').First() + "Of" + string.Join("And", type.GetGenericArguments().Select(GetSafeTypeName));
#endif

            return type.Name;
        }

#if LEGACY

        internal static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] types)
        {
            return type.GetMethod(name, types);
        }

        internal static PropertyInfo GetRuntimeProperty(this Type type, string name)
        {
            return type.GetProperty(name);
        }

        internal static FieldInfo GetDeclaredField(this Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        internal static PropertyInfo[] GetRuntimeProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        internal static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        internal static Attribute[] GetCustomAttributes(this FieldInfo fieldInfo, bool inherit = true)
        {
            return fieldInfo.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        internal static Attribute[] GetCustomAttributes(this Type type, bool inherit = true)
        {
            return type.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        internal static Attribute[] GetCustomAttributes(this PropertyInfo propertyInfo, bool inherit = true)
        {
            return propertyInfo.GetCustomAttributes(inherit).OfType<Attribute>().ToArray();
        }

        internal static T[] GetCustomAttributes<T>(this Type type, bool inherit = true)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        internal static T[] GetCustomAttributes<T>(this PropertyInfo propertyInfo, bool inherit = true)
            where T : Attribute
        {
            return propertyInfo.GetCustomAttributes(inherit).OfType<T>().ToArray();
        }

        internal static T GetCustomAttribute<T>(this Type type)
            where T : Attribute
        {
            return type.GetCustomAttributes().OfType<T>().FirstOrDefault();
        }

        internal static T GetCustomAttribute<T>(this PropertyInfo propertyInfo)
            where T : Attribute
        {
            return propertyInfo.GetCustomAttributes().OfType<T>().FirstOrDefault();
        }

        internal static object GetValue(this PropertyInfo propertyInfo, object obj)
        {
            return propertyInfo.GetValue(obj, null);
        }

        internal static void SetValue(this PropertyInfo propertyInfo, object obj, object value)
        {
            propertyInfo.SetValue(obj, value, null);
        }

#endif
    }
}