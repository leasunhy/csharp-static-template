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

namespace StaticTemplate
{
    internal class TemplateResolveRewriter : CSharpSyntaxRewriter
    {
        private Dictionary<string, ClassTemplateGroup> TemplateGroups;
        private SemanticModel SemanticModel;

        public TemplateResolveRewriter(IEnumerable<ClassTemplate> classDefs)
        {
            TemplateGroups = classDefs.GroupBy(t => t.TemplateName)
                .ToDictionary(g => g.Key, g => new ClassTemplateGroupBuilder(g).Build());
        }

        // TODO(leasunhy): remove this method in favor of one-rewriter-for-one-syntax-tree approach
        public SyntaxTree ResolveFor(SemanticModel semanticModel, SyntaxTree tree)
        {
            SemanticModel = semanticModel;
            return Visit(tree.GetRoot()).SyntaxTree;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            // if the node does not refer to a template, just return it unmodified
            var templateName = node.Identifier.ToString();
            ClassTemplateGroup group;
            if (!TemplateGroups.TryGetValue(templateName, out group))
                return node;

            // check if the instantiation is already done
            var instName = group.GetInstantiationNameFor(node.TypeArgumentList.Arguments);
            group.Instantiate(node.TypeArgumentList.Arguments);

            return IdentifierName(instName).WithLeadingTrivia(node.GetLeadingTrivia())
                                           .WithTrailingTrivia(node.GetTrailingTrivia());
        }
    }
}
