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
    /// <summary>
    /// A <see cref="CSharpSyntaxRewriter"/> that extracts and removes the templates from a syntax tree.
    /// </summary>
    internal class TemplateExtractRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;

        /// <summary>
        /// The extracted templates.
        /// </summary>
        public IEnumerable<ClassTemplate> ClassTemplates => _classTemplates;
        private readonly List<ClassTemplate> _classTemplates = new List<ClassTemplate>();

        private TemplateExtractRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        /// <summary>
        /// This method checks whether the <paramref name="node"/> is a template declaration,
        /// and if it is, adds it to <see cref="ClassTemplates"/>.
        /// <para>
        /// Note that although this method is public, it shall not be called directly. Call <see cref="Extract"/> instead.
        /// </para>
        /// </summary>
        /// <param name="node"></param>
        /// <returns>
        /// <paramref name="node"/> if it is not a template, null otherwise
        /// (resulting in the removal of the node in the original syntax tree).
        /// </returns>
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!ClassTemplate.IsClassTemplate(node))
                return node;
            _classTemplates.Add(new ClassTemplate(_semanticModel, node));
            return null;
        }

        /// <summary>
        /// Extracts and removes templates from a syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to be processed.</param>
        /// <param name="semanticModel">The semantic model for the syntax tree.</param>
        /// <returns></returns>
        public static (SyntaxTree, IEnumerable<ClassTemplate>) Extract(SyntaxTree tree, SemanticModel semanticModel)
        {
            var rewriter = new TemplateExtractRewriter(semanticModel);
            var newTree = rewriter.Visit(tree.GetRoot()).SyntaxTree;
            return (newTree, rewriter.ClassTemplates);
        }
    }
}