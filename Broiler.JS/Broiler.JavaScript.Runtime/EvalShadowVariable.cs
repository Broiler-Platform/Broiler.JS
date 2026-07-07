namespace Broiler.JavaScript.Runtime;

/// <summary>
/// A parameter-environment binding that may be introduced by a sloppy-mode direct
/// <c>eval</c> in a function's parameter list (FunctionDeclarationInstantiation
/// step 20: a separate environment for eval-introduced parameter vars).
///
/// The binding starts uninitialized and transparently forwards reads and writes to
/// its <see cref="outer"/> binding (the same-named binding the name would otherwise
/// resolve to). When a direct eval declares a <c>var</c> of this name, the engine's
/// <c>Register</c> path assigns this binding's <see cref="JSVariable.Value"/>, which
/// initializes it; from then on the binding "owns" the value and shadows the outer
/// one. Closures created in the parameter list (and body) capture this shared
/// binding the ordinary way, so they observe the eval-introduced value even after
/// the function returns — which an ordinary transient eval overlay cannot provide.
/// </summary>
public sealed class EvalShadowVariable : JSVariable
{
    private readonly JSVariable outer;

    // A global binding is read/written through JSVariable.GlobalValue (which keeps
    // the binding in sync with its global-object property); a function-local binding
    // is its own storage and uses GetValue/SetValue. The forwarding path must match
    // the outer binding's access mode, otherwise a forwarded write to a global would
    // update only the cached field and never the global-object property reads see.
    private readonly bool outerIsGlobal;

    public EvalShadowVariable(string name, JSVariable outer, bool outerIsGlobal)
        : base(JSUndefined.Value, name, initialized: false)
    {
        this.outer = outer;
        this.outerIsGlobal = outerIsGlobal;
    }

    public static JSVariable New(string name, JSVariable outer, bool outerIsGlobal)
        => new EvalShadowVariable(name, outer, outerIsGlobal);

    private JSValue OuterValue
    {
        get
        {
            if (outer == null)
                return JSUndefined.Value;
            return outerIsGlobal ? outer.GlobalValue : outer.GetValue();
        }
        set
        {
            if (outer == null)
                return;
            if (outerIsGlobal)
                outer.GlobalValue = value;
            else
                outer.SetValue(value);
        }
    }

    public override JSValue GetValue()
        => IsInitialized ? Value : OuterValue;

    public override JSValue SetValue(JSValue value)
    {
        // Once a direct eval has introduced this binding it owns the value; before
        // that, an assignment targets the outer binding it shadows (so e.g. a
        // parameter initializer that assigns an unrelated outer global still writes
        // through to the global).
        if (IsInitialized)
            Value = value;
        else
            OuterValue = value;

        return value;
    }

    // A compound assignment captures whether this shadow already owns its value
    // before the right-hand side runs. If a direct eval in the RHS initializes the
    // shadow in between, the write still targets the binding the read observed (the
    // outer binding), matching the single Reference of a compound assignment.
    public override bool CaptureReference() => IsInitialized;

    public override JSValue GetCaptured(bool ownedAtCapture)
        => ownedAtCapture ? Value : OuterValue;

    public override JSValue SetCaptured(bool ownedAtCapture, JSValue value)
    {
        if (ownedAtCapture)
            Value = value;
        else
            OuterValue = value;

        return value;
    }
}
