using System.Reflection;
using Cake.Core.Annotations;
using Cake.CycloneDX.Tools.CdxCli;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit
{
    public sealed class AliasNamespaceImportTests
    {
        // Cake.Sdk only honors [CakeNamespaceImport] on the alias method itself, and build scripts only get the
        // alias class's own namespace for free. Every addin type an alias takes must therefore live in the alias
        // class's namespace or be imported on the method.
        [Fact]
        public void Alias_Parameter_Types_From_Other_Namespaces_Are_Imported_On_The_Alias_Method()
        {
            var assembly = typeof(CdxCliAliases).Assembly;
            var violations = new List<string>();

            var aliases = assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.IsDefined(typeof(CakeMethodAliasAttribute), false));

            foreach (var alias in aliases)
            {
                var imported = alias.GetCustomAttributes<CakeNamespaceImportAttribute>()
                    .Select(attribute => attribute.Namespace)
                    .Append(alias.DeclaringType.Namespace)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var parameter in alias.GetParameters())
                {
                    var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                    if (type.Assembly == assembly && !imported.Contains(type.Namespace))
                    {
                        violations.Add($"{alias.DeclaringType.Name}.{alias.Name}({type.Name}) needs [CakeNamespaceImport(\"{type.Namespace}\")]");
                    }
                }
            }

            Assert.Empty(violations);
        }
    }
}
