//-----------------------------------------------------------------------
// <copyright file="CodeGeneratorSettingsBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The code generator settings base.</summary>
    public class CodeGeneratorSettingsBase
    {
        /// <summary>Initializes a new instance of the <see cref="CodeGeneratorSettingsBase"/> class.</summary>
        public CodeGeneratorSettingsBase()
        {
            GenerateDefaultValues = true;
            ExcludedTypeNames = new string[] { };
        }

        /// <summary>Gets or sets the schema type (default: JsonSchema).</summary>
        public SchemaType SchemaType { get; set; } = SchemaType.JsonSchema;

        /// <summary>Gets or sets a value indicating whether to generate default values for properties (when JSON Schema default is set, default: true).</summary>
        public bool GenerateDefaultValues { get; set; }

        /// <summary>Gets or sets the excluded type names (must be defined in an import or other namespace).</summary>
        public string[] ExcludedTypeNames { get; set; }

        /// <summary>Gets or sets the property name generator.</summary>
        [JsonIgnore]
        public IPropertyNameGenerator PropertyNameGenerator { get; set; }

        /// <summary>Gets or sets the type name generator.</summary>
        [JsonIgnore]
        public ITypeNameGenerator TypeNameGenerator { get; set; } = new DefaultTypeNameGenerator();

        /// <summary>Gets or sets the enum name generator.</summary>
        [JsonIgnore]
        public IEnumNameGenerator EnumNameGenerator { get; set; } = new DefaultEnumNameGenerator();

        /// <summary>Gets or sets the template factory.</summary>
        [JsonIgnore]
        public ITemplateFactory TemplateFactory { get; set; }

        /// <summary>Gets or sets the template directory path.</summary>
        public string TemplateDirectory { get; set; }

        /// <summary>Gets or sets the output language specific value generator.</summary>
        [JsonIgnore]
        public ValueGeneratorBase ValueGenerator { get; set; }
    }
}