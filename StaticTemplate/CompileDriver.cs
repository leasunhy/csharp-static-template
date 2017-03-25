using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.MSBuild;

namespace StaticTemplate
{
    public class CompileDriver
    {
        public static void CompileSolution(string solutionPath)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(solutionPath).Result;
            var projectIds = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects();
            foreach (var pid in projectIds)
            {
                var project = solution.GetProject(pid);
                var compilation = project.GetCompilationAsync().Result;

                var templateExtractor = new TemplateExtractRewriter();
                var templateExtractedSyntaxTrees =
                    from sourceTree in compilation.SyntaxTrees
                    select templateExtractor.Visit(sourceTree.GetRoot()).SyntaxTree;
                compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateExtractedSyntaxTrees);

                var templates = templateExtractor.ClassTemplates;
                var templateResolver = new TemplateResolveRewriter(templates);
                var templateResolvedSyntaxTrees =
                    from sourceTree in compilation.SyntaxTrees
                    select templateResolver.ResolveFor(compilation.GetSemanticModel(sourceTree), sourceTree);
                compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(templateResolvedSyntaxTrees);

                var emitResult = compilation.Emit(project.OutputFilePath ?? project.AssemblyName);
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
    }
}
