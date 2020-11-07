//-----------------------------------------------------------------------
// <copyright file="ClassTemplateModel.cs" company="NJsonSchema">
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
    /// <summary>The CSharp class template model.</summary>
    public class ClassTemplateModel : ClassTemplateModelBase
    {
        private readonly CSharpTypeResolver _resolver;
        private readonly JsonSchema _schema;
        private readonly object _rootObject;
        private readonly CSharpGeneratorSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModel"/> class.</summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        public ClassTemplateModel(string typeName, CSharpGeneratorSettings settings,
            CSharpTypeResolver resolver, JsonSchema schema, object rootObject)
            : base(resolver, schema, rootObject)
        {
            _resolver = resolver;
            _schema = schema;
            _rootObject = rootObject;
            _settings = settings;

            ClassName = typeName;
            Properties = _schema.ActualProperties.Values
                .Where(p => !p.IsInheritanceDiscriminator)
                .Select(property => new PropertyModel(this, property, _resolver, _settings))
                .ToArray();

            var inheritedSchema = schema.GetInheritedSchema(rootObject);
            if (inheritedSchema != null)
            {
                BaseClass = new ClassTemplateModel(BaseClassName, settings, resolver, inheritedSchema, rootObject);
                AllProperties = Properties.Concat(BaseClass.AllProperties).ToArray();
            }
            else
            {
                AllProperties = Properties;
            }
        }

        /// <summary>Gets or sets the class name.</summary>
        public override string ClassName { get; }

        /// <summary>Gets the namespace.</summary>
        public string Namespace => _settings.Namespace;

        /// <summary>Gets a value indicating whether the C#8 nullable reference types are enabled for this file.</summary>
        public bool GenerateNullableReferenceTypes => _settings.GenerateNullableReferenceTypes;

        /// <summary>Gets a value indicating whether an additional properties type is available.</summary>
        public bool HasAdditionalPropertiesType =>
            !_schema.IsDictionary &&
            !_schema.ActualTypeSchema.IsDictionary &&
            !_schema.IsArray &&
            !_schema.ActualTypeSchema.IsArray &&
            (_schema.ActualTypeSchema.AllowAdditionalProperties ||
             _schema.ActualTypeSchema.AdditionalPropertiesSchema != null);

        /// <summary>Gets the type of the additional properties.</summary>
        public string AdditionalPropertiesType => HasAdditionalPropertiesType ? "object" : null; // TODO: Find a way to use typed dictionaries
        //public string AdditionalPropertiesType => HasAdditionalPropertiesType ? _resolver.Resolve(
        //    _schema.AdditionalPropertiesSchema,
        //    _schema.AdditionalPropertiesSchema.IsNullable(_settings.SchemaType),
        //    string.Empty) : null;

        /// <summary>Gets the property models.</summary>
        public IEnumerable<PropertyModel> Properties { get; }

        /// <summary>Gets the property models with inherited properties.</summary>
        public IEnumerable<PropertyModel> AllProperties { get; }

        /// <summary>Gets a value indicating whether the class has description.</summary>
        public bool HasDescription => !(_schema is JsonSchemaProperty) &&
            (!string.IsNullOrEmpty(_schema.Description) ||
             !string.IsNullOrEmpty(_schema.ActualTypeSchema.Description));

        /// <summary>Gets the description.</summary>
        public string Description => !string.IsNullOrEmpty(_schema.Description) ?
            _schema.Description : _schema.ActualTypeSchema.Description;

        /// <summary>Gets a value indicating whether the class style is INPC.</summary>
        public bool RenderInpc => _settings.ClassStyle == CSharpClassStyle.Inpc;

        /// <summary>Gets a value indicating whether the class style is Prism.</summary>
        public bool RenderPrism => _settings.ClassStyle == CSharpClassStyle.Prism;

        /// <summary>Gets a value indicating whether the class style is Record.</summary>
        public bool RenderRecord => _settings.ClassStyle == CSharpClassStyle.Record;

        /// <summary>Gets a value indicating whether to render ToJson() and FromJson() methods.</summary>
        public bool GenerateJsonMethods => _settings.GenerateJsonMethods;

        /// <summary>Gets a value indicating whether the class has discriminator property.</summary>
        public bool HasDiscriminator => !string.IsNullOrEmpty(_schema.ActualDiscriminator);

        /// <summary>Gets the discriminator property name.</summary>
        public string Discriminator => _schema.ActualDiscriminator;

        /// <summary>Gets a value indicating whether this class represents a tuple.</summary>
        public bool IsTuple => _schema.ActualTypeSchema.IsTuple;

        /// <summary>Gets the tuple types.</summary>
        public string[] TupleTypes => _schema.ActualTypeSchema.Items
            .Select(i => _resolver.Resolve(i, i.IsNullable(_settings.SchemaType), string.Empty, false))
            .ToArray();

        /// <summary>Gets a value indicating whether the class has a parent class.</summary>
        public bool HasInheritance => _schema.GetInheritedSchema(_rootObject) != null;

        /// <summary>Gets the base class name.</summary>
        public string BaseClassName => HasInheritance ? _resolver.Resolve(_schema.GetInheritedSchema(_rootObject), false, string.Empty, false)
                .Replace(_settings.ArrayType + "<", _settings.ArrayBaseType + "<")
                .Replace(_settings.DictionaryType + "<", _settings.DictionaryBaseType + "<") : null;

        /// <summary>Gets the base class model.</summary>
        public ClassTemplateModel BaseClass { get; }

        /// <summary>Gets a value indicating whether the class inherits from exception.</summary>
        public bool InheritsExceptionSchema => _resolver.ExceptionSchema != null &&
                                               _schema?.InheritsSchema(_resolver.ExceptionSchema, _rootObject) == true;

        /// <summary>Gets a value indicating whether to use the DateFormatConverter.</summary>
        public bool UseDateFormatConverter => _settings.DateType.StartsWith("System.Date");

        /// <summary>Gets or sets the access modifier of generated classes and interfaces.</summary>
        public string TypeAccessModifier => _settings.TypeAccessModifier;

        /// <summary>Gets the access modifier of property setters (default: '').</summary>
        public string PropertySetterAccessModifier => !string.IsNullOrEmpty(_settings.PropertySetterAccessModifier) ? _settings.PropertySetterAccessModifier + " " : "";

        /// <summary>Gets the JSON serializer parameter code.</summary>
        public string JsonSerializerParameterCode => CSharpJsonSerializerGenerator.GenerateJsonSerializerParameterCode(_settings, null);

        /// <summary>Gets a value indicating whether the class is deprecated.</summary>
        public bool IsDeprecated => _schema.IsDeprecated;

        /// <summary>Gets a value indicating whether the class has a deprecated message.</summary>
        public bool HasDeprecatedMessage => !string.IsNullOrEmpty(_schema.DeprecatedMessage);

        /// <summary>Gets the deprecated message.</summary>
        public string DeprecatedMessage => _schema.DeprecatedMessage;
    }
}
