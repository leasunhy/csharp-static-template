using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StaticTemplate.Rewriters
{
    /// <summary>
    /// Resolves usage of templates in a syntax tree.
    /// </summary>
    internal class TemplateResolveRewriter : CSharpSyntaxRewriter
    {
        private readonly IReadOnlyDictionary<string, ClassTemplateGroup> _templateGroups;
        private SemanticModel _semanticModel;

        private TemplateResolveRewriter(SemanticModel semanticModel,
            IReadOnlyDictionary<string, ClassTemplateGroup> templateGroups)
        {
            _semanticModel = semanticModel;
            _templateGroups = templateGroups;
        }

        /// <summary>
        /// Visits a <see cref="GenericNameSyntax"/>. If it uses a template, resolve it.
        /// <para>Note that this method shall not be called directly. Please use <see cref="ResolveFor"/> instead.</para>
        /// </summary>
        /// <param name="node">The syntax node to visit.</param>
        /// <returns>
        /// <paramref name="node"/> if it is not a usage of template, or a new node with template usage resolved.
        /// </returns>
        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            // if the node does not refer to a template, just return it unmodified
            var templateName = node.Identifier.ToString();
            ClassTemplateGroup group;
            if (!_templateGroups.TryGetValue(templateName, out group))
                return node;

            // ?: t => SemanticModel.GetTypeInfo(t).Type ?? SemanticModel.GetDeclaredSymbol(t)
            var symbols = node.TypeArgumentList.Arguments.Select(t => _semanticModel.GetTypeInfo(t).Type)
                                                         .Cast<INamedTypeSymbol>().ToList();
            if (symbols.Any(s => s == null))
            {
                throw new Exception("Can't get symbol for some wtemplate type argument.");
            }
            // check if the instantiation is already done
            var instName = group.GetInstantiationNameFor(symbols);
            group.Instantiate(symbols);

            return IdentifierName(instName).WithLeadingTrivia(node.GetLeadingTrivia())
                                           .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        /// <summary>
        /// Resolves template usages for a syntax tree, given its semantic model and the templates in scope.
        /// </summary>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="semanticModel">The semantic model for <paramref name="tree"/></param>
        /// <param name="templateGroups">The templates in scope, grouped by their names.</param>
        /// <returns></returns>
        public static SyntaxTree ResolveFor(SyntaxTree tree, SemanticModel semanticModel,
            IReadOnlyDictionary<string, ClassTemplateGroup> templateGroups)
        {
            var rewriter = new TemplateResolveRewriter(semanticModel, templateGroups);
            return rewriter.Visit(tree.GetRoot()).SyntaxTree;
        }
    }
}
