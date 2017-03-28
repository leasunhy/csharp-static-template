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
        private readonly SemanticModel _semanticModel;

        private TemplateExtractRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!ClassTemplate.IsClassTemplate(node))
                return node;
            _classTemplates.Add(new ClassTemplate(_semanticModel, node));
            return null;
        }

        public static (SyntaxTree, IEnumerable<ClassTemplate>) Extract(SyntaxTree tree, SemanticModel semanticModel)
        {
            var rewriter = new TemplateExtractRewriter(semanticModel);
            var newTree = rewriter.Visit(tree.GetRoot()).SyntaxTree;
            return (newTree, rewriter.ClassTemplates);
        }
    }
}