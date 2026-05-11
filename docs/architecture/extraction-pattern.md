# Extraction pattern

Feature assemblies extend the engine through registration delegates instead of direct core dependencies. `DefaultBuiltInRegistry.AdditionalRegistrations`, `ConsoleFactory`, `StructuredCloneExtension`, and `IteratorPrototypeSetup` are the primary extension points verified by integration tests.

`DefaultBuiltInRegistry.AddProto` remains public and static so satellite assemblies can attach prototypes while preserving the dependency direction from feature assemblies to the engine layer.
