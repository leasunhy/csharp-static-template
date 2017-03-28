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
using StaticTemplate.Rewriters;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StaticTemplate
{
    public class ClassTemplate
    {
        public string OriginalFilePath { get; }
        public ClassDeclarationSyntax Syntax { get; }
        public CompilationUnitSyntax TemplateIsolation { get; }
        public bool IsSpecialized { get; }
        public int SpecialiedTypeArgCount { get; }
        public string TemplateName { get { return Syntax.Identifier.ToString(); } }
        public IEnumerable<INamedTypeSymbol> SpecialiedTypeArgList { get; }
        public string FullName { get { return $"{TemplateName}<{string.Join(", ", TypeParams)}>"; } }
        public IEnumerable<TypeParameterSyntax> TypeParams { get { return Syntax.TypeParameterList.Parameters; } }
        public int TypeParamCount { get { return Syntax.TypeParameterList.Parameters.Count; } }
        public int RemainingParamCount { get { return TypeParamCount - SpecialiedTypeArgCount; } }

        private Dictionary<string, SyntaxTree> instantiations = new Dictionary<string, SyntaxTree>();
        public IEnumerable<SyntaxTree> Instaniations => instantiations.Values;

        public ClassTemplate(SemanticModel semanticModel, ClassDeclarationSyntax template)
        {
            OriginalFilePath = template.SyntaxTree.FilePath;
            Syntax = template;
            TemplateIsolation = TemplateIsolationRewriter.IsolateFor(template);

            var constraintClauses = Syntax.ChildNodes().OfType<TypeParameterConstraintClauseSyntax>().ToList();
            SpecialiedTypeArgCount = constraintClauses.Count;
            IsSpecialized = SpecialiedTypeArgCount != 0;
            SpecialiedTypeArgList = new List<INamedTypeSymbol>();

            if (IsSpecialized)
            {
                var argDict = TypeParams.ToDictionary(p => p.ToString(), p => default(INamedTypeSymbol));
                foreach (var clause in constraintClauses)
                {
                    var constraint = clause.Constraints.FirstOrDefault() as TypeConstraintSyntax;
                    Debug.Assert(clause.Constraints.Count == 1 && constraint != null,
                        "Currently only explicit specialization is supported, exactly one IsType<T> constraint expected.");
                    var type = constraint.Type as GenericNameSyntax;
                    Debug.Assert(type.Identifier.ToString() == "IsType");
                    Debug.Assert(type.TypeArgumentList.Arguments.Count == 1);
                    var typeSyntax = type.TypeArgumentList.Arguments.Single();
                    var typeSymbol = (INamedTypeSymbol)semanticModel.GetTypeInfo(typeSyntax).Type;
                    argDict[clause.Name.ToString()] = typeSymbol;
                }
                SpecialiedTypeArgList = TypeParams.Select(p => argDict[p.ToString()]).ToList();

                // clean the constraint clauses
                Syntax = Syntax.RemoveNodes(constraintClauses, SyntaxRemoveOptions.KeepExteriorTrivia);
            }
        }

        public SyntaxTree Instantiate(string instantiationName, IEnumerable<INamedTypeSymbol> typeArgs)
        {
            if (!instantiations.ContainsKey(instantiationName))
            {
                var syntaxTree = TemplateInstantiationRewriter.InstantiateFor(
                                        TemplateIsolation, Syntax, instantiationName, typeArgs);
                instantiations[instantiationName] = syntaxTree;
                return syntaxTree;
            }
            return null;
        }

        /// <summary>
        /// This method determines whether <param name="syntax"></param> is a template.
        /// </summary>
        /// <param name="syntax"></param>
        /// <returns></returns>
        public static bool IsClassTemplate(ClassDeclarationSyntax syntax)
        {
            // determine whether there are [StaticTemplate] attributes
            var stAttrList = syntax.AttributeLists.Select(
                (lst, li) => lst.DescendantNodes()
                                .OfType<AttributeSyntax>()
                                .Where(n => n.Name.ToString() == "StaticTemplate")).ToList();
            return stAttrList.Any(lst => lst.Any());
        }

        /// <summary>
        /// This method cleans the [StaticTemplate] attributes.
        /// </summary>
        /// <param name="syntax"></param>
        /// <returns></returns>
        public static ClassDeclarationSyntax CleanClassTemplate(ClassDeclarationSyntax syntax)
        {
            // determine whether there is [StaticTemplate] attribute
            var stAttrList = syntax.AttributeLists.Select(
                (lst, li) => lst.DescendantNodes()
                                .OfType<AttributeSyntax>()
                                .Where(n => n.Name.ToString() == "StaticTemplate")).ToList();
            if (stAttrList.Any(lst => !lst.Any()))
                throw new ArgumentException("argument is not a template", nameof(syntax));

            // remove those attributes
            var newAttrLists = new SyntaxList<AttributeListSyntax>();
            for (int i = 0; i < syntax.AttributeLists.Count; ++i)
            {
                var newAttrListSyntax = syntax.AttributeLists[i].RemoveNodes(stAttrList[i],
                                            SyntaxRemoveOptions.KeepExteriorTrivia);
                newAttrLists.Add(newAttrListSyntax);
            }

            return syntax.WithAttributeLists(newAttrLists);
        }
    }
}
