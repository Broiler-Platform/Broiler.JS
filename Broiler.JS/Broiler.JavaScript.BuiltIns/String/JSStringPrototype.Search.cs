using Broiler.JavaScript.BuiltIns.RegExp;
using System;
using System.Globalization;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.String;

public partial class JSString
{
    private static bool IsRegExpArgument(JSValue value)
    {
        if (value is not JSObject @object)
            return false;

        var matcher = @object[(IJSSymbol)JSSymbol.match];
        if (!matcher.IsUndefined)
            return matcher.BooleanValue;

        return value is JSRegExp;
    }

    [JSPrototypeMethod]
    [JSExport("contains", Length = 1)]
    internal static JSValue Contains(in Arguments a)
    {
        var @this = a.This.AsString();
        var arg = a.Get1().StringValue;
        int position = a.GetIntAt(1, 0);

        position = Math.Min(Math.Max(0, position), @this.Length);

        if (@this.IndexOf(arg, position) >= 0)
            return JSValue.BooleanTrue;

        return JSValue.BooleanFalse;
    }

    [JSPrototypeMethod]
    [JSExport("endsWith", Length = 1)]
    internal static JSValue EndsWith(in Arguments a)
    {
        var @this = a.This.AsString();
        var f = a.Get1();

        if (IsRegExpArgument(f))
            throw JSEngine.NewTypeError("Substring argument must not be a regular expression.");

        var endPosition = a[1]?.IntegerValue ?? int.MaxValue;
        var fs = f.StringValue;

        if (endPosition == int.MaxValue)
            return @this.EndsWith(fs) ? JSValue.BooleanTrue : JSValue.BooleanFalse;

        endPosition = Math.Min(Math.Max(0, endPosition), @this.Length);

        if (fs.Length > endPosition)
            return JSValue.BooleanFalse;

        if (string.Compare(@this, endPosition - fs.Length, fs, 0, fs.Length) == 0)
            return JSValue.BooleanTrue;

        return JSValue.BooleanFalse;
    }

    [JSPrototypeMethod]
    [JSExport("startsWith", Length = 1)]
    internal static JSValue StartsWith(in Arguments a)
    {
        var @this = a.This.AsString();
        var searchStr = a[0] ?? JSUndefined.Value;
        var pos = a[1]?.IntegerValue ?? 0;

        if (IsRegExpArgument(searchStr))
            throw JSEngine.NewTypeError("Substring argument must not be a regular expression.");

        var search = searchStr.StringValue;
        if (pos == 0)
            return @this.StartsWith(search) ? JSValue.BooleanTrue : JSValue.BooleanFalse;

        pos = Math.Min(Math.Max(0, pos), @this.Length);
        if (pos + search.Length > @this.Length)
            return JSValue.BooleanFalse;

        int index = @this.IndexOf(search);
        if (index == pos)
            return JSValue.BooleanTrue;

        return JSValue.BooleanFalse;
    }

    [JSPrototypeMethod]
    [JSExport("includes", Length = 1)]
    internal static JSValue Includes(in Arguments a)
    {
        var @this = a.This.AsString();
        var searchStr = a[0] ?? JSUndefined.Value;
        var pos = a[1]?.IntegerValue ?? 0;

        if (IsRegExpArgument(searchStr))
            throw JSEngine.NewTypeError("Substring argument must not be a regular expression.");

        pos = Math.Min(Math.Max(pos, 0), @this.Length);
        return @this.IndexOf(searchStr.StringValue, pos) >= 0 ? JSValue.BooleanTrue : JSValue.BooleanFalse;
    }

    [JSPrototypeMethod]
    [JSExport("indexOf", Length = 1)]
    internal static JSValue IndexOf(in Arguments a)
    {
        var @this = a.This.AsString();
        // Spec order: ToString(this), then ToString(searchString), then
        // ToInteger(position). Read searchString BEFORE coercing position so an
        // exception from the search argument surfaces first.
        var searchString = (a[0] ?? JSUndefined.Value).StringValue;
        var pos = a[1]?.IntegerValue ?? 0;

        pos = Math.Min(Math.Max(pos, 0), @this.Length);

        var index = @this.IndexOf(searchString, pos);
        return JSValue.CreateNumber(index);
    }

    [JSPrototypeMethod]
    [JSExport("lastIndexOf", Length = 1)]
    internal static JSValue LastIndexOF(in Arguments a)
    {
        var @this = a.This.AsString();
        // Spec order: ToString(this), then ToString(searchString), then
        // ToNumber(position). Read searchString BEFORE coercing position.
        var search = (a[0] ?? JSUndefined.Value).StringValue;
        var fromIndex = a[1]?.DoubleValue ?? int.MaxValue;
        var startIndex = double.IsNaN(fromIndex) ? int.MaxValue : (int)(((long)fromIndex << 32) >> 32);

        startIndex = Math.Min(startIndex, @this.Length - 1);
        startIndex = Math.Min(startIndex + search.Length - 1, @this.Length - 1);

        if (startIndex < 0)
        {
            if (@this == string.Empty && search.Length == 0)
                return JSValue.NumberZero;

            return JSValue.NumberMinusOne;
        }

        return JSValue.CreateNumber(@this.LastIndexOf(search, startIndex, StringComparison.Ordinal));
    }

    [JSPrototypeMethod]
    [JSExport("localeCompare", Length = 1)]
    internal static JSValue LocaleCompare(in Arguments a)
    {
        var @this = a.This;
        if (@this.IsNullOrUndefined)
            throw JSEngine.NewTypeError("String.prototype.localeCompare called on null or undefined");

        var (compareString, locale, options) = a.Get3();
        var str = compareString.StringValue;

        // §String.prototype.localeCompare(that, locales, options) ≡
        // Intl.Collator(locales, options).compare(this, that), so the ordering matches
        // Intl.Collator for every locale/option combination.
        var collator = new Intl.JSIntlCollator(new Arguments(JSUndefined.Value, locale, options));
        return collator.Compare(new Arguments(JSUndefined.Value, JSValue.CreateString(@this.ToString()), JSValue.CreateString(str)));
    }

    [JSPrototypeMethod]
    [JSExport("search", Length = 1)]
    internal static JSValue Search(in Arguments a)
    {
        var @this = a.This.AsString();
        var search = a.Get1();

        if (!search.IsNullOrUndefined && search.IsObject)
        {
            var searcher = search[(IJSSymbol)JSSymbol.search];
            // GetMethod semantics: a null @@search is treated as absent.
            if (!searcher.IsNullOrUndefined)
            {
                if (!searcher.IsFunction)
                    throw JSEngine.NewTypeError("@@search is not callable");

                return searcher.InvokeFunction(new Arguments(search, a.This));
            }
        }

        //search string not defined
        if (search.IsUndefined)
            return JSValue.NumberZero;

        // is Regex?
        if (search is JSRegExp jSRegExp)
        {
            var reg = jSRegExp.value.Match(@this);

            if (!reg.Success)
                return JSValue.NumberMinusOne;
            return JSValue.CreateNumber(reg.Index);
        }

        var created = new JSRegExp(search.StringValue, "");
        var builtinSearcher = created[(IJSSymbol)JSSymbol.search];
        // §22.1.3.12 step 5: Invoke(rx, @@search, « string »), where string is ToString(O)
        // (computed above as @this) — not the raw receiver. A boxed String receiver must
        // therefore reach the built-in @@search as the primitive string it coerces to.
        return builtinSearcher.InvokeFunction(new Arguments(created, JSValue.CreateString(@this)));
    }
}
