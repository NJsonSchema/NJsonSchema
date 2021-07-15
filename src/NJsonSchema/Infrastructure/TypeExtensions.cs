//-----------------------------------------------------------------------
// <copyright file="XmlDocumentationExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Namotion.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NJsonSchema.Infrastructure
{
    /// <summary>Provides extension methods for reading contextual type names and descriptions.</summary>
    public static class TypeExtensions
    {
        private static Dictionary<ContextualMemberInfo, string> _names = new Dictionary<ContextualMemberInfo, string>();

        /// <summary>Gets the name of the property for JSON serialization.</summary>
        /// <returns>The name.</returns>
        internal static string GetName(this ContextualMemberInfo member)
        {
            if (!_names.ContainsKey(member))
            {
                lock (_names)
                {
                    if (!_names.ContainsKey(member))
                    {
                        _names[member] = GetNameWithoutCache(member);
                    }
                }
            }
            return _names[member];
        }

        private static string GetNameWithoutCache(ContextualMemberInfo member)
        {
            var jsonPropertyAttribute = member.GetContextAttribute<JsonPropertyAttribute>();
            if (jsonPropertyAttribute != null && !string.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))
            {
                return jsonPropertyAttribute.PropertyName;
            }

            var dataMemberAttribute = member.GetContextAttribute<DataMemberAttribute>();
            if (dataMemberAttribute != null && !string.IsNullOrEmpty(dataMemberAttribute.Name))
            {
                var dataContractAttribute = member.MemberInfo.DeclaringType.ToCachedType().GetTypeAttribute<DataContractAttribute>();
                if (dataContractAttribute != null)
                {
                    return dataMemberAttribute.Name;
                }
            }

            return member.Name;
        }

        /// <summary>Gets the name of the given member (based on the DisplayAttribute).</summary>
        /// <returns>The name or null if no name is available.</returns>
        public static string GetDisplayName(this ContextualType contextualType)
        {
            dynamic displayAttribute = contextualType.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
            // GetName returns null if the Name property on the attribute is not specified.
            return displayAttribute?.GetName();
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="type">The member info</param>
        /// <param name="attributeType">The attribute type to check.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static string GetDescription(this CachedType type, DescriptionAttributeType attributeType = DescriptionAttributeType.Context)
        {
            var attributes = type is ContextualType contextualType && attributeType == DescriptionAttributeType.Context ?
                contextualType.ContextAttributes : type.TypeAttributes;

            dynamic descriptionAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                return descriptionAttribute.Description;
            }
            else
            {
                dynamic displayAttribute = attributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null)
                {
                    // GetDescription returns null if the Description property on the attribute is not specified.
                    var description = displayAttribute.GetDescription();
                    if (description != null) 
                    {
                        return description;
                    }
                }

                if (type is ContextualMemberInfo contextualMember)
                {
                    var summary = contextualMember.GetXmlDocsSummary();
                    if (summary != string.Empty)
                    {
                        return summary;
                    }
                }
                else if (type != null)
                {
                    var summary = type.GetXmlDocsSummary();
                    if (summary != string.Empty)
                    {
                        return summary;
                    }
                }
            }

            return null;
        }

        /// <summary>Gets the description of the given member (based on the DescriptionAttribute, DisplayAttribute or XML Documentation).</summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The description or null if no description is available.</returns>
        public static string GetDescription(this ContextualParameterInfo parameter)
        {
            dynamic descriptionAttribute = parameter.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DescriptionAttribute");
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                return descriptionAttribute.Description;
            }
            else
            {
                dynamic displayAttribute = parameter.ContextAttributes.FirstAssignableToTypeNameOrDefault("System.ComponentModel.DataAnnotations.DisplayAttribute");
                if (displayAttribute != null && !string.IsNullOrEmpty(displayAttribute.Description))
                {
                    return displayAttribute.Description;
                }

                if (parameter != null)
                {
                    var summary = parameter.GetXmlDocs();
                    if (summary != string.Empty)
                    {
                        return summary;
                    }
                }
            }

            return null;
        }
    }
}