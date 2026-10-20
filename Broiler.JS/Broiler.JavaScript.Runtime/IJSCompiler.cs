using System;
using System.Collections.Generic;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Defines the contract for compiling JavaScript source code into an executable
/// expression tree. Implementations convert source code through parsing and AST
/// compilation to produce a <see cref="BExpression{T}"/> of
/// <see cref="JSFunctionDelegate"/>.
/// </summary>
public interface IJSCompiler
{
    /// <summary>
    /// Compiles JavaScript source code into an expression tree that can be
    /// further compiled to a callable delegate.
    /// </summary>
    /// <param name="code">The JavaScript source code to compile.</param>
    /// <param name="location">Optional source location identifier for diagnostics.</param>
    /// <param name="argsList">Optional list of argument names for function-style compilation.</param>
    /// <param name="codeCache">Optional code cache for compiled script reuse.</param>
    /// <returns>A compiled expression tree representing the JavaScript program.</returns>
    BExpression<JSFunctionDelegate> Compile(in StringSpan code, string location = null, IList<string> argsList = null, ICodeCache codeCache = null);

    /// <summary>
    /// Compiles JavaScript source code under <paramref name="options"/>: their goal (a script, or a
    /// module when <see cref="JSCompilationOptions.IsModule"/> is set) and their permission to await
    /// at the top level (<see cref="JSCompilationOptions.AllowTopLevelAwait"/>). <c>CoreScript.Compile</c>
    /// calls this member, with the options it keys the compilation under in the code cache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compiler that does not implement this member compiles through
    /// <see cref="Compile(in StringSpan, string, IList{string}, ICodeCache)"/>, which compiles a script
    /// without top-level <c>await</c>: this default forwards to it when the options ask for exactly that,
    /// and otherwise throws <see cref="NotSupportedException"/> naming the compiler's type, since that
    /// member cannot say what the options ask.
    /// </para>
    /// <para>
    /// Added at milestone SJL-12.5 (Broiler.VM's JSD-0080, decision D19). It is a behavioural change for
    /// a host's own compiler: before it, the goal and the permission reached the parser through ambient
    /// scopes that <c>CoreScript</c> entered around the compile, so a host compiler that parsed with this
    /// engine's parser compiled a module or a top-level <c>await</c> without being told; now such a
    /// compile reaches this default and is refused unless the host implements this member.
    /// </para>
    /// </remarks>
    /// <param name="code">The JavaScript source code to compile.</param>
    /// <param name="options">The compilation options; only the goal and the top-level-await permission are read here.</param>
    /// <param name="location">Optional source location identifier for diagnostics.</param>
    /// <param name="argsList">Optional list of argument names for function-style compilation.</param>
    /// <param name="codeCache">Optional code cache for compiled script reuse.</param>
    /// <returns>A compiled expression tree representing the JavaScript program.</returns>
    /// <exception cref="NotSupportedException">The options ask for a module or for top-level <c>await</c>, and the compiler does not implement this member.</exception>
    BExpression<JSFunctionDelegate> Compile(in StringSpan code, in JSCompilationOptions options, string location = null, IList<string> argsList = null, ICodeCache codeCache = null)
    {
        if (!options.IsModule && !options.AllowTopLevelAwait)
            return Compile(code, location, argsList, codeCache);

        throw new NotSupportedException(
            "The compiler " + GetType().FullName + " implements only IJSCompiler.Compile(in StringSpan, string, IList<string>, ICodeCache), "
            + "which compiles a script without top-level await, and cannot compile "
            + (options.IsModule ? "a module" : "a script with top-level await")
            + ". Implement IJSCompiler.Compile(in StringSpan, in JSCompilationOptions, string, IList<string>, ICodeCache) to compile it.");
    }
}
