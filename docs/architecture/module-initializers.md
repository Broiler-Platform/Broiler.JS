# Module initializers

Broiler.JS uses module initializers to wire optional satellite behavior when assemblies are loaded. The documented initializer pattern keeps core projects small while allowing built-ins, compiler services, CLR interop, and runtime helpers to register factories and delegates.

Integration tests verify that the expected initializer classes exist and that critical delegates are populated after satellite assemblies are loaded.
