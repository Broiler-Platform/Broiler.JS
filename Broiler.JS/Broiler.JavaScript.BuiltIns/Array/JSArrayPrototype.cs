using System;
using Broiler.ECMAScript.Numbers;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Array;

public partial class JSArray
{
    [JSExport(IsConstructor = true, Length = 1)]
    public new static JSValue Constructor(in Arguments a)
    {
        var @this = a.This;
        var arg = a.Get1();
        var result = new JSArray();

        if (a.Length == 0)
            return new JSArray();

        if (a.Length == 1 && arg.IsNumber)
        {
            // Array(len) step 5.c: intLen = ToUint32(len), the shared library's (SJL-4.5); a length
            // that is not SameValueZero to it is a RangeError.
            var len = arg.DoubleValue;
            var intLen = IntegerConversions.ToUint32(len);
            if (intLen != len)
                throw JSEngine.NewRangeError($"Invalid array length");
            return new JSArray(intLen);
        }

        for (int i = 0; i < a.Length; i++)
        {
            var ele = a.GetAt(i);
            result.Add(ele);
        }

        return result;
    }
}
