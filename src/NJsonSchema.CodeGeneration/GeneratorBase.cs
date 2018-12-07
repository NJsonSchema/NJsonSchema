//-----------------------------------------------------------------------
// <copyright file="GeneratorBase.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NJsonSchema.CodeGeneration
{
    /// <summary>The base class of the code generators</summary>
    public abstract class GeneratorBase
    {
        private readonly TypeResolverBase _resolver;
        private readonly CodeGeneratorSettingsBase _settings;

        /// <summary>Initializes a new instance of the <see cref="GeneratorBase"/> class.</summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="typeResolver">The type resolver.</param>
        /// <param name="settings">The settings.</param>
        protected GeneratorBase(object rootObject, TypeResolverBase typeResolver, CodeGeneratorSettingsBase settings)
        {
            RootObject = rootObject;
            _resolver = typeResolver;
            _settings = settings;
        }

        /// <summary>Gets the root object.</summary>
        protected object RootObject { get; }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile(string typeNameHint)
        {
            var schema = (JsonSchema4)RootObject;
            return GenerateFile(schema, typeNameHint);
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile()
        {
            var schema = (JsonSchema4)RootObject;
            return GenerateFile(schema, schema.Title != null && Regex.IsMatch(schema.Title, "^[a-zA-Z0-9_]*$") ? schema.Title : null);
        }

        /// <summary>Generates the type from the schema and all types from the resolver.</summary>
        /// <param name="schema">The schema</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        public CodeArtifactCollection GenerateTypes(JsonSchema4 schema, string typeNameHint)
        {
            _resolver.Resolve(schema, false, typeNameHint); // register root type
            return GenerateTypes();
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        public string GenerateFile(JsonSchema4 schema, string typeNameHint)
        {
            var collection = GenerateTypes(schema, typeNameHint);
            return GenerateFile(collection);
        }

        /// <summary>Generates all types from the resolver.</summary>
        /// <returns>The code.</returns>
        public virtual CodeArtifactCollection GenerateTypes()
        {
            var processedTypes = new List<string>();
            var types = new Dictionary<string, CodeArtifact>();
            while (_resolver.Types.Any(t => !processedTypes.Contains(t.Value)))
            {
                foreach (var pair in _resolver.Types.ToList())
                {
                    processedTypes.Add(pair.Value);
                    var result = GenerateType(pair.Key, pair.Value);
                    types[result.TypeName] = result;
                }
            }

            var artifacts = types.Values.Where(p =>
                !_settings.ExcludedTypeNames.Contains(p.TypeName));

            return new CodeArtifactCollection(artifacts, null);
        }

        /// <summary>Generates the the whole file containing all needed types.</summary>
        /// <returns>The code</returns>
        protected abstract string GenerateFile(CodeArtifactCollection artifactCollection);

        /// <summary>Generates the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The code.</returns>
        protected abstract CodeArtifact GenerateType(JsonSchema4 schema, string typeNameHint);
    }
}