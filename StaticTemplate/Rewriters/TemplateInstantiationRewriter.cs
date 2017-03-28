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
    public class TemplateInstantiationRewriter : CSharpSyntaxRewriter
    {
        private List<INamedTypeSymbol> TypeArgs;
        private ClassDeclarationSyntax Template;
        private Dictionary<string, INamedTypeSymbol> TypeMap;
        private SyntaxToken InstName;

        private TemplateInstantiationRewriter(ClassDeclarationSyntax template, string instName, IEnumerable<INamedTypeSymbol> typeArgs)
        {
            TypeArgs = typeArgs.ToList();
            Template = template;
            if (template.TypeParameterList.Parameters.Count() != TypeArgs.Count)
                throw new InvalidOperationException("Type arguments should be as many as type parameters");
            TypeMap = template.TypeParameterList
                              .Parameters
                              .Zip(typeArgs, (p, a) => Tuple.Create(p.ToString(), a))
                              .ToDictionary(_ => _.Item1, _ => _.Item2);
            InstName = IdentifierName(instName).Identifier;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var orig = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            var typeparams = orig.TypeParameterList;
            return orig.RemoveNode(typeparams, SyntaxRemoveOptions.KeepExteriorTrivia).WithIdentifier(InstName);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            // note that we don't need to check whether node is a TypeSyntax,
            // because the name of variable, method, etc, is a IdentifierToken,
            // not IdentifierNameSyntax.
            if (TypeMap.TryGetValue(node.Identifier.ToString(), out INamedTypeSymbol target))
            {
                if (target.SpecialType != SpecialType.None)
                {
                    return PredefinedType(ParseToken(target.ToDisplayString()))
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia());
                }
                return ParseTypeName(target.ToDisplayString())
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            return node;
        }

        public static SyntaxTree InstantiateFor(CompilationUnitSyntax compilationUnit,
                                                ClassDeclarationSyntax template,
                                                string instName,
                                                IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var node = new TemplateInstantiationRewriter(template, instName, typeArgs).Visit(compilationUnit);
            return node.SyntaxTree;
        }
    }
}
