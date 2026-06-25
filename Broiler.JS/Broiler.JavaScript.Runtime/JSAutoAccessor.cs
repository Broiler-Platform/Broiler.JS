using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Factory for the getter/setter functions of a public class auto-accessor
/// (<c>accessor x = v</c>, decorators proposal). An auto-accessor desugars to a
/// private backing field plus a getter/setter pair installed on the home object
/// (prototype for an instance accessor, constructor for a static one). The
/// generated getter returns the backing private field and the setter writes it,
/// both through the ordinary private-field path — so a receiver that does not
/// carry the field (e.g. a derived class for a <c>static accessor</c>) gets the
/// spec-mandated brand-check TypeError for free.
/// </summary>
public static class JSAutoAccessor
{
    public static JSValue CreateGetter(KeyString backingKey, string accessorName)
    {
        var key = backingKey;
        return JSValue.CreateFunction(
            (in Arguments a) =>
            {
                if (a.This is JSObject o)
                    return o.GetValue(key, o, true);

                JSObject.ThrowMissingPrivateMember(in key, reading: true);
                return JSUndefined.Value;
            },
            accessorName == null ? "get" : "get " + accessorName,
            length: 0,
            createPrototype: false);
    }

    public static JSValue CreateSetter(KeyString backingKey, string accessorName)
    {
        var key = backingKey;
        return JSValue.CreateFunction(
            (in Arguments a) =>
            {
                var value = a.Length > 0 ? a.GetAt(0) : JSUndefined.Value;
                if (a.This is JSObject o)
                    o.SetValue(key, value, o, true);
                else
                    JSObject.ThrowMissingPrivateMember(in key, reading: false);

                return JSUndefined.Value;
            },
            accessorName == null ? "set" : "set " + accessorName,
            length: 1,
            createPrototype: false);
    }
}
