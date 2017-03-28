using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.MSBuild;

using StaticTemplate.Rewriters;

namespace StaticTemplate
{
    public static class CompileDriver
    {
        public static void CompileSolution(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(solutionPath).Result;
            var projectIds = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects();
            var projects = projectIds.Select(pid => solution.GetProject(pid));
            foreach (var project in projects)
            {
                if (project.Language != "C#")
                {
                    Console.WriteLine($"Skipping non-c# project {project.FilePath}");
                }
                var compilation = (CSharpCompilation)project.GetCompilationAsync().Result;

                var emitResult = CompileAndEmit(ref compilation);
#if DEBUG
                foreach (var compilationSyntaxTree in compilation.SyntaxTrees)
                {
                    Console.WriteLine(compilationSyntaxTree.ToString());
                }
#endif
                if (!emitResult.Success)
                {
                    Console.Error.WriteLine($"Failed to compile project: {project.FilePath}");
                }
                foreach (var diag in emitResult.Diagnostics)
                {
                    if (diag.Severity == DiagnosticSeverity.Error || diag.Severity == DiagnosticSeverity.Warning)
                    {
                        Console.Error.WriteLine(diag);
                    }
                }
            }
        }

        public static void CompileSingleCSharpFile(string filePath)
        {
            var sourceText = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText).WithFilePath(filePath);

            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
            var referenceNames = new [] { "mscorlib", "System", "System.Core", "System.Runtime", "System.Linq" };
            var referencePaths = referenceNames.Select(n => Path.Combine(runtimePath, n + ".dll")).ToList();
            var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p)).ToList();
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true);
            var assemblyName = Path.GetFileNameWithoutExtension(filePath) + ".exe";
            var compilation = CSharpCompilation.Create(assemblyName, new[] {syntaxTree}, references, options);

            var emitResult = CompileAndEmit(ref compilation);
            if (!emitResult.Success)
            {
                Console.Error.WriteLine($"Failed to compile file: {filePath}");
            }
            foreach (var diag in emitResult.Diagnostics)
            {
                Console.Error.WriteLine(diag);
            }
        }

        public static EmitResult CompileAndEmit(ref CSharpCompilation compilation, string emitPath = null)
        {
            var extracted = TemplateExtractPass(compilation, out List<ClassTemplate> templates);
            var resolved = TemplateResolvePass(extracted, templates);
            var instantiated = TemplateInstantiatePass(resolved, templates);

            compilation = instantiated;

            var emitResult = compilation.Emit(emitPath ?? compilation.AssemblyName + ".exe");
            return emitResult;
        }

        public static CSharpCompilation TemplateExtractPass(CSharpCompilation compilation, out List<ClassTemplate> templates)
        {
            var templateExtractResults = (
                from t in compilation.SyntaxTrees.AsParallel()
                select TemplateExtractRewriter.Extract(t, compilation.GetSemanticModel(t))).ToList();
            var templateExtractedSyntaxTrees = templateExtractResults.Select(t => t.Item1).ToList();
            templates = templateExtractResults.SelectMany(t => t.Item2).ToList();
            var extracted = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateExtractedSyntaxTrees);
            return extracted;
        }

        public static CSharpCompilation TemplateResolvePass(CSharpCompilation compilation, IEnumerable<ClassTemplate> templates)
        {
            var templateGroups = templates.GroupBy(t => t.TemplateName)
                .ToDictionary(g => g.Key, g => new ClassTemplateGroup(g));
            var templateResolvedSyntaxTrees = (
                from tree in compilation.SyntaxTrees.AsParallel()
                select TemplateResolveRewriter.ResolveFor(tree, compilation.GetSemanticModel(tree), templateGroups)
                                              .WithFilePath(tree.FilePath)).ToList();
            var resolved = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateResolvedSyntaxTrees);
            return resolved;
        }

        public static CSharpCompilation TemplateInstantiatePass(CSharpCompilation compilation,
            IEnumerable<ClassTemplate> templates)
        {
            var newSyntaxTrees = templates.SelectMany(t => t.Instaniations);
            var instantiated = compilation.AddSyntaxTrees(newSyntaxTrees);
            return instantiated;
        }

        public static void Compile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            switch (extension)
            {
                case ".sln":
                    CompileSolution(filePath);
                    break;
                case ".cs":
                    CompileSingleCSharpFile(filePath);
                    break;
                default:
                    throw new ArgumentException("Path should point to a file ending with `.cs` or `.sln`.", filePath);
            }
        }

    }
}
