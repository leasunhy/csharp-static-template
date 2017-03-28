using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace StaticTemplate
{
    /// <summary>
    /// Represents a group of templates.
    /// <para>A template group has a primary template (i.e. not specialized), and arbitrary number of
    /// specialized templates.</para>
    /// <para>Note that no templates with the same name but with different number of
    /// type parameters can be declared.</para>
    /// </summary>
    public class ClassTemplateGroup : IEnumerable<ClassTemplate>
    {
        /// <summary>
        /// The <see cref="ClassTemplate"/> members.
        /// </summary>
        public IEnumerable<ClassTemplate> Templates { get; }

        /// <summary>
        /// The primary template in the template group.
        /// </summary>
        public ClassTemplate PrimaryTemplate { get; }

        /// <summary>
        /// The count of type parameters.
        /// </summary>
        public int TypeParamCount => PrimaryTemplate.TypeParamCount;

        /// <summary>
        /// The name of the template.
        /// </summary>
        public string TemplateName => PrimaryTemplate.TemplateName;

        /// <summary>
        /// Constructs a <see cref="ClassTemplateGroup"/> from a sequence of templates.
        /// The constructor determines which one of the templates is the primary one,
        /// and checks if the templates meet the conditions.
        /// </summary>
        /// <param name="templates">The sequence of templates that should belong to the same group.</param>
        public ClassTemplateGroup(IEnumerable<ClassTemplate> templates)
        {
            // TODO(leasunhy): add checking for conflicts among templates
            //                 (e.g. no same specialization on the same type param,
            //                 all templates should have equal number of type params, etc.)
            // TODO(leasunhy): add checking for type parameter identifiers (force same names)

            if (templates == null)
                throw new ArgumentException("Argument cannot be null.", nameof(templates));

            Templates = templates.ToList();

            var templateName = Templates.First().TemplateName;
            if (Templates.Any(t => t.TemplateName != templateName))
                throw new ArgumentException("Templates must have same name.", nameof(templates));

            if (!Templates.Any())
                throw new ArgumentException("Argument cannot be empty.", nameof(templates));

            PrimaryTemplate = Templates.Single(t => !t.IsSpecialized);
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return Templates.GetEnumerator(); }

        /// <summary>
        /// Get the name of template instantiation for a set of arguments.
        /// </summary>
        /// <param name="typeArgs">The type arguments the template is istantiated for.</param>
        /// <returns>The name for the instantiation.</returns>
        public string GetInstantiationNameFor(IEnumerable<INamedTypeSymbol> typeArgs) =>
            $"{TemplateName}#{string.Join(":", typeArgs.Select(a => a.ToDisplayString()))}#";

        /// <summary>
        /// Looks for the best matched template for given type arguments and instantiate the template with those arguments.
        /// </summary>
        /// <param name="typeArgs">The type arguments.</param>
        public void Instantiate(IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var namedTypeSymbols = typeArgs as INamedTypeSymbol[] ?? typeArgs.ToArray();
            FindTemplateForArguments(namedTypeSymbols).Instantiate(GetInstantiationNameFor(namedTypeSymbols), namedTypeSymbols);
        }

        /// <summary>
        /// Given type arguments, looks for the best matched template.
        /// </summary>
        /// <param name="typeArgs">The type arguments.</param>
        /// <returns>The best matched templated.</returns>
        public ClassTemplate FindTemplateForArguments(IEnumerable<INamedTypeSymbol> typeArgs)
        {
            var namedTypeSymbols = typeArgs as INamedTypeSymbol[] ?? typeArgs.ToArray();
            if (namedTypeSymbols.Length != TypeParamCount)
                throw new InvalidOperationException("Incorrect number of type arguements for template" + TemplateName);

            // matching (in terms of specialized type arguments) templates
            var matched = Templates.Select(temp => Tuple.Create(temp, MatchTypeArgs(temp, namedTypeSymbols)))
                                                        .Where(t => t.Item2.Item1);
            var highestRanked = matched.MaxBy(t => t.Item2.Item2).Item2
                                       .Select(t => t.Item1).ToList();
            if (highestRanked.Count == 1)
                return highestRanked.Single();

            // if there are more than one candidates, try determine which one is the most specialized (accepts fewest args)
            var hasFewestParams = highestRanked.MinBy(t => t.RemainingParamCount).Item2.ToList();
            if (hasFewestParams.Count == 1)
                return hasFewestParams.Single();

            throw new Exception("Ambiguous partial specializations for " + GetInstantiationNameFor(namedTypeSymbols));
        }

        /// <summary>
        /// Given a template and a set of type arguments, checks whether they are matched and the number of matches between
        /// specialized type arguments and <paramref name="typeArgs"/>.
        /// </summary>
        /// <param name="template">The template to check with the arguments.</param>
        /// <param name="typeArgs">The type arguments to check with the template.</param>
        /// <returns></returns>
        private static Tuple<bool, int> MatchTypeArgs(ClassTemplate template, IEnumerable<INamedTypeSymbol> typeArgs)
        {
            // TODO(leasunhy): use semantic model?
            var success = true;
            var matched = 0;
            template.SpecialiedTypeArgList.ZipWith(typeArgs, (a1, a2) =>
            {
                success = success && (a1 == null || a1.ToDisplayString() == a2.ToDisplayString());
                if (!success) return;
                matched += a1 != null && a1.ToDisplayString() == a2.ToDisplayString() ? 1 : 0;
            });
            return Tuple.Create(success, matched);
        }

        IEnumerator IEnumerable.GetEnumerator() { return Templates.GetEnumerator(); }
    }
}
