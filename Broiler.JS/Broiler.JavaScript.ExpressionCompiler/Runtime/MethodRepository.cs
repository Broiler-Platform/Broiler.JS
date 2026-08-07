using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

public class MethodRepository : IMethodRepository
{

    public static ConstructorInfo constructor = typeof(MethodRepository).GetConstructor();

    public string IL;
    public string Exp;

    public class RuntimeMethod
    {
        // MethodInfo, not DynamicMethod, to match IMethodRepository.RegisterNew — which is typed
        // that way so the model assembly holding IMethodRepository stays free of Reflection.Emit.
        // Every value stored here is still a DynamicMethod.
        public MethodInfo Method;
        public string IL;
        public string Exp;
        public Type Type;

        /// <summary>
        /// Set instead of <see cref="Method"/> when this site's IL generation was deferred to
        /// first invocation (roadmap item 1-1). Exactly one of the two is ever non-null.
        /// </summary>
        internal DeferredMethod Deferred;
    }

    public ulong RegisterNew(MethodInfo d, string il, string exp, Type type)
    {
        var x = GCHandle.Alloc(new RuntimeMethod {
            Method = d,
            IL = il,
            Exp = exp,
            Type = type
        });
        return (ulong)(IntPtr)x;
    }

    /// <summary>
    /// Registers a site whose IL has not been generated, returning the same kind of handle
    /// <see cref="RegisterNew"/> does so the creation site emitted for it is unchanged.
    /// </summary>
    internal ulong RegisterDeferred(DeferredMethod deferred, Type type)
    {
        var x = GCHandle.Alloc(new RuntimeMethod
        {
            Deferred = deferred,
            IL = string.Empty,
            Exp = string.Empty,
            Type = type
        });
        return (ulong)(IntPtr)x;
    }

    public object Create(Box[] boxes, ulong id)
    {
        var rm = GCHandle.FromIntPtr((IntPtr)id).Target as RuntimeMethod;
        // A deferred site hands back a thunk over this instance's boxes; the boxes are captured
        // here, at closure creation, exactly as they are for a generated one. What is postponed
        // is the machine code, which is shared by every instance of the site and belongs to
        // none of them.
        if (rm.Deferred != null)
            return rm.Deferred.CreateThunk(this, boxes);

        var c = new Closures(this, boxes, rm.IL, rm.Exp);
        return rm.Method.CreateDelegate(rm.Type, c);
    }
}
