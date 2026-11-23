// Moved from Broiler.JavaScript.Ast with its namespace at window W2 of milestone SJL-14 (decision D50: a non-tree type,
// whose new home is Broiler.JavaScript.Expressions); Ast keeps a type forwarder to it, and it is not marked obsolete. It is
// compiled in a nullable context, as Ast compiled it, so that its public signatures read as they did.
#nullable enable

namespace Broiler.JavaScript.Ast.Misc;

/// <summary>
/// Marker interface for types that can be stored as a property value
/// inside <c>JSProperty</c>.  Implemented by <c>JSValue</c> in the
/// Core assembly so that the Storage assembly can reference property
/// values without a direct dependency on the runtime type system.
/// </summary>
// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
// Broiler-Falsified-If: a type other than JSValue, LazyDataPropertyCell or an IDeferredPropertyValue implements the marker and reaches a property slot, so reading that property throws InvalidOperationException instead of producing a value
// Broiler-Human:        PENDING
public interface IPropertyValue { }

/// <summary>
/// Marker interface for types that can act as a property getter or
/// setter inside <c>JSProperty</c>.  Implemented by <c>JSFunction</c>
/// in the Core assembly.  Extends <see cref="IPropertyValue"/> because
/// every accessor is also a valid property value.
/// </summary>
// Broiler-AI:           Origin=Ported; IP=Medium; Security=Medium; Resources=0; Fingerprint=TBF
// Broiler-Falsified-If: a getter or setter slot holds an accessor that is not a callable function and a property read invokes it instead of producing undefined
// Broiler-Human:        PENDING
public interface IPropertyAccessor : IPropertyValue { }
