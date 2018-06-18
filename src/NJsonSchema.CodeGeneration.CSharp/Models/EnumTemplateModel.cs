//-----------------------------------------------------------------------
// <copyright file="EnumTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
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
        private readonly JsonSchema4 _schema;
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="EnumTemplateModel" /> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="settings">The settings.</param>
        public EnumTemplateModel(string typeName, JsonSchema4 schema, CSharpGeneratorSettings settings)
        {
            _schema = schema;
            _settings = settings;
            Name = typeName;
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }

        /// <summary>Gets a value indicating whether the enum has description.</summary>
        public bool HasDescription => !(_schema is JsonProperty) && !string.IsNullOrEmpty(_schema.Description);

        /// <summary>Gets the description.</summary>
        public string Description => _schema.Description;

        /// <summary>Gets a value indicating whether the enum is of type string.</summary>
        public bool IsStringEnum => _schema.Type != JsonObjectType.Integer;

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => _settings.TypeAccessModifier;

        /// <summary>Gets the enum values.</summary>
        public IEnumerable<EnumerationItemModel> Enums
        {
            get
            {
             
                var entries = new List<EnumerationItemModel>();
                
                Dictionary<string, object> Descriptions = new Dictionary<string, object>();
                Dictionary<string, object> Titles = new Dictionary<string, object>();

                if (_schema.ExtensionData.ContainsKey("elaboration"))
                    Descriptions = (Dictionary<string, object>)_schema.ExtensionData["elaboration"];

                if (_schema.ExtensionData.ContainsKey("options"))
                    Titles = (Dictionary<string, object>)_schema.ExtensionData["options"];
                
                for (var i = 0; i < _schema.Enumeration.Count; i++)
                {
                    var value = _schema.Enumeration.ElementAt(i);
                    if (value != null)
                    {
                        var name = _schema.EnumerationNames.Count > i ?
                            _schema.EnumerationNames.ElementAt(i) :
                            _schema.Type.HasFlag(JsonObjectType.Integer) ? "_" + value : value.ToString();

                        string MyDesc = value.ToString();
                        if (Descriptions.Count == _schema.Enumeration.Count)
                            MyDesc = Descriptions[value.ToString()].ToString();

                        entries.Add(new EnumerationItemModel
                        {
                            Name = _settings.EnumNameGenerator.Generate(i, name, value, _schema),
                            Value = value.ToString(),
                            InternalValue = _schema.Type.HasFlag(JsonObjectType.Integer) ? value.ToString() : i.ToString(),
                            Description = MyDesc
                        });
                    }
                }

                return entries;
            }
        }
    }
}
