using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public static class JSClassStaticPropertyValidator
{
    public static KeyString Validate(in KeyString key)
    {
        if (key.Key == KeyStrings.prototype.Key)
            throw JSObject.NewTypeError("Class static elements cannot be named prototype");

        return key;
    }

    public static JSValue Validate(JSValue key)
    {
        if (key.EqualsLiteral("prototype"))
            throw JSObject.NewTypeError("Class static elements cannot be named prototype");

        return key;
    }
}
