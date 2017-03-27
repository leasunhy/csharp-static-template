using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StaticTemplate
{
    public class ClassTemplate
    {
        public ClassDeclarationSyntax Syntax { get; }
        public bool IsSpecialized { get; }
        public int SpecialiedTypeArgCount { get; }
        public string TemplateName { get { return Syntax.Identifier.ToString(); } }
        public IEnumerable<TypeSyntax> SpecialiedTypeArgList { get; }
        public string FullName { get { return $"{TemplateName}<{string.Join(", ", TypeParams)}>"; } }
        public IEnumerable<TypeParameterSyntax> TypeParams { get { return Syntax.TypeParameterList.Parameters; } }
        public int TypeParamCount { get { return Syntax.TypeParameterList.Parameters.Count; } }
        public int RemainingParamCount { get { return TypeParamCount - SpecialiedTypeArgCount; } }

        public ClassTemplate(ClassDeclarationSyntax template)
        {
            Syntax = template;

            var constraintClauses = Syntax.ChildNodes().OfType<TypeParameterConstraintClauseSyntax>().ToList();
            SpecialiedTypeArgCount = constraintClauses.Count;
            IsSpecialized = SpecialiedTypeArgCount != 0;
            SpecialiedTypeArgList = new List<TypeSyntax>();

            if (IsSpecialized)
            {
                var argDict = TypeParams.ToDictionary(p => p.ToString(), p => default(TypeSyntax));
                foreach (var clause in constraintClauses)
                {
                    var constraint = clause.Constraints.FirstOrDefault() as TypeConstraintSyntax;
                    Debug.Assert(clause.Constraints.Count == 1 && constraint != null,
                        "Currently only explicit specialization is supported, exactly one IsType<T> constraint expected.");
                    var type = constraint.Type as GenericNameSyntax;
                    Debug.Assert(type.Identifier.ToString() == "IsType");
                    Debug.Assert(type.TypeArgumentList.Arguments.Count == 1);
                    argDict[clause.Name.ToString()] = type.TypeArgumentList.Arguments.Single();
                }
                SpecialiedTypeArgList = TypeParams.Select(p => argDict[p.ToString()]).ToList();

                // clean the constraint clauses
                Syntax = Syntax.RemoveNodes(constraintClauses, SyntaxRemoveOptions.KeepExteriorTrivia);
            }
        }

        public ClassDeclarationSyntax Instantiate(string instantiationName, IEnumerable<TypeSyntax> typeArgs)
        {
            var rewriter = new TemplateInstantiationRewriter(Syntax, instantiationName, typeArgs);
            var syntaxTree = (ClassDeclarationSyntax) rewriter.Visit(Syntax);
            return syntaxTree;
        }
    }
}
