using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StaticTemplate
{
    internal class TemplateExtractRewriter : CSharpSyntaxRewriter
    {
        private List<ClassTemplate> classTemplates = new List<ClassTemplate>();
        public List<ClassTemplate> ClassTemplates => classTemplates;
        private SemanticModel semanticModel = null;

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!ClassTemplate.IsClassTemplate(node))
                return node;
            classTemplates.Add(new ClassTemplate(semanticModel, node));
            return null;
        }

        // TODO(leasunhy): remove this method in favor of one-rewriter-for-one-file approach
        public SyntaxTree ExtractFor(SemanticModel semanticModel, SyntaxTree tree)
        {
            this.semanticModel = semanticModel;
            var newTree = Visit(tree.GetRoot()).SyntaxTree;
            this.semanticModel = null;
            return newTree;
        }
    }
}