using Broiler.JavaScript.ExpressionCompiler.ClosureSeparator;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Broiler.JavaScript.ExpressionCompiler.Runtime;

/// <summary>
/// The compiled inner-lambda sites of one compilation, addressed by the opaque id the
/// creation site emitted for each.
/// </summary>
/// <remarks>
/// <para>
/// <b>A site's id is its index here, not the address of a <c>GCHandle</c>.</b> It used to be
/// the latter, and the handle was never freed — so every site a compilation registered was
/// rooted for the life of the process. For a site whose IL was generated eagerly that is a
/// <c>DynamicMethod</c>; for a deferred one it is the whole un-emitted expression tree, and a
/// function that is compiled but never called never reaches <c>DeferredMethod.Force</c> to
/// release it. Measured on a script of 250 such functions, that was ~86 KB retained per
/// function — 8.6 MB per compile that nothing could reclaim, whatever the caller did with the
/// code.
/// </para>
/// <para>
/// An index needs no handle because the lookup already has the repository:
/// <see cref="Create"/> is an instance method, and the creation site the emitter builds
/// (<c>RuntimeMethodBuilder.Relay</c>) calls it on the <see cref="Closures.Repository"/> the
/// closure chain carries. So every id is resolved against the repository that issued it, and
/// the sites live and die with the compiled code that can still reach them — which is the
/// lifetime they always should have had.
/// </para>
/// <para>
/// The store is copy-on-write: a site can be registered while another thread creates a
/// closure, because a deferred site registers its own nested sites when it is first invoked.
/// Growing copies rather than mutates, so a reader holding an earlier array still resolves
/// every id issued before it — and a reader is a plain array index, as it was through the
/// handle.
/// </para>
/// </remarks>
public class MethodRepository : IMethodRepository
{

    public static ConstructorInfo constructor = typeof(MethodRepository).GetConstructor();

    public string IL;
    public string Exp;

    private readonly object gate = new();
    private RuntimeMethod[] sites = new RuntimeMethod[8];
    private int count;

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
        => Register(new RuntimeMethod
        {
            Method = d,
            IL = il,
            Exp = exp,
            Type = type
        });

    /// <summary>
    /// Registers a site whose IL has not been generated, returning the same kind of id
    /// <see cref="RegisterNew"/> does so the creation site emitted for it is unchanged.
    /// </summary>
    internal ulong RegisterDeferred(DeferredMethod deferred, Type type)
        => Register(new RuntimeMethod
        {
            Deferred = deferred,
            IL = string.Empty,
            Exp = string.Empty,
            Type = type
        });

    private ulong Register(RuntimeMethod site)
    {
        lock (gate)
        {
            var current = sites;
            if (count == current.Length)
            {
                var grown = new RuntimeMethod[current.Length * 2];
                Array.Copy(current, grown, count);
                current = grown;
            }

            current[count] = site;

            // A reader cannot reach a slot before the id that names it, and the only way
            // an id reaches a reader is the creation site the emitter builds from this
            // return value — so the store above is ordered ahead of every read of it by
            // the same chain that delivers the id, which is what the GCHandle this
            // replaces relied on too. The release write is what a *grown* array
            // additionally needs: it publishes the new array with its contents, and the
            // old one stays valid for every id issued against it, so a reader holding
            // either resolves correctly.
            Volatile.Write(ref sites, current);
            return (ulong)count++;
        }
    }

    public object Create(Box[] boxes, ulong id)
    {
        var rm = Volatile.Read(ref sites)[(int)id];
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
