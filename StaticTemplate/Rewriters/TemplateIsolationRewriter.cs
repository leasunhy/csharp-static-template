using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StaticTemplate.Rewriters
{
    /// <summary>
    /// Isolates a template from a <see cref="CompilationUnitSyntax"/>.
    /// The isolation can be used for instantiating the template.
    /// </summary>
    /// <example>
    /// Given target class template TemplateTarget and code:
    /// <code>
    /// using System;
    /// namespace Example
    /// {
    ///     using System.Linq;
    ///
    ///     [StaticTemplate]
    ///     class TemplateTarget&lt;T&gt;
    ///
    ///     [StaticTemplate]
    ///     class TemplateNonTarget&lt;T&gt;
    ///
    ///     class NonTemplate {}
    /// }
    /// </code>
    /// The isolation results in code:
    /// <code>
    /// using System;
    /// namespace Example
    /// {
    ///     using System.Linq;
    ///
    ///     [StaticTemplate]
    ///     class TemplateTarget&lt;T&gt;
    /// }
    /// </code>
    /// And the isolation is thus useful for instantiations.
    /// </example>
    class TemplateIsolationRewriter : CSharpSyntaxRewriter
    {
        private readonly ClassDeclarationSyntax _target;

        private TemplateIsolationRewriter(ClassDeclarationSyntax target)
        {
            _target = target;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return base.VisitNamespaceDeclaration(node);
            // TODO(leasunhy): extra processing here: what other types of nodes shall be removed?
        }

        /// <summary>
        /// Visits a <see cref="ClassDeclarationSyntax"/>. If it is not the target, remove it.
        /// </summary>
        /// <param name="node">The node to visit.</param>
        /// <returns>Null if it is not the target template to isolate; a cleaned version of the template if it is.</returns>
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node != _target)
                return null;
            return ClassTemplate.CleanClassTemplate(node);
        }

        /// <summary>
        /// Given a template target that belongs to a <see cref="CompilationUnitSyntax"/>, isolate it.
        /// </summary>
        /// <param name="target">The template to isolate.</param>
        /// <returns>A new <see cref="CompilationUnitSyntax"/> with the template isolated.</returns>
        public static CompilationUnitSyntax IsolateFor(ClassDeclarationSyntax target)
        {
            return (CompilationUnitSyntax)new TemplateIsolationRewriter(target).Visit(target.SyntaxTree.GetRoot());
        }
    }
}
