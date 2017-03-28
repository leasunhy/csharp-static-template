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
    /// Rewrites a template into a template instance for given type arguments.
    /// </summary>
    public class TemplateInstantiationRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, INamedTypeSymbol> _typeMap;
        private readonly SyntaxToken _instName;
        private bool _inClassDeclaration = false;

        private TemplateInstantiationRewriter(ClassDeclarationSyntax template, string instName,
                                              IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var typeArgLst = typeArgs.ToList();
            if (template.TypeParameterList.Parameters.Count != typeArgLst.Count)
                throw new InvalidOperationException("Type arguments should be as many as type parameters");
            _typeMap = template.TypeParameterList
                               .Parameters
                               .Zip(typeArgLst, (p, a) => Tuple.Create(p.ToString(), a))
                               .ToDictionary(_ => _.Item1, _ => _.Item2);
            _instName = IdentifierName(instName).Identifier;
        }

        // TODO(leasunhy): enable nested static templates.
        /// <summary>
        /// Visits a <see cref="ClassDeclarationSyntax"/> which must be the template
        /// if working on a template isolation.
        /// <para>This method removes type parameter list of the template as well.</para>
        /// </summary>
        /// <param name="node">The syntax node to visit.</param>
        /// <returns></returns>
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _inClassDeclaration = true;
            var orig = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            _inClassDeclaration = false;
            var typeparams = orig.TypeParameterList;
            return orig.RemoveNode(typeparams, SyntaxRemoveOptions.KeepExteriorTrivia).WithIdentifier(_instName);
        }

        /// <summary>
        /// Rewrites the <see cref="IdentifierNameSyntax"/> with the type parameters of the
        /// template substited with the type arguments.
        /// </summary>
        /// <param name="node">The syntax node to visit.</param>
        /// <returns>The syntax node as described above.</returns>
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            // We only handle the IdentifierNameSyntax nodes inside a ClassDeclarationSyntax.
            // Given we are working on a template isolation, the ClassDeclarationSyntax must
            // be the template itself.
            if (!_inClassDeclaration) return node;

            // note that we don't need to check whether node is a TypeSyntax,
            // because the name of variable, method, etc, is a IdentifierToken,
            // not IdentifierNameSyntax.
            if (_typeMap.TryGetValue(node.Identifier.ToString(), out INamedTypeSymbol target))
            {
                return ParseTypeName(target.ToDisplayString())
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }
            return node;
        }

        /// <summary>
        /// Rewrites a template into a template instance for a given list of arguments.
        /// </summary>
        /// <param name="compilationUnit">
        /// The template isolation as described in <see cref="TemplateIsolationRewriter"/>.
        /// </param>
        /// <param name="template">The template to rewrite.</param>
        /// <param name="instName">The name of the template instance.</param>
        /// <param name="typeArgs">The list of type arguments for the instantiation.</param>
        /// <returns></returns>
        public static SyntaxTree InstantiateFor(CompilationUnitSyntax compilationUnit,
                                                ClassDeclarationSyntax template,
                                                string instName,
                                                IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var rewriter = new TemplateInstantiationRewriter(template, instName, typeArgs);
            var node = rewriter.Visit(compilationUnit);
            return node.SyntaxTree;
        }
    }
}
