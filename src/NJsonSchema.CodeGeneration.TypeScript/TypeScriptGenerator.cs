//-----------------------------------------------------------------------
// <copyright file="CSharpClassGenerator.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using NJsonSchema.CodeGeneration.TypeScript.Models;
using System.Linq;
using NJsonSchema.CodeGeneration.Models;
using System.Collections.Generic;

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript interface and enum code generator. </summary>
    public class TypeScriptGenerator : GeneratorBase
    {
        private readonly TypeScriptTypeResolver _resolver;
        private TypeScriptExtensionCode _extensionCode;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="schema">The schema.</param>
        public TypeScriptGenerator(JsonSchema schema)
            : this(schema, new TypeScriptGeneratorSettings())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator"/> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        public TypeScriptGenerator(object rootObject, TypeScriptGeneratorSettings settings)
            : this(rootObject, settings, new TypeScriptTypeResolver(rootObject, settings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptGenerator" /> class.</summary>
        /// <param name="rootObject">The root object to search for all JSON Schemas.</param>
        /// <param name="settings">The generator settings.</param>
        /// <param name="resolver">The resolver.</param>
        public TypeScriptGenerator(object rootObject, TypeScriptGeneratorSettings settings, TypeScriptTypeResolver resolver)
            : base(rootObject, resolver, settings)
        {
            _resolver = resolver;
            Settings = settings;
        }

        /// <summary>Gets the generator settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; }

        /// <summary>Generates all types from the resolver with extension code from the settings.</summary>
        /// <returns>The code.</returns>
        public override IEnumerable<CodeArtifact> GenerateTypes()
        {
            _extensionCode = _extensionCode ??
                new TypeScriptExtensionCode(Settings.ExtensionCode, Settings.ExtendedClasses);

            return GenerateTypes(_extensionCode);
        }

        /// <summary>Generates all types from the resolver with the given extension code.</summary>
        /// <returns>The code.</returns>
        public IEnumerable<CodeArtifact> GenerateTypes(TypeScriptExtensionCode extensionCode)
        {
            var artifacts = base.GenerateTypes();
            foreach (var artifact in artifacts)
            {
                if (extensionCode?.ExtensionClasses.ContainsKey(artifact.TypeName) == true)
                {
                    var classCode = artifact.Code;

                    var index = classCode.IndexOf("constructor(", StringComparison.Ordinal);
                    if (index != -1)
                    {
                        var code = classCode.Insert(index, extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n\n    ");
                        yield return new CodeArtifact(artifact.TypeName, artifact.BaseTypeName, artifact.Type, artifact.Language, artifact.Category, code);
                    }
                    else
                    {
                        index = classCode.IndexOf("class", StringComparison.Ordinal);
                        index = classCode.IndexOf("{", index, StringComparison.Ordinal) + 1;

                        var code = classCode.Insert(index, "\n    " + extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n");
                        yield return new CodeArtifact(artifact.TypeName, artifact.BaseTypeName, artifact.Type, artifact.Language, artifact.Category, code);
                    }
                }
                else
                {
                    yield return artifact;
                }
            }

            if (artifacts.Any(r => r.Code.Contains("formatDate(")))
            {
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File.FormatDate", new TemplateModelBase());
                yield return new CodeArtifact("formatDate", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template);
            }

            if (Settings.HandleReferences)
            {
                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File.ReferenceHandling", new TemplateModelBase());
                yield return new CodeArtifact("jsonParse", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Utility, template);
            }
        }

        /// <summary>Generates the file.</summary>
        /// <returns>The file contents.</returns>
        protected override string GenerateFile(IEnumerable<CodeArtifact> artifacts)
        {
            var model = new FileTemplateModel(Settings)
            {
                Types = artifacts.OrderByBaseDependency().Concatenate(),
                ExtensionCode = _extensionCode
            };

            var template = Settings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
            return ConversionUtilities.TrimWhiteSpaces(template.Render());
        }

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The fallback type name.</param>
        /// <returns>The code.</returns>
        protected override CodeArtifact GenerateType(JsonSchema schema, string typeNameHint)
        {
            var typeName = _resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                var model = new EnumTemplateModel(typeName, schema, Settings);

                string templateName;
                if (Settings.EnumStyle == TypeScriptEnumStyle.Enum)
                {
                    templateName = nameof(TypeScriptEnumStyle.Enum);
                }
                else if (Settings.EnumStyle == TypeScriptEnumStyle.StringLiteral)
                {
                    templateName = $"{nameof(TypeScriptEnumStyle.Enum)}.{nameof(TypeScriptEnumStyle.StringLiteral)}";
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(Settings.EnumStyle), Settings.EnumStyle, "Unknown enum style");
                }

                var template = Settings.TemplateFactory.CreateTemplate("TypeScript", templateName, model);
                return new CodeArtifact(typeName, CodeArtifactType.Enum, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Contract, template);
            }
            else
            {
                var model = new ClassTemplateModel(typeName, typeNameHint, Settings, _resolver, schema, RootObject);
                var template = Settings.CreateTemplate(typeName, model);

                var type = Settings.TypeStyle == TypeScriptTypeStyle.Interface
                    ? CodeArtifactType.Interface
                    : CodeArtifactType.Class;

                return new CodeArtifact(typeName, model.BaseClass, type, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Contract, template);
            }
        }
    }
}
