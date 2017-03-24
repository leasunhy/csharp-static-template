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
        private Dictionary<string, ClassDeclarationSyntax> ClassDefs;
        private SemanticModel SemanticModel;

        internal IDictionary<string, ClassDeclarationSyntax> TemplateInstantiations { get; }

        public TemplateResolveRewriter(SemanticModel semanticModel, IEnumerable<ClassDeclarationSyntax> classDefs)
        {
            SemanticModel = semanticModel;
            ClassDefs = classDefs.ToDictionary(def => def.Identifier.ToString());
            TemplateInstantiations = new Dictionary<string, ClassDeclarationSyntax>();
        }

        // currently we only allow templates to appear unnested
        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var orig = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

            // if no template instantiations found
            if (TemplateInstantiations.Count == 0)
                return orig;

            return orig.AddMembers(TemplateInstantiations.Values.ToArray());
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            // if the node does not refer to a template, just return it unmodified
            var templateName = node.Identifier.ToString();
            if (!ClassDefs.ContainsKey(templateName))
                return node;

            // check if the instantiation is already done
            var instName = GetInstantiationName(templateName, node.TypeArgumentList);
            if (!TemplateInstantiations.ContainsKey(instName))
            {
                TemplateInstantiations.Add(instName, InstantiateTemplate(templateName, instName, node.TypeArgumentList));
            }

            return IdentifierName(instName).WithLeadingTrivia(node.GetLeadingTrivia())
                                           .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        private ClassDeclarationSyntax InstantiateTemplate(string templateName, string instName, TypeArgumentListSyntax typeArgs)
        {
            var template = ClassDefs[templateName];
            var rewriter = new TemplateInstantiationRewriter(template, instName, typeArgs.Arguments);
            return (ClassDeclarationSyntax)rewriter.Visit(template);
        }

        private string GetInstantiationName(string templateName, TypeArgumentListSyntax typeArgs) =>
            $"{templateName}#{string.Join("_", typeArgs.Arguments.ToString())}#";
    }
}
