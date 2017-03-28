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

namespace StaticTemplate.Rewriters
{
    internal class TemplateExtractRewriter : CSharpSyntaxRewriter
    {
        private readonly List<ClassTemplate> _classTemplates = new List<ClassTemplate>();
        public IEnumerable<ClassTemplate> ClassTemplates => _classTemplates;
        private SemanticModel _semanticModel;

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!ClassTemplate.IsClassTemplate(node))
                return node;
            _classTemplates.Add(new ClassTemplate(_semanticModel, node));
            return null;
        }

        // TODO(leasunhy): remove this method in favor of one-rewriter-for-one-file approach
        public SyntaxTree ExtractFor(SemanticModel semanticModel, SyntaxTree tree)
        {
            _semanticModel = semanticModel;
            var newTree = Visit(tree.GetRoot()).SyntaxTree;
            _semanticModel = null;
            return newTree;
        }
    }
}