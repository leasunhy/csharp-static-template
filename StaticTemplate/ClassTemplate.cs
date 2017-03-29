using System;
using System.Collections.Concurrent;
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
    /// <summary>
    /// Represents a class template.
    /// <para>
    /// During compilation, a template is instantiated multiple times, once for a different set
    /// of template arguments, effectively turning into different classes.
    /// </para>
    /// <para>
    /// This class represents one template. See <see cref="ClassTemplateGroup"/> for a template group
    /// containing multiple templates among which there exist one primary template and its specializaitions.
    /// </para>
    /// </summary>
    public class ClassTemplate
    {
        /// <summary>
        /// The original <see cref="ClassDeclarationSyntax"/> representing the template.
        /// Note that the [StaticTemplate] attributes and type parameter contraint clauses
        /// (specialization) are not removed.
        /// </summary>
        public ClassDeclarationSyntax OriginalSyntax { get; }

        /// <summary>
        /// A <see cref="CompilationUnitSyntax"/> that keeps the context of the template.
        /// It is obtained by removing unrelated syntax nodes from the syntax tree where the
        /// template is extracted.
        /// </summary>
        /// <seealso cref="TemplateIsolationRewriter"/>
        public CompilationUnitSyntax TemplateIsolation { get; }

        /// <summary>
        /// Indicates whether the template is a specialized one. (i.e. has type parameter
        /// contraint clauses.
        /// </summary>
        public bool IsSpecialized { get; }

        /// <summary>
        /// The number of specialized type arguements.
        /// </summary>
        public int SpecialiedTypeArgCount { get; }

        /// <summary>
        /// The type name of the template.
        /// </summary>
        /// <example>Serializer&lt;T&gt; -> Serializer</example>
        public string TemplateName => OriginalSyntax.Identifier.ToString();

        /// <summary>
        /// The list of specialized type arguements.
        /// </summary>
        public IEnumerable<INamedTypeSymbol> SpecialiedTypeArgList { get; }

        /// <summary>
        /// The full name of the template.
        /// </summary>
        /// <example>Serializer&lt;T&gt;, no matter whether T is specialized.</example>
        public string FullName => $"{TemplateName}<{string.Join(", ", TypeParams)}>";

        /// <summary>
        /// The type parameters of this template.
        /// </summary>
        public IEnumerable<TypeParameterSyntax> TypeParams => OriginalSyntax.TypeParameterList.Parameters;

        /// <summary>
        /// The count of type parameters of the template.
        /// </summary>
        public int TypeParamCount => OriginalSyntax.TypeParameterList.Parameters.Count;

        /// <summary>
        /// The count of unbound (not specialized) type parameters.
        /// </summary>
        public int RemainingParamCount => TypeParamCount - SpecialiedTypeArgCount;

        /// <summary>
        /// The instantiations of the template with each one being a standalone syntax tree.
        /// </summary>
        public IEnumerable<SyntaxTree> Instantiations => _instantiations.Values;
        private readonly Dictionary<string, SyntaxTree> _instantiations = new Dictionary<string, SyntaxTree>();

        /// <summary>
        /// Construct a new <see cref="ClassTemplate"/>.
        /// </summary>
        /// <param name="semanticModel">The <see cref="SemanticModel"/> that is used to lookup symbols
        /// for the specialized type arguments (if any).</param>
        /// <param name="template">The <see cref="ClassDeclarationSyntax"/> that represents the template.
        /// Must be extracted from a <see cref="CompilationUnitSyntax"/></param>
        public ClassTemplate(SemanticModel semanticModel, ClassDeclarationSyntax template)
        {
            OriginalSyntax = template;
            TemplateIsolation = TemplateIsolationRewriter.IsolateFor(template);

            // checking whether the template is a specialized one
            var constraintClauses = OriginalSyntax.ChildNodes().OfType<TypeParameterConstraintClauseSyntax>().ToList();
            SpecialiedTypeArgCount = constraintClauses.Count;
            IsSpecialized = SpecialiedTypeArgCount != 0;
            SpecialiedTypeArgList = new List<INamedTypeSymbol>();

            // if the template is a specialized one, parse the contraint clauses into specialized type arguments
            if (IsSpecialized)
            {
                var argDict = TypeParams.ToDictionary(p => p.ToString(), p => default(INamedTypeSymbol));
                foreach (var clause in constraintClauses)
                {
                    var constraint = clause.Constraints.FirstOrDefault() as TypeConstraintSyntax;
                    Debug.Assert(clause.Constraints.Count == 1 && constraint != null,
                        "Currently only explicit specialization is supported, exactly one IsType<T> constraint expected.");
                    var type = constraint.Type as GenericNameSyntax;
                    Debug.Assert(type != null, "Currently only IsType<T> constraint is supported.");
                    Debug.Assert(type.Identifier.ToString() == "IsType");
                    Debug.Assert(type.TypeArgumentList.Arguments.Count == 1);
                    var typeSyntax = type.TypeArgumentList.Arguments.Single();
                    // get the symbol representing the type of the type arguments, and record the mapping
                    var typeSymbol = (INamedTypeSymbol)semanticModel.GetTypeInfo(typeSyntax).Type;
                    argDict[clause.Name.ToString()] = typeSymbol;
                }
                SpecialiedTypeArgList = TypeParams.Select(p => argDict[p.ToString()]).ToList();
            }
        }

        /// <summary>
        /// Instantiate the template for arguments <paramref name="typeArgs"/> with name <paramref name="instantiationName"/>.
        /// The syntax tree resulting from the instantiation is then saved into <see cref="Instantiations"/>, which can be added
        /// to a compilation.
        /// </summary>
        /// <param name="instantiationName">The name of the instantiation.</param>
        /// <param name="typeArgs">The type arguments for the instantiation.</param>
        public void Instantiate(string instantiationName, IEnumerable<INamedTypeSymbol> typeArgs)
        {
            if (!_instantiations.ContainsKey(instantiationName))
            {
                lock (_instantiations)
                {
                    if (_instantiations.ContainsKey(instantiationName)) return;
                    var syntaxTree = TemplateInstantiationRewriter.InstantiateFor(
                                            TemplateIsolation, instantiationName, typeArgs);
                    _instantiations[instantiationName] = syntaxTree;
                }
            }
        }

        /// <summary>
        /// This method determines whether <param name="syntax"></param> is a template.
        /// </summary>
        /// <param name="syntax">The <see cref="ClassDeclarationSyntax"/> to be checked.</param>
        /// <returns>Whether <paramref name="syntax"/> is a template.</returns>
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
        /// This method cleans the [StaticTemplate] attributes and the type parameter contraint clauses.
        /// </summary>
        /// <param name="syntax">The <see cref="ClassDeclarationSyntax"/> to be cleaned.</param>
        /// <returns>The cleaned <paramref name="syntax"/>.</returns>
        public static ClassDeclarationSyntax CleanClassTemplate(ClassDeclarationSyntax syntax)
        {
            // remove constraint clauses
            var constraintClauses = syntax.ChildNodes().OfType<TypeParameterConstraintClauseSyntax>().ToList();
            syntax = syntax.RemoveNodes(constraintClauses, SyntaxRemoveOptions.KeepExteriorTrivia);

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
