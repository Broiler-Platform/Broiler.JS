namespace Broiler.JavaScript.Runtime;

public enum ObjectStatus
{
    None = 0,
    Frozen = 1,
    Sealed = 2,
    NonExtensible = 4,
    SealedOrFrozen = 3,
    SealedFrozenNonExtensible = 7,

    // Immutable prototype exotic object (§10.4.7): its [[SetPrototypeOf]] only
    // succeeds when the new value equals the current [[Prototype]]; otherwise it
    // returns false (so Object.setPrototypeOf throws, Reflect.setPrototypeOf
    // returns false). Applies to %Object.prototype% and host global objects.
    ImmutablePrototype = 8
}
