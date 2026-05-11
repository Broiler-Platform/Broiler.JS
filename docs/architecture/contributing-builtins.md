# Contributing built-ins

Built-in implementations live under `Broiler.JavaScript.BuiltIns.*` namespaces in the `Broiler.JavaScript.BuiltIns` assembly. Add new built-ins in the feature assembly, register them through the existing registry/delegate pattern, and cover behavior in `Broiler.JavaScript.BuiltIns.Tests` or an integration test when cross-assembly behavior is involved.

Compliance-oriented built-ins should reference the relevant ECMAScript clause or public compliance suite area in the test name or documentation update.
