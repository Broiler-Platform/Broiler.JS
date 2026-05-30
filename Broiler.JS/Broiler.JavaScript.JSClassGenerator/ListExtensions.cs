using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Broiler.JavaScript.JSClassGenerator;

internal static class ListExtensions
{
    public static string ClrProxyMarshal(this string target, ITypeSymbol type, string value) {
        if (type.Name == "JSValue")
        {
            return target;
        }
        return $"JSEngine.ClrInterop.Marshal({target})";
    }

    public static string ToJSValueFromClr(this string name, ITypeSymbol type, string parameter)
    {
        var typeName = type.ToClrName();
        switch(typeName)
        {
            case "JSValue":
            case "Broiler.JavaScript.Core.JSValue":
                return name;
            case "JSNumber":
            case "Broiler.JavaScript.Core.JSNumber":
            case "Broiler.JavaScript.Core.JSNumber?":
            case "Broiler.JavaScript.BuiltIns.Number.JSNumber":
            case "Broiler.JavaScript.BuiltIns.Number.JSNumber?":
                return $"(JSNumber)JSValueToClrConverter.ToJSNumber({name}, \"{parameter}\")";
            case "JSObject":
            case "Broiler.JavaScript.Core.JSObject":
                return $"{name} is JSObject obj{parameter} ? obj{parameter} : JSException.ThrowTypeError<JSObject>(\"{parameter} is not an object\")";
            case "JSFunction":
            case "Broiler.JavaScript.Core.JSFunction":
            case "JSFunction?":
            case "Broiler.JavaScript.Core.JSFunction?":
                return $"{name} is JSFunction obj{parameter} ? obj{parameter} : JSException.ThrowTypeError<JSFunction>(\"{parameter} is not a function\")";
            case "Int32":
            case "int":
                return $"JSValueToClrConverter.ToInt({name}, \"{parameter}\")";
            case "int?":
                return $"JSValueToClrConverter.ToNullableInt({name}, \"{parameter}\")";
            case "long":
            case "Int64":
                return $"JSValueToClrConverter.ToLong({name}, \"{parameter}\")";
            case "long?":
                return $"JSValueToClrConverter.ToNullableLong({name}, \"{parameter}\")";
            case "short":
                return $"JSValueToClrConverter.ToShort({name}, \"{parameter}\")";
            case "short?":
                return $"JSValueToClrConverter.ToNullableShort({name}, \"{parameter}\")";
            case "byte":
            case "Byte":
                return $"JSValueToClrConverter.ToByte({name}, \"{parameter}\")";
            case "byte?":
                return $"JSValueToClrConverter.ToNullableByte({name}, \"{parameter}\")";
            case "sbyte":
            case "SByte":
                return $"JSValueToClrConverter.ToSByte({name}, \"{parameter}\")";
            case "sbyte?":
                return $"JSValueToClrConverter.ToNullableSByte({name}, \"{parameter}\")";
            case "double":
            case "Double":
                return $"JSValueToClrConverter.ToDouble({name}, \"{parameter}\")";
            case "double?":
                return $"JSValueToClrConverter.ToNullableDouble({name}, \"{parameter}\")";
            case "Single":
            case "float":
                return $"JSValueToClrConverter.ToFloat({name}, \"{parameter}\")";
            case "float?":
                return $"JSValueToClrConverter.ToNullableFloat({name}, \"{parameter}\")";
            case "Boolean":
            case "bool":
                return $"JSValueToClrConverter.ToBoolean({name}, \"{parameter}\")";
            case "bool?":
                return $"JSValueToClrConverter.ToNullableBoolean({name}, \"{parameter}\")";
            case "string":
            case "String":
                return $"JSValueToClrConverter.ToString({name}, \"{parameter}\")";
        }
        return $"JSValueToClrConverter.GetAsOrThrow<{typeName}>({name}, \"{parameter}\")";
    }

    public static bool IsJSFunction(this IMethodSymbol method) => method.Parameters.Length == 1
            && method.Parameters[0] is IParameterSymbol p
            && p.RefKind == RefKind.In
            && p.Type.Name == "Arguments";

    public static string ToCamelCase(this string @this)
    {
        var length = @this.Length;
        if (length == 0)
        {
            return string.Empty;
        }

        var d = new char[length];
        var i = 0;
        for (; i < length; i++)
        {
            var ch = @this[i];
            d[i] = char.ToLower(ch);
            if (!char.IsUpper(ch))
            {
                i++;
                break;
            }
        }

        for (; i < length; i++)
        {
            d[i] = @this[i];
        }

        return new string(d);
    }

    public static string GetOrCreateName(this List<string> list, string name, string className = "Names")
    {
        var identifier = name.ToCSharpIdentifier();

        if (!list.Contains(identifier))
        {
            list.Add(identifier);
        }
        return className + "." + identifier;
    }

    public static string ToCSharpIdentifier(this string name)
    {
        return IsCSharpKeyword(name) ? "@" + name : name;
    }

    public static string ToUnescapedCSharpIdentifier(this string name)
    {
        return name.StartsWith("@") ? name.Substring(1) : name;
    }

    private static bool IsCSharpKeyword(string name)
    {
        switch (name)
        {
            case "abstract":
            case "as":
            case "base":
            case "bool":
            case "break":
            case "byte":
            case "case":
            case "catch":
            case "char":
            case "checked":
            case "class":
            case "const":
            case "continue":
            case "decimal":
            case "default":
            case "delegate":
            case "do":
            case "double":
            case "else":
            case "enum":
            case "event":
            case "explicit":
            case "extern":
            case "false":
            case "finally":
            case "fixed":
            case "float":
            case "for":
            case "foreach":
            case "goto":
            case "if":
            case "implicit":
            case "in":
            case "int":
            case "interface":
            case "internal":
            case "is":
            case "lock":
            case "long":
            case "namespace":
            case "new":
            case "null":
            case "object":
            case "operator":
            case "out":
            case "override":
            case "params":
            case "private":
            case "protected":
            case "public":
            case "readonly":
            case "ref":
            case "return":
            case "sbyte":
            case "sealed":
            case "short":
            case "sizeof":
            case "stackalloc":
            case "static":
            case "string":
            case "struct":
            case "switch":
            case "this":
            case "throw":
            case "true":
            case "try":
            case "typeof":
            case "uint":
            case "ulong":
            case "unchecked":
            case "unsafe":
            case "ushort":
            case "using":
            case "virtual":
            case "void":
            case "volatile":
            case "while":
                return true;
            default:
                return false;
        }
    }
}
