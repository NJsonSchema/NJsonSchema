//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.CSharp.Models
{
    // TODO: Add base class for CSharp.EnumTemplateModel and TypeScript.EnumTemplateModel

    /// <summary>The CSharp enum template model.</summary>
    public class EnumTemplateModel : TemplateModelBase
    {
        private readonly JsonSchema _schema;
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="EnumTemplateModel" /> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The settings.</param>
        public EnumTemplateModel(string typeName, JsonSchema schema, CSharpGeneratorSettings settings)
        {
            _schema = schema;
            _settings = settings;
            Name = typeName;
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription => !(_schema is JsonSchemaProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _schema.Description;

        /// <summary>Gets the property extension data.</summary>
        public IDictionary<string, object> ExtensionData => _schema.ExtensionData;

        /// <summary>Gets a value indicating whether the enum is of type string.</summary>
        public bool IsStringEnum => _schema.Type != JsonObjectType.Integer;

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => _settings.TypeAccessModifier;

        /// <summary>Gets or sets if we output as Bit Flags.</summary>
        public bool IsEnumAsBitFlags => _settings.EnforceFlagEnums || _schema.IsFlagEnumerable;

        /// <summary>Gets the enum values.</summary>
        public IEnumerable<EnumerationItemModel> Enums
        {
            get
            {
                var entries = new List<EnumerationItemModel>();
                for (var i = 0; i < _schema.Enumeration.Count; i++)
                {
                    var value = _schema.Enumeration.ElementAt(i);
                    if (value != null)
                    {
                        if (_schema.Type.IsInteger())
                        {
                            var name = _schema.EnumerationNames.Count > i ?
                                _schema.EnumerationNames.ElementAt(i) : "_" + value;

                            if (_schema.IsFlagEnumerable && TryGetInt64(value, out long valueInt64))
                            {
                                entries.Add(new EnumerationItemModel
                                {
                                    Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                    Value = value.ToString(),
                                    InternalValue = valueInt64.ToString(),
                                    InternalFlagValue = valueInt64.ToString()
                                });
                            }
                            else
                            {
                                entries.Add(new EnumerationItemModel
                                {
                                    Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                    Value = value.ToString(),
                                    InternalValue = value.ToString(),
                                    InternalFlagValue = (1 << i).ToString()
                                });
                            }
                        }
                        else
                        {
                            var name = _schema.EnumerationNames.Count > i ?
                                _schema.EnumerationNames.ElementAt(i) : value.ToString();

                            entries.Add(new EnumerationItemModel
                            {
                                Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                                Value = value.ToString(),
                                InternalValue = i.ToString(),
                                InternalFlagValue = (1 << i).ToString()
                            });
                        }
                    }
                }

                return entries;
            }
        }

        private static bool TryGetInt64(object value, out long valueInt64)
        {
            if (value is byte b)
            {
                valueInt64 = b;
                return true;
            }
            else if (value is sbyte sb)
            {
                valueInt64 = sb;
                return true;
            }
            else if (value is short int16)
            {
                valueInt64 = int16;
                return true;
            }
            else if (value is ushort uint16)
            {
                valueInt64 = uint16;
                return true;
            }
            else if (value is int int32)
            {
                valueInt64 = int32;
                return true;
            }
            else if (value is uint uint32)
            {
                valueInt64 = uint32;
                return true;
            }
            else if (value is long int64)
            {
                valueInt64 = int64;
                return true;
            }
            else if (value is ulong uint64)
            {
                valueInt64 = (long)uint64;
                return true;
            }
            else if (value is float ieee754 && System.Math.Floor(ieee754) == ieee754)
            {
                valueInt64 = (long)ieee754;
                return true;
            }
            else if (value is double dieee754 && System.Math.Floor(dieee754) == dieee754)
            {
                valueInt64 = (long)dieee754;
                return true;
            }
            else
            {
                valueInt64 = default(long);
                return false;
            }
        }
    }
}