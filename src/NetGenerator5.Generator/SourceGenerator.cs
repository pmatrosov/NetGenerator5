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
            // #if DEBUG
            //             if (!System.Diagnostics.Debugger.IsAttached)
            //             {
            //                 System.Diagnostics.Debugger.Launch();
            //             }
            // #endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //#if DEBUG
            //            if (!System.Diagnostics.Debugger.IsAttached)
            //            {
            //                System.Diagnostics.Debugger.Launch();
            //            }
            //#endif

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory) == false)
            {
                throw new ArgumentException("MSBuildProjectDirectory");
            }

            var configFile = context.AdditionalFiles.First(e => e.Path.EndsWith("generatorsettings.json")).GetText(context.CancellationToken);
            var config = Newtonsoft.Json.Linq.JObject.Parse(configFile?.ToString() ?? throw new Exception("No config file"));

            var projectPath = Path.Combine(projectDirectory, config["OutputPath"]?.ToString().Replace("/", "\\") ?? throw new Exception("No OutputPath set"));
            var modelNamespaceName = config["ModelNamespace"]?.ToString() ?? throw  new Exception("No ModelNamespace set");
            var controllerNamespaceName = config["ControllerNamespace"]?.ToString() ?? throw new Exception("No ControllerNamespace set");

            var modelTypes = FindTypes(context, modelNamespaceName, "ModelAttribute");
            if (modelTypes.Length == 0)
                return;

            var controllerTypes = FindTypes(context, controllerNamespaceName, "ApiControllerAttribute");
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
