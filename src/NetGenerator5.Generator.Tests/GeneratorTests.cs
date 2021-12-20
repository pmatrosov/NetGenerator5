using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Sdk;

namespace NetGenerator5.Generator.Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void SimpleGeneratorTest()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(@"
namespace TestNs
{
    [Model]
    public record ExampleModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}");

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            IEnumerable<ISourceGenerator> generators = new[] { new SourceGenerator() };

            // Create the driver that will control the generation, passing in our generator

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder.Add("build_property.MSBuildProjectDirectory", "123");
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, null, null, new GlobalOptionsProvider(builder.ToImmutable()));

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            //// Or we can look at the results directly:
            //GeneratorDriverRunResult runResult = driver.GetRunResult();

            //// The runResult contains the combined results of all generators passed to the driver
            //Debug.Assert(runResult.GeneratedTrees.Length == 1);
            //Debug.Assert(runResult.Diagnostics.IsEmpty);

            //// Or you can access the individual results on a by-generator basis
            //GeneratorRunResult generatorResult = runResult.Results[0];
            //Debug.Assert(generatorResult.Generator == generator);
            //Debug.Assert(generatorResult.Diagnostics.IsEmpty);
            //Debug.Assert(generatorResult.GeneratedSources.Length == 1);
            //Debug.Assert(generatorResult.Exception is null);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    }

    internal class GlobalOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly CustomConfigOptions _globalOptions;

        public GlobalOptionsProvider(ImmutableDictionary<string, string> globalProperties) => _globalOptions = new CustomConfigOptions(globalProperties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => CustomConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => CustomConfigOptions.Empty;

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;
    }

    internal sealed class CustomConfigOptions : AnalyzerConfigOptions
    {
        internal static ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<string, string>(KeyComparer);

        public static CustomConfigOptions Empty { get; } = new(EmptyDictionary);

        private readonly ImmutableDictionary<string, string> _properties;

        public CustomConfigOptions(ImmutableDictionary<string, string> properties) => _properties = properties;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _properties.TryGetValue(key, out value);
    }
}
