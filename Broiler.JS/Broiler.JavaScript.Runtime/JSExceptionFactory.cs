using System.Runtime.CompilerServices;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Creates an engine-raised <see cref="JSException"/>, recording the engine call site
/// that raised it.
/// </summary>
/// <remarks>
/// The <see cref="JSErrorFactory"/> counterpart for the throw sites that need the
/// <see cref="JSException"/> itself — to read its <see cref="JSException.Error"/>, or
/// to reject a promise with it — rather than a bare <see cref="System.Exception"/>.
/// See <see cref="JSErrorFactory"/> for why the caller-info parameters are declared
/// on the delegate.
/// </remarks>
public delegate JSException JSExceptionFactory(
    string message,
    [CallerMemberName] string function = null,
    [CallerFilePath] string filePath = null,
    [CallerLineNumber] int line = 0);
