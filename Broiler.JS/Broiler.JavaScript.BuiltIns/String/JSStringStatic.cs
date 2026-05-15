using System;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using System.Text;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{

    [JSExport("fromCharCode", Length = 1)]
    internal static JSValue FromCharCode(in Arguments a)
    {
        if (a.Length == 0)
            return new JSString(string.Empty);

        var al = a.Length;
        StringBuilder sb = new(al);

        for (var ai = 0; ai < al; ai++)
        {
            var ch = a.GetAt(ai);
            sb.Append((char)ch.IntValue);
        }

        return new JSString(sb.ToString());
    }

    [JSExport("fromCodePoint", Length = 1)]
    internal static JSValue FromCodePoint(in Arguments a)
    {
        if (a.Length == 0)
            return new JSString(string.Empty);

        var len = a.Length;
        var result = new StringBuilder(len);

        for (var i = 0; i < len; i++)
        {
            var item = a.GetAt(i);
            var codePointDouble = item.DoubleValue;
            int codePoint = (int)codePointDouble;

            if (codePoint < 0 || codePoint > 0x10FFFF || codePoint != codePointDouble)
                throw JSEngine.NewRangeError($"Invalid code point {codePointDouble}");

            if (codePoint <= 65535)
                result.Append((char)codePoint);
            else
            {
                result.Append((char)((codePoint - 65536) / 1024 + 0xD800));
                result.Append((char)((codePoint - 65536) % 1024 + 0xDC00));
            }

        }

        return new JSString(result.ToString());
    }

    [JSExport("raw", Length = 1)]
    internal static JSValue Raw(in Arguments a)
    {
        var template = a.Get1();
        if (template.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        var templateObject = template as JSObject ?? JSObject.CreatePrimitiveObject(template) as JSObject
            ?? throw new InvalidOperationException("CreatePrimitiveObject returned a non-object value.");
        var raw = templateObject[KeyStrings.raw];
        if (raw.IsNullOrUndefined)
            throw JSEngine.NewTypeError("Cannot convert undefined or null to object");

        var rawObject = raw as JSObject ?? JSObject.CreatePrimitiveObject(raw) as JSObject
            ?? throw new InvalidOperationException("CreatePrimitiveObject returned a non-object value.");

        var len = rawObject.Length;
        if (len <= 0)
            return new JSString(string.Empty);

        var result = new StringBuilder(len);
        for (uint i = 0; i < len; i++)
        {
            var item = rawObject[i];
            result.Append(item.StringValue);

            var substitutionIndex = i + 1;
            if (i < len - 1 && substitutionIndex < a.Length)
            {
                item = a.GetAt((int)substitutionIndex);
                result.Append(item.StringValue);
            }
        }

        return new JSString(result.ToString());
    }
}
