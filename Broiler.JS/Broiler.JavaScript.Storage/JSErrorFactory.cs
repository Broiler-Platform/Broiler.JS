using System;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Storage;

/// <summary>
/// Creates an engine-raised JavaScript error, recording the engine call site that
/// raised it.
/// </summary>
/// <remarks>
/// <para>
/// The error factories are delegates because the lower assemblies — this one and
/// Runtime — have to raise a real JavaScript error without referencing the Engine
/// assembly that knows how to build one.
/// </para>
/// <para>
/// The caller-info parameters are why this is a named delegate rather than a
/// <c>Func&lt;string, Exception&gt;</c>. A <c>JSException</c> records the engine
/// method that raised it as the first frame of the JavaScript stack, and the
/// compiler fills those parameters in at the <c>throw</c> site. A plain
/// <c>Func&lt;string, Exception&gt;</c> has nowhere to put them, so they were
/// instead captured where the delegate was *wired* — collapsing the origin of every
/// error in the engine to the one lambda in the module initializer. Every TypeError
/// a page could provoke, from a property read on <c>undefined</c> on down, reported
/// <c>at InitializeFactories:JSValueCoreExtensions.cs:17</c>, which named the line
/// that installed the factory rather than anything to do with the failure. Declaring
/// the parameters here lets each throw site record its own position; the initializer
/// forwards what it is handed instead of substituting itself.
/// </para>
/// </remarks>
public delegate Exception JSErrorFactory(
    string message,
    [CallerMemberName] string function = null,
    [CallerFilePath] string filePath = null,
    [CallerLineNumber] int line = 0);
