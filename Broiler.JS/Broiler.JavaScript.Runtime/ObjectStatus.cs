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
    ImmutablePrototype = 8,

    // Roadmap item 2-9: this object's named properties have been written back into the
    // property trie, which is from then on the truth. Carried here rather than as its own
    // field because a bool would start a fresh alignment group and cost 8 bytes on EVERY
    // object — including the ones with no named properties at all, which the item is
    // supposed to make cheaper, not dearer.
    NamedPropertiesMaterialized = 16
}
