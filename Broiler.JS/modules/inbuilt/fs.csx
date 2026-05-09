#r "nuget: BroilerJS.Core,1.2.1"
using System;
using System.Linq;
using System.Threading.Tasks;
using BroilerJS.Core;
using BroilerJS.Core.Clr;

public static class FSDir {

}

public static class FSModule {

    // public static JSValue Dir = ClrType.From(typeof(FSDir));

    public static JSValue Stat(in Arguments a) {
        var (path, options) = a.Get2();
        return JSUndefined.Value;
    }

    public static JSValue Access(in Arguments a) {
        throw JSContext.CurrentContext.NewTypeError($"Not implemented");
    }

}

static Task Module(JSModule module) {

    module.Exports = ClrType.From(typeof(FSModule));
    return Task.CompletedTask;
}

return (JSModuleDelegate)Module;