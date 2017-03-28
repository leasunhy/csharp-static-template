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
    internal class TemplateResolveRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, ClassTemplateGroup> _templateGroups;
        private SemanticModel _semanticModel;

        public TemplateResolveRewriter(IEnumerable<ClassTemplate> classDefs)
        {
            _templateGroups = classDefs.GroupBy(t => t.TemplateName)
                .ToDictionary(g => g.Key, g => new ClassTemplateGroup(g));
        }

        // TODO(leasunhy): remove this method in favor of one-rewriter-for-one-syntax-tree approach
        public SyntaxTree ResolveFor(SemanticModel semanticModel, SyntaxTree tree)
        {
            _semanticModel = semanticModel;
            return Visit(tree.GetRoot()).SyntaxTree;
        }

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
    }
}
