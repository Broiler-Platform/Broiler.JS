using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Engine.Extensions;

public static partial class JSValueExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name)
    {
        var a = new Arguments(@this);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name, JSValue arg0)
    {
        var a = new Arguments(@this, arg0);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name, JSValue arg0, JSValue arg1)
    {
        var a = new Arguments(@this, arg0, arg1);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name, JSValue arg0, JSValue arg1, JSValue arg2)
    {
        var a = new Arguments(@this, arg0, arg1, arg2);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name, JSValue arg0, JSValue arg1, JSValue arg2, JSValue arg3)
    {
        var a = new Arguments(@this, arg0, arg1, arg2, arg3);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethod(this JSValue @this, in KeyString name, JSValue[] args)
    {
        var a = new Arguments(@this, args);
        return @this.InvokeMethod(in name, in a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSValue InvokeMethodSpread(this JSValue @this, in KeyString name, JSValue[] args)
    {
        var a = new Arguments(@this, args, 0);
        return @this.InvokeMethod(in name, in a);
    }
}
