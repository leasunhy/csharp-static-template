using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Semantics;
using System.Runtime.InteropServices;

namespace StaticTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var paths = args.ToList();
            paths = new List<string>() { "test.cs" };  // for debug purpose
            if (paths.Count == 0) return;
            var compilation = CreateCompilation(paths);

            var newSyntaxTrees = new List<SyntaxTree>();
            foreach (var sourceTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(sourceTree);
                var templateExtractRewriter = new TemplateExtractRewriter();
                var templateExtractedSyntaxNode = templateExtractRewriter.Visit(sourceTree.GetRoot());
                var templateInstantiationRewriter = new TemplateResolveRewriter(semanticModel,
                                                            templateExtractRewriter.ClassTemplates);
                var newSyntaxNode = templateInstantiationRewriter.Visit(templateExtractedSyntaxNode);
                newSyntaxTrees.Add(newSyntaxNode.SyntaxTree);
            }
            compilation = CSharpCompilation.Create("test", newSyntaxTrees, compilation.References,
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            foreach (var st in compilation.SyntaxTrees)
            {
                Console.WriteLine(st.ToString());
            }

            var emitResult = compilation.Emit("test.exe");
            if (!emitResult.Success)
            {
                Console.WriteLine("Failed to compile: " + string.Join(",", paths));
                foreach (var diag in emitResult.Diagnostics)
                    Console.WriteLine(diag);
            }
        }

        private static Compilation CreateCompilation(IEnumerable<string> paths)
        {
            // read and parse source files
            var sourceTexts = paths.Select(p => File.ReadAllText(p)).ToList();
            var syntaxTrees = paths.Zip(sourceTexts,
                (p, t) => CSharpSyntaxTree.ParseText(t).WithFilePath(p)).ToList();

            // prepare references
            var runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
            var referenceNames = new string[] { "mscorlib", "System", "System.Core", "System.Runtime", "System.Linq"};
            var referencePaths = referenceNames.Select(n => Path.Combine(runtimePath, n + ".dll")).ToList();
            var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p)).ToList();

            var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
            return CSharpCompilation.Create("test", syntaxTrees, references, options);
        }
    }
}
