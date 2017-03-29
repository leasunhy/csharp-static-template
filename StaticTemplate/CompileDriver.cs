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
        #region Driver Methods
        /// <summary>
        /// Compile either a solution file or a C# source file, according to the extension of its filename.
        /// </summary>
        /// <param name="filePath">The path to the file to be compiled.</param>
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

        /// <summary>
        /// Compile a C# solution with static templates.
        /// </summary>
        /// <param name="solutionPath">The path to the solution file.</param>
        public static void CompileSolution(string solutionPath)
        {
            // create Roslyn workspace and load the solution
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(solutionPath).Result;

            // get dependency sets of the projects in the solution
            var dependencySets = solution.GetProjectDependencyGraph().GetDependencySets();

            // compile the projects one by one
            foreach (var set in dependencySets)
            {
                set.AsParallel().ForAll(pid =>
                {
                    var project = solution.GetProject(pid);
                    if (project.Language != "C#")
                    {
                        // TODO(leasunhy): how to handle non C# projects?
                        Console.WriteLine($"Skipping non-c# project {project.FilePath}");
                        return;
                    }
                    // get the compilation
                    var compilation = (CSharpCompilation)project.GetCompilationAsync().Result;
                    // compile!
                    var emitResult = CompileAndEmit(ref compilation);
#if DEBUG
                    // output all processed syntax trees in the solution for debug
                    foreach (var compilationSyntaxTree in compilation.SyntaxTrees)
                    {
                        Console.WriteLine(compilationSyntaxTree.ToString());
                    }
#endif
                    if (!emitResult.Success)
                    {
                        Console.Error.WriteLine($"Failed to compile project: {project.FilePath}");
                    }
                    // output compiler diagnostics
                    foreach (var diag in emitResult.Diagnostics)
                    {
                        if (diag.Severity == DiagnosticSeverity.Error || diag.Severity == DiagnosticSeverity.Warning)
                        {
                            Console.Error.WriteLine(diag);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Compile a single C# file with static templates.
        /// Note that, since assembly references are not easy to infer from the source code,
        /// only a fixed set of references are added. If you need custom references, 
        /// please use <see cref="CompileSolution"/> instead.
        /// </summary>
        /// <param name="filePath">The path to the C# source file.</param>
        public static void CompileSingleCSharpFile(string filePath)
        {
            // read the source text and parse it into a syntax tree
            var sourceText = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText).WithFilePath(filePath);

            // prepare references
            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
            var referenceNames = new [] { "mscorlib", "System", "System.Core", "System.Runtime", "System.Linq" };
            var referencePaths = referenceNames.Select(n => Path.Combine(runtimePath, n + ".dll")).ToList();
            var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p)).ToList();

            // prepare options
            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true);
            var assemblyName = Path.GetFileNameWithoutExtension(filePath);

            // create compilation
            var compilation = CSharpCompilation.Create(assemblyName, new[] {syntaxTree}, references, options);

            // compile!
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

        /// <summary>
        /// The method compiles <paramref name="compilation"/> with static templates and emits as an executable.
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="emitPath"></param>
        /// <returns></returns>
        public static EmitResult CompileAndEmit(ref CSharpCompilation compilation, string emitPath = null)
        {
            // Extract templates, resolve template usages and instantantiate the templates according usages
            var extracted = TemplateExtractPass(compilation, out List<ClassTemplate> templates);
            var resolved = TemplateResolvePass(extracted, templates);
            var instantiated = TemplateInstantiatePass(resolved, templates);

            // return the resulting compilation
            compilation = instantiated;

            // TODO(leasunhy): what if we need `.dll` instead of `.exe`?
            var emitResult = compilation.Emit(emitPath ?? compilation.AssemblyName + ".exe");
            return emitResult;
        }
        #endregion

        #region Compilation Passes
        /// <summary>
        /// Extract and remove templates from the syntax trees in the compilation in parallel.
        /// </summary>
        /// <param name="compilation">The compilation from which templates are extracted.</param>
        /// <param name="templates">The extracted templates.</param>
        /// <returns>The compilation containing syntax trees with templates removed.</returns>
        private static CSharpCompilation TemplateExtractPass(CSharpCompilation compilation, out List<ClassTemplate> templates)
        {
            // simutaneously extract and remove the templates from the syntax trees in parallel
            var templateExtractResults = (
                from t in compilation.SyntaxTrees.AsParallel()
                select TemplateExtractRewriter.Extract(t, compilation.GetSemanticModel(t))).ToList();
            // new syntax trees
            var templateExtractedSyntaxTrees = templateExtractResults.Select(t => t.Item1).ToList();
            // each syntax tree may have multiple templates; we use SelectMany (flatMap) here
            templates = templateExtractResults.SelectMany(t => t.Item2).ToList();
            // replace syntax trees
            var extracted = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateExtractedSyntaxTrees);
            return extracted;
        }

        /// <summary>
        /// Given templates and syntax trees that make use of the templates, resolve the usages and instantiate
        /// the templates upon being used.
        /// </summary>
        /// <param name="compilation">The compilation containing syntax trees that uses the templates.</param>
        /// <param name="templates">The available templates.</param>
        /// <returns>The compilation with uses of templates resolved.</returns>
        private static CSharpCompilation TemplateResolvePass(CSharpCompilation compilation, IEnumerable<ClassTemplate> templates)
        {
            // construct template groups; see ClassTemplateGroup for details.
            var templateGroups = templates.GroupBy(t => t.TemplateName)
                .ToDictionary(g => g.Key, g => new ClassTemplateGroup(g));
            // resolve the usages of templates and instantiate the templates with proper arguements
            var templateResolvedSyntaxTrees = (
                from tree in compilation.SyntaxTrees.AsParallel()
                select TemplateResolveRewriter.ResolveFor(tree, compilation.GetSemanticModel(tree), templateGroups)
                                              .WithFilePath(tree.FilePath)).ToList();
            // replace syntax trees
            var resolved = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateResolvedSyntaxTrees);
            return resolved;
        }

        /// <summary>
        /// Add template instantiations (which are standalone syntax trees) into the compilation.
        /// </summary>
        /// <param name="compilation">The compilation to which the syntax trees to be added.</param>
        /// <param name="templates">The templates.</param>
        /// <returns>The compilation with new syntax trees added.</returns>
        private static CSharpCompilation TemplateInstantiatePass(CSharpCompilation compilation,
            IEnumerable<ClassTemplate> templates)
        {
            var newSyntaxTrees = templates.SelectMany(t => t.Instantiations);
            var instantiated = compilation.AddSyntaxTrees(newSyntaxTrees);
            return instantiated;
        }
        #endregion
    }
}
