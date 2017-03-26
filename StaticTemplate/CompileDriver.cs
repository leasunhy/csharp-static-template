using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.MSBuild;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StaticTemplate
{
    public class CompileDriver
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

                var emitResult = Emit(ref compilation);
                if (!emitResult.Success)
                {
                    Console.Error.WriteLine($"Failed to compile project: {project.FilePath}");
                }
                foreach (var diag in emitResult.Diagnostics)
                {
                    Console.Error.WriteLine(diag);
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

            var emitResult = Emit(ref compilation);
            if (!emitResult.Success)
            {
                Console.Error.WriteLine($"Failed to compile file: {filePath}");
            }
            foreach (var diag in emitResult.Diagnostics)
            {
                Console.Error.WriteLine(diag);
            }
        }

        public static EmitResult Emit(ref CSharpCompilation compilation, string emitPath = null)
        {
            var templateExtractor = new TemplateExtractRewriter();
            var templateExtractedSyntaxTrees =
                from sourceTree in compilation.SyntaxTrees
                select templateExtractor.Visit(sourceTree.GetRoot()).SyntaxTree;
            var extracted = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateExtractedSyntaxTrees);

            var templates = templateExtractor.ClassTemplates;
            var templateResolver = new TemplateResolveRewriter(templates);
            var templateResolvedSyntaxTrees =
                from sourceTree in compilation.SyntaxTrees
                select templateResolver.ResolveFor(extracted.GetSemanticModel(sourceTree), sourceTree);
            var resolved = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateResolvedSyntaxTrees);
            compilation = resolved;

            var emitResult = compilation.Emit(emitPath ?? compilation.AssemblyName);
            return emitResult;
        }

        public static void Compile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == "sln")
            {
                CompileSolution(filePath);
            }
            else if (extension == "cs")
            {
                CompileSingleCSharpFile(filePath);
            }
            else
            {
                throw new ArgumentException("Path should point to a file ending with `.cs` or `.sln`.", filePath);
            }
        }

    }
}
