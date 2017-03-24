using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace StaticTemplate
{
    public class ClassTemplate
    {
        public ClassDeclarationSyntax Syntax { get; }
        public bool IsSpecialized { get; }
        public IDictionary<TypeParameterSyntax, TypeSyntax> SpecialiedTypeArgs { get; }
        public string TemplateName { get { return Syntax.Identifier.ToString(); } }
        public string FullName { get { return $"{TemplateName}<{string.Join(", ", TypeParams)}>"; } }
        public int TypeParamCount { get { return TypeParams.Count(); } }
        public int RemainingParamCount { get { return TypeParamCount - SpecialiedTypeArgs.Count; } }
        public IEnumerable<TypeParameterSyntax> TypeParams { get { return Syntax.TypeParameterList.Parameters; } }

        public ClassTemplate(ClassDeclarationSyntax template)
        {
            Syntax = template;

            var constraintClauses = Syntax.ChildNodes().OfType<TypeParameterConstraintClauseSyntax>().ToList();
            if (constraintClauses.Count != 0)
            {
                IsSpecialized = true;
                Debug.Assert(constraintClauses.Count == TypeParamCount, "Currently only full specialization is supported.");
            }
        }

        public ClassDeclarationSyntax Instantiate(string instantiationName, IEnumerable<TypeSyntax> typeArgs)
        {
            var rewriter = new TemplateInstantiationRewriter(Syntax, instantiationName, typeArgs);
            return (ClassDeclarationSyntax)rewriter.Visit(Syntax);
        }
    }
}
