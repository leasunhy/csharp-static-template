using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticTemplate.Rewriters
{
    class TemplateIsolationRewriter : CSharpSyntaxRewriter
    {
        private readonly ClassDeclarationSyntax target;

        private TemplateIsolationRewriter(ClassDeclarationSyntax target)
        {
            this.target = target;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return base.VisitNamespaceDeclaration(node);
            // TODO(leasunhy): extra processing here: what other types of nodes shall be removed?
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node != target)
                return null;
            return ClassTemplate.CleanClassTemplate(node);
        }

        public static CompilationUnitSyntax IsolateFor(ClassDeclarationSyntax target)
        {
            return (CompilationUnitSyntax)new TemplateIsolationRewriter(target).Visit(target.SyntaxTree.GetRoot());
        }
    }
}
