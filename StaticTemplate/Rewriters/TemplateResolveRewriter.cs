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
        private Dictionary<string, ClassTemplateGroupBuilder> TemplateGroupBuilders;
        private Dictionary<string, ClassTemplateGroup> TemplateGroups;
        private SemanticModel SemanticModel;

        internal IDictionary<string, ClassDeclarationSyntax> TemplateInstantiations { get; }

        public TemplateResolveRewriter(SemanticModel semanticModel, IEnumerable<ClassDeclarationSyntax> classDefs)
        {
            SemanticModel = semanticModel;
            TemplateInstantiations = new Dictionary<string, ClassDeclarationSyntax>();

            TemplateGroupBuilders = new Dictionary<string, ClassTemplateGroupBuilder>();
            foreach (var classDef in classDefs)
            {
                var key = classDef.Identifier.ToString();
                ClassTemplateGroupBuilder builder;
                if (!TemplateGroupBuilders.TryGetValue(key, out builder))
                    TemplateGroupBuilders[key] = builder = new ClassTemplateGroupBuilder();
                builder.AddTemplate(new ClassTemplate(classDef));
            }
        }

        // currently we only allow templates to appear unnested
        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var orig = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

            // if no template instantiations found
            if (TemplateInstantiations.Count == 0)
                return orig;

            TemplateGroups = TemplateGroupBuilders.Select(pair => Tuple.Create(pair.Key, pair.Value.Build()))
                                                  .ToDictionary(_ => _.Item1, _ => _.Item2);

            return orig.AddMembers(TemplateInstantiations.Values.ToArray());
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            // if the node does not refer to a template, just return it unmodified
            var templateName = node.Identifier.ToString();
            if (!TemplateGroupBuilders.ContainsKey(templateName))
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
            var group = TemplateGroups[templateName];
            return group.FindTemplateForArguments(typeArgs.Arguments).Instantiate(instName, typeArgs.Arguments);
        }

        private string GetInstantiationName(string templateName, TypeArgumentListSyntax typeArgs) =>
            $"{templateName}#{string.Join("_", typeArgs.Arguments.ToString())}#";
    }
}
