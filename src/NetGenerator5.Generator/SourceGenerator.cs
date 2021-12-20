using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NetGenerator5.Generator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // #if DEBUG
            //             if (!System.Diagnostics.Debugger.IsAttached)
            //             {
            //                 System.Diagnostics.Debugger.Launch();
            //             }
            // #endif

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory) == false)
            {
                throw new ArgumentException("MSBuildProjectDirectory");
            }    
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.SourceGenerator_OutputPath", out var outputPath) == false)
            {
                throw new ArgumentException("SourceGenerator_ControllerNamespace");
            }
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.SourceGenerator_ModelNamespace", out var modelNamespace) == false)
            {
                throw new ArgumentException("SourceGenerator_ModelNamespace");
            }            
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.SourceGenerator_ControllerNamespace", out var controllerNamespace) == false)
            {
                throw new ArgumentException("SourceGenerator_ControllerNamespace");
            }

            var projectPath = Path.Combine(projectDirectory, outputPath.Replace("/", "\\") ?? throw new Exception("No OutputPath set"));
            
            var modelTypes = FindTypes(context, modelNamespace, "ModelAttribute");
            if (modelTypes.Length == 0)
                return;

            var controllerTypes = FindTypes(context, controllerNamespace, "ApiControllerAttribute");
            if (controllerTypes.Length == 0)
                return;

            var generatedFilePath = Path.Combine(projectPath, "generated.js");
            var generatedFileContent = DateTime.Now.ToLongTimeString() + ": " + modelTypes.Select(t => t.Name).Aggregate((a, b) => a + ", " + b);

            File.WriteAllText(generatedFilePath, $"// {generatedFileContent}");

            context.AddSource("Ts.generated", SourceText.From($"// {generatedFileContent}", Encoding.UTF8));
        }

        private ITypeSymbol[] FindTypes(GeneratorExecutionContext context, string namespaceName, string attributeFilter)
        {
            var @namespace = FindNamespace(context, namespaceName);
            return @namespace.GetMembers()
                .Where(e => e.GetAttributes().Any(a => a.AttributeClass?.ToString().EndsWith(attributeFilter) == true))
                .OfType<ITypeSymbol>()
                .ToArray();
        }

        private INamespaceSymbol FindNamespace(GeneratorExecutionContext context, string namespaceName)
        {
            var namespaceNameParts = namespaceName.Split('.');

            INamespaceSymbol @namespace = context.Compilation.GlobalNamespace;

            while (@namespace.ToString() != namespaceName)
            {
                foreach (var innerNamespace in @namespace.GetNamespaceMembers())
                {
                    if (namespaceNameParts.Contains(innerNamespace.Name))
                    {
                        @namespace = innerNamespace;
                        break;
                    }
                }
            }

            return @namespace;
        }
    }
}
