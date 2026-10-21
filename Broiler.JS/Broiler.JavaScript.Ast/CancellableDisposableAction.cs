// Moved from Broiler.JavaScript.Runtime with its namespace, which keeps a type forwarder to it, and
// compiled as it was there, outside a nullable context, so its public signatures read as they did.
#nullable disable

using System;

namespace Broiler.JavaScript.Runtime;

/// <summary>An action run when the object is disposed, unless it was cancelled or committed first.</summary>
/// <remarks>
/// Held by <c>Broiler.JavaScript.Ast</c> since milestone SJL-12.5, because the parser's obsolete
/// <c>FastTokenStream.UndoMark</c> returns it and the parser no longer references the runtime (Broiler.VM's
/// JSD-0080, decision D20). <c>Broiler.JavaScript.Runtime</c> forwards the type here, so an assembly built
/// against it still binds.
/// </remarks>
public class CancellableDisposableAction(Action action) : IDisposable
{
    public void Cancel() => action = null;

    public T Commit<T>(T value)
    {
        action = null;
        return value;
    }

    public bool Commit()
    {
        action = null;
        return true;
    }

    public void Dispose() => action?.Invoke();
}
