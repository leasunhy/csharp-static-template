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
using Microsoft.CodeAnalysis.MSBuild;
using System.Runtime.InteropServices;

namespace StaticTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                ShowUsage();
                Environment.Exit(1);
            }

            var solutionPath = args[0];
            if (!File.Exists(solutionPath))
            {
                Console.Error.WriteLine($"File does not exist: {solutionPath}");
                Environment.Exit(1);
            }

            CompileDriver.CompileSolution(solutionPath);
        }

        private static void ShowUsage()
        {
            var myName = AppDomain.CurrentDomain.FriendlyName;
            Console.Error.WriteLine($"Usage: {myName} solution-file");
        }
    }
}
