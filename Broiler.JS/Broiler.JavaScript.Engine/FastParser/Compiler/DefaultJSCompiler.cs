using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Engine.FastParser.Compiler;

/// <summary>
/// Default implementation of <see cref="IJSCompiler"/> that delegates to
/// a registered compilation function.  The static constructor proactively
/// loads the <c>Broiler.JavaScript.Compiler</c> assembly (if available) to
/// trigger its <c>[ModuleInitializer]</c>, which calls
/// <see cref="Register(Func{StringSpan, JSCompilationOptions, string, IList{string}, ICodeCache, BExpression{JSFunctionDelegate}})"/>
/// to install the real <c>FastCompiler</c>-based pipeline.
/// </summary>
public class DefaultJSCompiler : IJSCompiler
{
    /// <summary>
    /// The registered compilation, the one <see cref="Register(Func{StringSpan, string, IList{string}, ICodeCache, BExpression{JSFunctionDelegate}})"/>
    /// or <see cref="Register(Func{StringSpan, JSCompilationOptions, string, IList{string}, ICodeCache, BExpression{JSFunctionDelegate}})"/>
    /// made last.  Set by the Compiler assembly's module initializer.  One reference, so a compile
    /// never sees half of one registration and half of another.
    /// </summary>
    private static Registration _registration;

    /// <summary>A registered compilation: a function that takes the options, or one that does not.</summary>
    private sealed record Registration(
        Func<StringSpan, string, IList<string>, ICodeCache, BExpression<JSFunctionDelegate>> Compile,
        Func<StringSpan, JSCompilationOptions, string, IList<string>, ICodeCache, BExpression<JSFunctionDelegate>> CompileWithOptions);

    /// <summary>
    /// Static constructor that ensures the Compiler assembly is loaded and
    /// its <c>[ModuleInitializer]</c> has run.  Without this, the assembly
    /// may never be loaded by the runtime (assemblies are loaded lazily),
    /// leaving no compilation registered even when the Compiler
    /// assembly is deployed alongside the application.
    /// </summary>
    static DefaultJSCompiler()
    {
        EnsureCompilerAssemblyLoaded();
    }

    /// <summary>
    /// Attempts to load the <c>Broiler.JavaScript.Compiler</c> assembly and
    /// run its module constructor so that the <c>[ModuleInitializer]</c>
    /// registers the compilation pipeline via <see cref="Register(Func{StringSpan, JSCompilationOptions, string, IList{string}, ICodeCache, BExpression{JSFunctionDelegate}})"/>.
    /// If the assembly is not available the failure is silently ignored;
    /// <see cref="Compile(in StringSpan, in JSCompilationOptions, string, IList{string}, ICodeCache)"/>
    /// will throw an informative exception instead.
    /// </summary>
    private static void EnsureCompilerAssemblyLoaded()
    {
        if (_registration != null)
            return;

        try
        {
            var assembly = Assembly.Load("Broiler.JavaScript.Compiler");
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
        catch (Exception ex) when (
            ex is System.IO.FileNotFoundException
            or System.IO.FileLoadException
            or BadImageFormatException)
        {
            // Compiler assembly is not available.  No compilation is registered,
            // and Compile() will throw an informative InvalidOperationException.
        }
    }

    /// <summary>
    /// Registers a compilation function that takes no options, and so compiles a script without
    /// top-level <c>await</c>.  With it registered, a compile whose options ask for a module or for
    /// top-level <c>await</c> is refused with <see cref="NotSupportedException"/>.  It replaces
    /// whichever function was registered before.
    /// </summary>
    public static void Register(
        Func<StringSpan, string, IList<string>, ICodeCache, BExpression<JSFunctionDelegate>> compileFunc)
    {
        ArgumentNullException.ThrowIfNull(compileFunc);
        _registration = new Registration(compileFunc, null);
    }

    /// <summary>
    /// Registers the compilation function, which compiles under the options it is given.  Called by
    /// the Compiler assembly's module initializer to wire in the real <c>FastCompiler</c> pipeline.
    /// It replaces whichever function was registered before.
    /// </summary>
    public static void Register(
        Func<StringSpan, JSCompilationOptions, string, IList<string>, ICodeCache, BExpression<JSFunctionDelegate>> compileFunc)
    {
        ArgumentNullException.ThrowIfNull(compileFunc);
        _registration = new Registration(null, compileFunc);
    }

    private static Registration Registered()
        => _registration ?? throw new InvalidOperationException(
            "The JavaScript compiler is not available. " +
            "Reference the Broiler.JavaScript.Compiler assembly to enable script compilation.");

    /// <inheritdoc />
    /// <remarks>Compiles a script without top-level <c>await</c>.</remarks>
    public BExpression<JSFunctionDelegate> Compile(
        in StringSpan code,
        string location = null,
        IList<string> argsList = null,
        ICodeCache codeCache = null)
    {
        var registration = Registered();
        return registration.Compile is { } compile
            ? compile(code, location, argsList, codeCache)
            : registration.CompileWithOptions(code, default, location, argsList, codeCache);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// The function registered takes no options, and the options ask for a module or for top-level <c>await</c>.
    /// </exception>
    public BExpression<JSFunctionDelegate> Compile(
        in StringSpan code,
        in JSCompilationOptions options,
        string location = null,
        IList<string> argsList = null,
        ICodeCache codeCache = null)
    {
        var registration = Registered();
        if (registration.CompileWithOptions is { } compileWithOptions)
            return compileWithOptions(code, options, location, argsList, codeCache);

        if (!options.IsModule && !options.AllowTopLevelAwait)
            return registration.Compile(code, location, argsList, codeCache);

        throw new NotSupportedException(
            "The compilation function registered with DefaultJSCompiler takes no options, and so compiles a script without top-level await; it cannot compile "
            + (options.IsModule ? "a module" : "a script with top-level await")
            + ". Register a function that takes JSCompilationOptions to compile it.");
    }
}
