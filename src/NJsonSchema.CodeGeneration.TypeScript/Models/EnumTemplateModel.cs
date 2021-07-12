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

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    /// <summary>The TypeScript enum template model.</summary>
    public class EnumTemplateModel
    {
        private readonly JsonSchema _schema;
        private readonly TypeScriptGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="EnumTemplateModel" /> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The settings.</param>
        public EnumTemplateModel(string typeName, JsonSchema schema, TypeScriptGeneratorSettings settings)
        {
            _schema = schema;
            _settings = settings;
            Name = typeName;
        }

        /// <summary>Gets the name of the enum.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription => !(_schema is JsonSchemaProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => ConversionUtilities.RemoveLineBreaks(_schema.Description);

        /// <summary>Gets a value indicating whether the export keyword should be added to all enums.</summary>
        public bool ExportTypes => _settings.ExportTypes;

        /// <summary>Gets the property extension data.</summary>
        public IDictionary<string, object> ExtensionData => _schema.ExtensionData;

        /// <summary>Gets the enum values.</summary>
        public List<EnumerationItemModel> Enums
        {
            get
            {
                var entries = new List<EnumerationItemModel>();
                for (int i = 0; i < _schema.Enumeration.Count; i++)
                {
                    var value = _schema.Enumeration.ElementAt(i);
                    if (value != null)
                    {
                        var name = _schema.EnumerationNames.Count > i ?
                            _schema.EnumerationNames.ElementAt(i) :
                            _schema.Type.IsInteger() ? "_" + value : value.ToString();

                        entries.Add(new EnumerationItemModel
                        {
                            Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                            Value = _schema.Type.IsInteger() ?
                                value.ToString() :
                                (_settings.TypeScriptVersion < 2.4m ? "<any>" : "") + "\"" + value + "\"",
                        });
                    }
                }
                return entries;
            }
        }
    }
}