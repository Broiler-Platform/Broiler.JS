using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Broiler.JavaScript.BuiltIns.Tests;

public class BuiltInsTests
{
    private static JSContext CreateContext(JavaScriptFeatureFlags experimentalFeatures = JavaScriptFeatureFlags.AllExperimentalEs2026)
        => new(experimentalFeatures: experimentalFeatures);

    [Fact]
    public void WeakRef_Construct_And_Deref()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var obj = { value: 42 }; var wr = new WeakRef(obj); wr.deref().value;");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Expm1_Remains_Monotonic_Around_Small_Negative_Inputs()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              return Math.expm1(-0.010000000000000002) <= Math.expm1(-0.01)
                && Math.expm1(-0.01) <= Math.expm1(-0.009999999999999998);
            })();
            """);

        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void EventTarget_Construct_Succeeds()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var t = new EventTarget(); typeof t;");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void FinalizationRegistry_Construct_Succeeds()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var fr = new FinalizationRegistry(function(v) {}); typeof fr;");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void Native_Error_Prototypes_Have_Own_Message_Property()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var ctors = [Error, EvalError, RangeError, ReferenceError, SyntaxError, TypeError, URIError, AggregateError];
                return ctors.map(function (Ctor) {
                    var descriptor = Object.getOwnPropertyDescriptor(Ctor.prototype, 'message');
                    return [
                        Object.prototype.hasOwnProperty.call(Ctor.prototype, 'message'),
                        descriptor.value === '',
                        descriptor.writable,
                        descriptor.enumerable === false,
                        descriptor.configurable
                    ].join(',');
                }).join('|');
            })();
            """);

        Assert.Equal("true,true,true,true,true|true,true,true,true,true|true,true,true,true,true|true,true,true,true,true|true,true,true,true,true|true,true,true,true,true|true,true,true,true,true|true,true,true,true,true", result.ToString());
    }

    [Fact]
    public void Function_Prototype_Apply_With_Primitive_Receiver_Throws_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("Function.prototype.apply.call(true, null, []);"));

        Assert.Equal("TypeError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
    }

    [Fact]
    public void Function_Prototype_Bind_With_NonCallable_Receiver_Throws_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("Function.prototype.bind.call({}, null);"));

        Assert.Equal("TypeError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
    }

    [Fact]
    public void DynamicFunction_Construct_Parses_Before_NewTarget_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var getProtoCalled = false;
                var newTarget = new Proxy(function () {}.bind(null), {
                    get: function (target, property, receiver) {
                        if (property === 'prototype') {
                            getProtoCalled = true;
                            return null;
                        }
                        return Reflect.get(target, property, receiver);
                    }
                });

                try {
                    Reflect.construct(Function, ['@error'], newTarget);
                    return 'no throw';
                } catch (e) {
                    return e.constructor.name + '|' + getProtoCalled;
                }
            })();
            """);

        Assert.Equal("SyntaxError|false", result.ToString());
    }

    [Fact]
    public void GeneratorFunction_Construct_Parses_Before_NewTarget_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var getProtoCalled = false;
                var newTarget = new Proxy(function () {}.bind(null), {
                    get: function (target, property, receiver) {
                        if (property === 'prototype') {
                            getProtoCalled = true;
                            return null;
                        }
                        return Reflect.get(target, property, receiver);
                    }
                });
                var Generator = (function* () {}).constructor;

                try {
                    Reflect.construct(Generator, ['@error'], newTarget);
                    return 'no throw';
                } catch (e) {
                    return e.constructor.name + '|' + getProtoCalled;
                }
            })();
            """);

        Assert.Equal("SyntaxError|false", result.ToString());
    }

    [Fact]
    public void AsyncGeneratorFunction_Construct_Parses_Before_NewTarget_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var getProtoCalled = false;
                var newTarget = new Proxy(function () {}.bind(null), {
                    get: function (target, property, receiver) {
                        if (property === 'prototype') {
                            getProtoCalled = true;
                            return null;
                        }
                        return Reflect.get(target, property, receiver);
                    }
                });
                var AsyncGenerator = (async function* () {}).constructor;

                try {
                    Reflect.construct(AsyncGenerator, ['@error'], newTarget);
                    return 'no throw';
                } catch (e) {
                    return e.constructor.name + '|' + getProtoCalled;
                }
            })();
            """);

        Assert.Equal("SyntaxError|false", result.ToString());
    }

    [Fact]
    public void Json_RawJson_Coerces_Invalid_Primitive_Inputs_To_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { JSON.rawJSON(Symbol('123')); }),
                    thrownCtor(function () { JSON.rawJSON(undefined); }),
                    thrownCtor(function () { JSON.rawJSON({}); }),
                    thrownCtor(function () { JSON.rawJSON([]); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|SyntaxError|SyntaxError|SyntaxError", result.ToString());
    }

    [Fact]
    public void Native_TypeErrors_Remain_TypeError_In_JavaScript_Catch()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Object.getPrototypeOf(); }),
                    thrownCtor(function () { Array.prototype.toLocaleString.call(null); }),
                    thrownCtor(function () { ''.padStart(1, Symbol()); }),
                    thrownCtor(function () { ''.padEnd(1, Symbol()); }),
                    thrownCtor(function () { WeakMap.prototype.set.call(new WeakMap(), 1, 1); }),
                    thrownCtor(function () { WeakSet.prototype.add.call(new WeakSet(), 1); }),
                    thrownCtor(function () { new WeakMap([[1, 1]]); }),
                    thrownCtor(function () { new WeakSet([1]); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void TypedArray_BaseConstructor_Throws_TypeError_When_Called_Or_Constructed()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var TypedArray = Object.getPrototypeOf(Int8Array);
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { TypedArray(); }),
                    thrownCtor(function () { new TypedArray(); }),
                    thrownCtor(function () { TypedArray(1); }),
                    thrownCtor(function () { new TypedArray(1); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Generator_BaseConstructor_Throws_TypeError_When_Called_Or_Constructed()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var Generator = Object.getPrototypeOf(function* () {});
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Generator(); }),
                    thrownCtor(function () { new Generator(); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Symbol_Primitives_And_Wrappers_Are_Not_Callable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var sym = Symbol('desc');
                var symObj = Object(Symbol());

                return [
                    thrownCtor(function () { sym(); }),
                    thrownCtor(function () { new sym(); }),
                    thrownCtor(function () { symObj(); }),
                    thrownCtor(function () { new symObj(); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Strict_Assignment_To_Inherited_Readonly_Property_Throws_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                'use strict';

                function foo() {}
                Object.defineProperty(foo.prototype, 'bar', { value: 'unwritable' });

                var o = new foo();

                try {
                    o.bar = 'overridden';
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name + '|' + o.bar + '|' + o.hasOwnProperty('bar');
                }
            })();
            """);

        Assert.Equal("TypeError|unwritable|false", result.ToString());
    }

    [Fact]
    public void JSON_Stringify_BigInt_Throws_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              return [
                thrownCtor(function () { JSON.stringify(0n); }),
                thrownCtor(function () { JSON.stringify(Object(0n)); }),
                thrownCtor(function () { JSON.stringify({ x: 0n }); })
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_Delete_Uses_Handler_Context_And_Enforces_Revocation_And_New()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var handler = {
                    deleteProperty: function(target, prop) {
                        return this === handler && prop === 'attr';
                    }
                };

                var revoked = Proxy.revocable({ attr: 1 }, {});
                revoked.revoke();

                return [
                    delete new Proxy({ attr: 1 }, handler).attr,
                    thrownCtor(function () { delete new Proxy({}, { deleteProperty: {} }).attr; }),
                    thrownCtor(function () { delete revoked.proxy.attr; }),
                    thrownCtor(function () { Proxy({}, {}); })
                ].join('|');
            })();
        ");

        Assert.Equal("true|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void JSON_Stringify_Circular_Replacer_Value_Throws_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              var circular = [{}];
              try {
                JSON.stringify(circular, function () { return circular; });
                return "no-throw";
              } catch (e) {
                return e.constructor.name;
              }
            })();
            """);

        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void JSON_Revoked_Proxy_Array_Checks_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (() => {
                var handle = Proxy.revocable([], {});
                handle.revoke();
                try {
                  JSON.stringify({}, handle.proxy);
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var handle = Proxy.revocable([], {});
                var calls = 0;
                handle.revoke();
                try {
                  JSON.parse('[null, null]', function() {
                    this[1] = handle.proxy;
                    calls += 1;
                  });
                  return 'no error';
                } catch (e) {
                  return e.name + ':' + calls;
                }
              })()
            ].join('|');
            """);

        Assert.Equal("TypeError|TypeError:1", result.ToString());
    }

    [Fact]
    public void Strict_Global_Readonly_Value_Assignments_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            "use strict";
            [
              (() => { try { NaN = 12; return 'no error'; } catch (e) { return e.name; } })(),
              (() => { try { Infinity = 12; return 'no error'; } catch (e) { return e.name; } })(),
              (() => { try { undefined = 12; return 'no error'; } catch (e) { return e.name; } })()
            ].join('|');
            """);

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Strict_Functions_Preserve_This_And_Throw_On_Strict_Only_Assignments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (() => {
                function f() {
                  "use strict";
                  return !this;
                }

                return f.call(null) ? 'true' : 'false';
              })(),
              (() => {
                function strict() {
                  "use strict";
                  this.insert = function() {};
                }

                try {
                  strict.call(undefined);
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                try {
                  (function() {
                    "use strict";
                    var value = 10;
                    value.x = 5;
                  })();
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var value = 10;
                value.x = 5;
                return value.x === undefined ? 'undefined' : 'defined';
              })(),
              (() => {
                try {
                  (function assignSelfStrict() {
                    "use strict";
                    assignSelfStrict = 12;
                  })();
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var assignSelf = 42;
                (function assignSelf() {
                  assignSelf = 12;
                })();
                return assignSelf;
              })()
            ].join('|');
            """);

        Assert.Equal("true|TypeError|TypeError|undefined|TypeError|42", result.ToString());
    }

    [Fact]
    public void Strict_Functions_Propagate_To_Nested_Functions_And_Callbacks()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (function () {
                function f1() {
                  "use strict";
                  function f() {
                    return typeof this;
                  }

                  return [f(), typeof this].join('|');
                }

                return f1();
              })(),
              (function () {
                var value = 'unreplaced';
                "ab".replace("b", (function () {
                  "use strict";
                  return function () {
                    value = typeof this;
                    return "a";
                  };
                })());

                return value;
              })()
            ].join(';');
            """);

        Assert.Equal("undefined|undefined;undefined", result.ToString());
    }

    [Fact]
    public void Strict_Scripts_Propagate_To_Nested_Functions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            "use strict";
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              return [
                thrownCtor(function () {
                  (function () {
                    var sym = Symbol('66');
                    sym.a = 0;
                  })();
                }),
                thrownCtor(function () {
                  (function () {
                    var sym = Symbol('66');
                    sym['a' + 'b'] = 0;
                  })();
                }),
                thrownCtor(function () {
                  (function () {
                    var sym = Symbol('66');
                    sym[62] = 0;
                  })();
                })
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Empty_Statements_Key_Coercion_And_Builtin_RegExp_Fallbacks_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        string Eval(string code)
        {
            using var ctx = new JSContext();
            return ctx.Eval(code).ToString();
        }

        Assert.Equal("true", Eval("""
            function foo() {
                ; 'use strict';
                return this !== undefined;
            }

            foo.call(undefined);
            """));

        Assert.Equal("true", Eval("function foo() {\n    'use str\\\nict';\n    return this !== undefined;\n}\n\nfoo.call(undefined);"));

        Assert.Equal("true", Eval("""
            (function () {
                var evaluated = 0;
                var base = {};
                var key = {
                    toString: function () {
                        evaluated++;
                        return '';
                    }
                };

                base[key] ^= 0;
                return evaluated === 1;
            })();
            """));

        Assert.Equal("true", Eval("""
            (function () {
                var evaluated = 0;
                var base = {};
                var key = {
                    toString: function () {
                        evaluated++;
                        return '';
                    }
                };

                ++base[key];
                return evaluated === 1;
            })();
            """));

        Assert.Equal("true", Eval("""
            (function () {
                var evaluated = 0;
                var base = {};
                var key = {
                    toString: function () {
                        evaluated++;
                        return '';
                    }
                };

                base[key]--;
                return evaluated === 1;
            })();
            """));

        Assert.Equal("true|true|true:true:true", Eval("""
            (function () {
                var groups = /./.exec('a');
                var descriptor = Object.getOwnPropertyDescriptor(groups, 'groups');
                return groups.hasOwnProperty('groups')
                    + '|' + (groups.groups === undefined)
                    + '|' + descriptor.writable + ':' + descriptor.enumerable + ':' + descriptor.configurable;
            })();
            """));
    }

    [Fact]
    public void Object_Create_Applies_Property_Descriptors_And_Rejects_Invalid_Accessors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (() => {
                try {
                  Object.create({}, { prop: { get: null } });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                try {
                  Object.create({}, { prop: { set: false } });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              Object.create({}, { prop: { value: 1, enumerable: true } }).prop
            ].join('|');
            """);

        Assert.Equal("TypeError|TypeError|1", result.ToString());
    }

    [Fact]
    public void Object_Create_And_DefineProperties_Coerce_Primitive_Properties_Arguments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (() => {
                try {
                  Object.create({}, "x");
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                try {
                  Object.defineProperties({}, "x");
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var target = {};
                return Object.defineProperties(target, true) === target ? 'same' : 'different';
              })()
            ].join('|');
            """);

        Assert.Equal("TypeError|TypeError|same", result.ToString());
    }

    [Fact]
    public void Object_DefineProperty_Uses_SameValue_For_Negative_Zero()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              (() => {
                var arr = [];
                Object.defineProperty(arr, '0', { value: -0 });
                try {
                  Object.defineProperty(arr, '0', { value: +0 });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var obj = {};
                Object.defineProperty(obj, 'value', { value: -0 });
                try {
                  Object.defineProperty(obj, 'value', { value: +0 });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })()
            ].join('|');
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Object_DefineProperties_Array_Length_Regressions_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              var arr1 = [0, 1];
              Object.defineProperty(arr1, '1', { value: 1, configurable: false });

              var arr2 = [];

                var arr3 = [1, 2, 3];
                Object.defineProperty(arr3, 'length', { writable: false });

                var arr4 = [];
                Object.defineProperty(arr4, 'length', { writable: false });

                var arr5 = [];
                Object.defineProperty(arr5, 'length', { writable: false });

              return [
                thrownCtor(function () {
                  Object.defineProperties(arr1, { length: { value: 1 } });
                }) + ':' + arr1.length,
                thrownCtor(function () {
                  Object.defineProperties(arr2, { length: { configurable: true } });
                }),
                thrownCtor(function () {
                  Object.defineProperties(arr3, { '3': { value: 'abc' } });
                }) + ':' + arr3.hasOwnProperty('3'),
                thrownCtor(function () {
                  Object.defineProperty(arr4, 'length', { value: 12 });
                }) + ':' + arr4.length,
                thrownCtor(function () {
                  Object.defineProperties(arr5, { length: { value: 12 } });
                }) + ':' + arr5.length
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError:2|TypeError|TypeError:false|TypeError:0|TypeError:0", result.ToString());
    }

    [Fact]
    public void Object_Seal_Prevents_Object_Assign_From_Adding_New_Properties()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              (() => {
                var target = Object.seal({ foo: 1 });
                try {
                  Object.assign(target, { get bar() {} });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              Object.isExtensible(Object.seal({}))
            ].join('|');
            """);

        Assert.Equal("TypeError|false", result.ToString());
    }

    [Fact]
    public void WeakRef_TypeOf_IsObject()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var wr = new WeakRef({}); typeof wr;");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void BuiltIns_ModuleInitializer_Registers()
    {
        EnsureBuiltInsLoaded();
        Assert.NotNull(DefaultBuiltInRegistry.AdditionalRegistrations);
    }

    [Fact]
    public void PropertyDescriptor_Accessors_Exist_For_Compatibility_BuiltIns()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            var descriptors = [
              Object.getOwnPropertyDescriptor(Array, Symbol.species),
              Object.getOwnPropertyDescriptor(Promise, Symbol.species),
              Object.getOwnPropertyDescriptor(Map, Symbol.species),
              Object.getOwnPropertyDescriptor(Set, Symbol.species),
              Object.getOwnPropertyDescriptor(RegExp, Symbol.species),
              Object.getOwnPropertyDescriptor(ArrayBuffer, Symbol.species),
              Object.getOwnPropertyDescriptor(TypedArray, Symbol.species),
              Object.getOwnPropertyDescriptor(Symbol.prototype, "description"),
              Object.getOwnPropertyDescriptor(RegExp.prototype, "dotAll"),
              Object.getOwnPropertyDescriptor(TypedArray.prototype, Symbol.toStringTag),
              Object.getOwnPropertyDescriptor(Intl.NumberFormat.prototype, "format")
            ];

            descriptors.every(function(desc) {
              return desc
                && typeof desc.get === "function"
                && desc.set === undefined
                && desc.enumerable === false
                && desc.configurable === true;
            });
            """);

        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Async_Generator_Functions_Share_Their_Intrinsic_Constructor_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              async function* first() {}
              async function* second() {}
              var AsyncGeneratorFunction = (async function* () {}).constructor;
              return first instanceof AsyncGeneratorFunction
                && second instanceof AsyncGeneratorFunction
                && Object.getPrototypeOf(first) === Object.getPrototypeOf(second);
            })();
            """);

        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void PropertyDescriptor_Accessors_Exist_For_Legacy_RegExp_Statics()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            var names = ["lastMatch", "$&", "lastParen", "$+", "leftContext", "$`", "rightContext", "$'", "$1", "$2", "$9"];
            names.every(function(name) {
              var desc = Object.getOwnPropertyDescriptor(RegExp, name);
              return desc
                && typeof desc.get === "function"
                && desc.set === undefined
                && desc.enumerable === false
                && desc.configurable === true;
            });
            """);

        Assert.True(result.BooleanValue);
    }

    // ── M2: JSMath tests ─────────────────────────────────────────────

    [Fact]
    public void Math_PI_ReturnsCorrectValue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.PI");
        Assert.Equal(Math.PI, result.DoubleValue, 10);
    }

    [Fact]
    public void Math_Abs_NegativeNumber()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.abs(-42)");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Floor_ReturnsFloor()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.floor(4.7)");
        Assert.Equal(4.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Ceil_ReturnsCeiling()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.ceil(4.1)");
        Assert.Equal(5.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Round_RoundsCorrectly()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.round(4.5)");
        Assert.Equal(5.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Max_ReturnsLargest()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.max(1, 5, 3)");
        Assert.Equal(5.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Min_ReturnsSmallest()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.min(1, 5, 3)");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Sqrt_ReturnsSquareRoot()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.sqrt(25)");
        Assert.Equal(5.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Pow_ReturnsPower()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.pow(2, 10)");
        Assert.Equal(1024.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Random_ReturnsBetweenZeroAndOne()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.random()");
        var value = result.DoubleValue;
        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void Math_Trunc_Truncates()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.trunc(42.84)");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Math_Sign_ReturnsSign()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Math.sign(-5)");
        Assert.Equal(-1.0, result.DoubleValue);
    }

    // ── M2: JSReflect tests ──────────────────────────────────────────

    [Fact]
    public void Reflect_TypeOf_IsObject()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof Reflect");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void Reflect_Apply_CallsFunction()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Reflect.apply(Math.floor, undefined, [1.75])");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void Reflect_OwnKeys_ReturnsKeys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var obj = { a: 1, b: 2 }; Reflect.ownKeys(obj).length");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void Reflect_Has_ChecksProperty()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Reflect.has({ x: 0 }, 'x')");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Reflect_Has_Recognizes_Boxed_String_Index_Properties()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var str = new String("hello");
              return [
                Reflect.has(str, "4"),
                Reflect.has(str, "-0"),
                Reflect.has(str, -0)
              ].join("|");
            })();
            """);

        Assert.Equal("true|false|true", result.ToString());
    }

    [Fact]
    public void Reflect_DefineProperty_Succeeds()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var obj = {};
            Reflect.defineProperty(obj, 'x', { value: 7 });
            obj.x;
        ");
        Assert.Equal(7.0, result.DoubleValue);
    }

    [Fact]
    public void Reflect_DefineProperty_And_DeleteProperty_Throw_TypeError_For_Invalid_Arguments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Reflect.defineProperty(1, 'p', {}); }),
                    thrownCtor(function () { Reflect.defineProperty(null, 'p', {}); }),
                    thrownCtor(function () { Reflect.defineProperty(undefined, 'p', {}); }),
                    thrownCtor(function () { Reflect.defineProperty('', 'p', {}); }),
                    thrownCtor(function () { Reflect.defineProperty(Symbol(), 'p', {}); }),
                    thrownCtor(function () { Reflect.defineProperty({}, 'p', 1); }),
                    thrownCtor(function () { Reflect.deleteProperty(1, 'p'); }),
                    thrownCtor(function () { Reflect.deleteProperty(null, 'p'); }),
                    thrownCtor(function () { Reflect.deleteProperty(undefined, 'p'); }),
                    thrownCtor(function () { Reflect.deleteProperty('', 'p'); })
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Reflect_PreventExtensions_Works()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var obj = {};
            Reflect.preventExtensions(obj);
            Reflect.isExtensible(obj);
        ");
        Assert.False(result.BooleanValue);
    }

    [Fact]
    public void Reflect_Get_ReturnsValue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Reflect.get({ x: 42 }, 'x')");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Reflect_Set_SetsValue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var obj = {};
            Reflect.set(obj, 'x', 99);
            obj.x;
        ");
        Assert.Equal(99.0, result.DoubleValue);
    }

    [Fact]
    public void Reflect_Operations_Forward_To_Proxy_Target_Internal_Methods()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrown(fn) {
                    try {
                        return 'ok:' + fn();
                    } catch (e) {
                        return 'throw:' + e.constructor.name;
                    }
                }

                var callableTarget = function () { return 1; };
                var callableHandle = Proxy.revocable(callableTarget, {});
                var callableProxy = new Proxy(callableHandle.proxy, {});

                var valueTarget = { foo: 1 };
                var valueHandle = Proxy.revocable(valueTarget, {});
                var valueProxy = new Proxy(valueHandle.proxy, {});

                var setTarget = {};
                var setHandle = Proxy.revocable(setTarget, {});
                var setProxy = new Proxy(setHandle.proxy, {});

                var before = [
                    thrown(function () { return Reflect.apply(callableProxy, undefined, []); }),
                    thrown(function () { return typeof Reflect.construct(callableProxy, []); }),
                    thrown(function () { return Reflect.has(valueProxy, 'foo'); }),
                    thrown(function () { return Reflect.ownKeys(valueProxy).join(','); }),
                    thrown(function () { return Reflect.set(setProxy, 'bar', 2); }),
                    setTarget.bar
                ].join('|');

                callableHandle.revoke();
                valueHandle.revoke();
                setHandle.revoke();

                var after = [
                    thrown(function () { return Reflect.apply(callableProxy, undefined, []); }),
                    thrown(function () { return Reflect.has(valueProxy, 'foo'); }),
                    thrown(function () { return Reflect.ownKeys(valueProxy).length; }),
                    thrown(function () { return Reflect.set(setProxy, 'bar', 3); })
                ].join('|');

                return before + '|' + after;
            })();
        ");

        Assert.Equal("ok:1|ok:object|ok:true|ok:foo|ok:true|2|throw:TypeError|throw:TypeError|throw:TypeError|throw:TypeError", result.ToString());
    }

    [Fact]
    public void In_Operator_Uses_Proxy_Has_When_Target_Is_A_Proxy()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrown(fn) {
                    try {
                        return 'ok:' + fn();
                    } catch (e) {
                        return 'throw:' + e.constructor.name;
                    }
                }

                var handle = Proxy.revocable({ foo: 1 }, {});
                var proxy = new Proxy(handle.proxy, {});
                var before = thrown(function () { return 'foo' in proxy; });
                handle.revoke();
                var after = thrown(function () { return 'foo' in proxy; });
                return before + '|' + after;
            })();
        ");

        Assert.Equal("ok:true|throw:TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_Has_Trap_Is_Used_And_Enforces_Target_Invariants()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        return 'ok:' + fn();
                    } catch (e) {
                        return 'throw:' + e.constructor.name;
                    }
                }

                var intercepted = [];
                var interceptedHandler = {
                    has: function(target, key) {
                        intercepted.push(String(this === interceptedHandler) + ':' + key);
                        return key === 'virtual';
                    }
                };
                var interceptedProxy = new Proxy({}, interceptedHandler);

                var nonCallableTrap = new Proxy({}, { has: 1 });

                var fixedTarget = {};
                Object.defineProperty(fixedTarget, 'fixed', {
                    value: 1,
                    configurable: false
                });
                var fixedProxy = new Proxy(fixedTarget, {
                    has: function() {
                        return false;
                    }
                });

                var sealedTarget = { missing: 1 };
                Object.preventExtensions(sealedTarget);
                var sealedProxy = new Proxy(sealedTarget, {
                    has: function() {
                        return false;
                    }
                });

                return [
                    thrownCtor(function () { return 'virtual' in interceptedProxy; }),
                    intercepted.join(','),
                    thrownCtor(function () { return Reflect.has(nonCallableTrap, 'x'); }),
                    thrownCtor(function () { return Reflect.has(fixedProxy, 'fixed'); }),
                    thrownCtor(function () { return Reflect.has(sealedProxy, 'missing'); })
                ].join('|');
            })();
        ");

        Assert.Equal("ok:true|true:virtual|throw:TypeError|throw:TypeError|throw:TypeError", result.ToString());
    }

    [Fact]
    public void Object_BuiltIns_Respect_Proxy_OwnPropertyKey_Order_When_OwnKeys_Trap_Is_Missing()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function same(actual, expected) {
                    if (actual.length !== expected.length) {
                        return false;
                    }

                    for (var i = 0; i < expected.length; i++) {
                        if (actual[i] !== expected[i]) {
                            return false;
                        }
                    }

                    return true;
                }

                function createTarget() {
                    var target = {};
                    var sym = Symbol();
                    target[sym] = 1;
                    target.foo = 2;
                    target[0] = 3;
                    return { target: target, sym: sym };
                }

                var definePropertiesState = createTarget();
                var definePropertiesKeys = [];
                Object.defineProperties({}, new Proxy(definePropertiesState.target, {
                    getOwnPropertyDescriptor: function (_target, key) {
                        definePropertiesKeys.push(key);
                    }
                }));

                var getOwnPropertyDescriptorsState = createTarget();
                var getOwnPropertyDescriptorsKeys = [];
                Object.getOwnPropertyDescriptors(new Proxy(getOwnPropertyDescriptorsState.target, {
                    getOwnPropertyDescriptor: function (_target, key) {
                        getOwnPropertyDescriptorsKeys.push(key);
                    }
                }));

                var freezeState = createTarget();
                var freezeKeys = [];
                Object.freeze(new Proxy(freezeState.target, {
                    getOwnPropertyDescriptor: function (target, key) {
                        freezeKeys.push(key);
                        return Reflect.getOwnPropertyDescriptor(target, key);
                    }
                }));

                var frozenState = createTarget();
                Object.freeze(frozenState.target);
                var isFrozenKeys = [];
                Object.isFrozen(new Proxy(frozenState.target, {
                    getOwnPropertyDescriptor: function (target, key) {
                        isFrozenKeys.push(key);
                        return Reflect.getOwnPropertyDescriptor(target, key);
                    }
                }));

                var sealedState = createTarget();
                Object.seal(sealedState.target);
                var isSealedKeys = [];
                Object.isSealed(new Proxy(sealedState.target, {
                    getOwnPropertyDescriptor: function (target, key) {
                        isSealedKeys.push(key);
                        return Reflect.getOwnPropertyDescriptor(target, key);
                    }
                }));

                var sealState = createTarget();
                var sealKeys = [];
                Object.seal(new Proxy(sealState.target, {
                    defineProperty: function (target, key, descriptor) {
                        sealKeys.push(key);
                        return Reflect.defineProperty(target, key, descriptor);
                    }
                }));

                return [
                    same(definePropertiesKeys, ['0', 'foo', definePropertiesState.sym]),
                    same(getOwnPropertyDescriptorsKeys, ['0', 'foo', getOwnPropertyDescriptorsState.sym]),
                    same(freezeKeys, ['0', 'foo', freezeState.sym]),
                    same(isFrozenKeys, ['0', 'foo', frozenState.sym]),
                    same(isSealedKeys, ['0', 'foo', sealedState.sym]),
                    same(sealKeys, ['0', 'foo', sealState.sym])
                ].join('|');
            })();
        ");

        Assert.Equal("true|true|true|true|true|true", result.ToString());
    }

    // ── M2: JSProxy tests ────────────────────────────────────────────

    [Fact]
    public void Proxy_TypeOf_IsFunction()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof Proxy");
        Assert.Equal("function", result.ToString());
    }

    [Fact]
    public void Proxy_GetTrap_Intercepts()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var handler = {
                get: function(target, name) {
                    return name in target ? target[name] : 37;
                }
            };
            var p = new Proxy({}, handler);
            p.a = 1;
            p.b;
        ");
        Assert.Equal(37.0, result.DoubleValue);
    }

    [Fact]
    public void Proxy_SetTrap_Intercepts()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var log = [];
            var handler = {
                set: function(obj, prop, value) {
                    log.push(prop);
                    obj[prop] = value;
                    return true;
                }
            };
            var p = new Proxy({}, handler);
            p.a = 1;
            log.length;
        ");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void Proxy_Construct_WithTargetAndHandler()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var target = { message: 'hello' };
            var handler = {};
            var p = new Proxy(target, handler);
            p.message;
        ");
        Assert.Equal("hello", result.ToString());
    }

    [Fact]
    public void Proxy_SetTrap_Receives_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var target = {};
            var seen = [];
            var proxy = new Proxy(target, {
                set: function(obj, prop, value, receiver) {
                    seen = [String(prop), String(value)];
                    obj[prop] = value;
                    return true;
                }
            });
            proxy.answer = 42;
            [seen[0], seen[1], target.answer].join('|');
        ");
        Assert.Equal("answer|42|42", result.ToString());
    }

    [Fact]
    public void Proxy_Revoked_Get_Set_And_ObjectKeys_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var revoked = Proxy.revocable({ fixed: 1 }, {});
            revoked.revoke();
            [
                (function () { try { return revoked.proxy.fixed; } catch (e) { return e.constructor.name; } })(),
                (function () { try { revoked.proxy.fixed = 2; return 'no-throw'; } catch (e) { return e.constructor.name; } })(),
                (function () { try { Object.keys(revoked.proxy); return 'no-throw'; } catch (e) { return e.constructor.name; } })()
            ].join('|');
        ");
        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_Set_Forwards_ThrowError_To_Proxy_Targets()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var target = {};
                Object.defineProperty(target, 'value', {
                    value: 1,
                    writable: false,
                    configurable: true
                });

                var inner = new Proxy(target, {});
                var outer = new Proxy(inner, {});

                return [
                    thrownCtor(function () {
                        (function () {
                            'use strict';
                            outer.value = 2;
                        })();
                    }),
                    Reflect.set(outer, 'value', 2) ? 'true' : 'false'
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|false", result.ToString());
    }

    [Fact]
    public void Reflect_Set_Uses_Inherited_Proxy_Trap_And_Preserves_Function_Prototype_Assignment()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var func = function () {};
                var funcTarget = new Proxy(func, {});
                var funcProxy = new Proxy(funcTarget, { set: undefined });

                Reflect.set(funcProxy, 'prototype', null);

                var trapCalls = 0;
                var target = new Proxy({}, {
                  set: function (_target, key) {
                    trapCalls++;
                    return key === 'foo';
                  }
                });
                var proxy = new Proxy(target, { set: undefined });
                var receiver = Object.create(proxy);

                return [
                  func.prototype === null,
                  Reflect.set(receiver, 'foo', 1),
                  trapCalls,
                  Reflect.set(proxy, 'bar', 2),
                  trapCalls
                ].join('|');
            })();
            """);

        Assert.Equal("true|true|1|false|2", result.ToString());
    }

    [Fact]
    public void Proxy_Revoked_Construct_Throws_TypeError_When_NewTarget_Prototype_Is_Resolved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var handlers = {
                get: function() {
                    handle.revoke();
                }
            };
            var handle = Proxy.revocable(function() {}, handlers);
            var f = handle.proxy;
            try {
                new f();
                return 'no-throw';
            } catch (e) {
                return e.constructor.name;
            }
        ");

        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_NonCallable_And_Invalid_Construct_Traps_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var applyProxy = new Proxy(function () {}, { apply: 1 });
                var constructProxy = new Proxy(function () {}, { construct: 1 });
                var badReturnProxy = new Proxy(function () {}, {
                    construct: function () {
                        return true;
                    }
                });

                return [
                    thrownCtor(function () { applyProxy(); }),
                    thrownCtor(function () { new constructProxy(); }),
                    thrownCtor(function () { new badReturnProxy(); })
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_GetTrap_Cannot_Lie_About_NonConfigurable_Readonly_Data_Properties()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var target = {};
            Object.defineProperty(target, 'fixed', {
                value: 1,
                writable: false,
                configurable: false
            });
            var proxy = new Proxy(target, {
                get: function() { return 2; }
            });
            try {
                proxy.fixed;
                return 'no-throw';
            } catch (e) {
                return e.constructor.name;
            }
        ");
        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_OwnKeys_Trap_Must_Report_NonConfigurable_Target_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var target = {};
            Object.defineProperty(target, 'fixed', {
                value: 1,
                enumerable: true,
                configurable: false
            });
            var proxy = new Proxy(target, {
                ownKeys: function() { return []; }
            });
            try {
                Object.keys(proxy);
                return 'no-throw';
            } catch (e) {
                return e.constructor.name;
            }
        ");
        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_OwnKeys_Trap_Rejects_Non_String_And_Non_Symbol_Entries()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                function ownKeysResult(value) {
                    return thrownCtor(function () {
                        Reflect.ownKeys(new Proxy({}, {
                            ownKeys: function () {
                                return [value];
                            }
                        }));
                    });
                }

                return [
                    ownKeysResult([]),
                    ownKeysResult(true),
                    ownKeysResult(null),
                    ownKeysResult({}),
                    ownKeysResult(undefined),
                    ownKeysResult(1)
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_Define_Delete_And_GetOwnPropertyDescriptor_Invariants_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var defineResult = (function () {
                    var proxy = new Proxy({}, {
                        defineProperty: function(target, prop, desc) {
                            Object.defineProperty(target, prop, {
                                configurable: false,
                                writable: true
                            });
                            return true;
                        }
                    });

                    return thrownCtor(function () {
                        Reflect.defineProperty(proxy, 'prop', { writable: false });
                    });
                })();

                var deleteResult = (function () {
                    var proxy = new Proxy({ prop: 1 }, {
                        deleteProperty: function(target, prop) {
                            Object.preventExtensions(target);
                            return true;
                        }
                    });

                    return thrownCtor(function () {
                        Reflect.deleteProperty(proxy, 'prop');
                    });
                })();

                var getOwnPropertyDescriptorResult = (function () {
                    var target = { foo: 1 };
                    var proxy = new Proxy(target, {
                        getOwnPropertyDescriptor: function() {
                            return;
                        }
                    });

                    Object.preventExtensions(target);
                    return thrownCtor(function () {
                        Object.getOwnPropertyDescriptor(proxy, 'foo');
                    });
                })();

                return [defineResult, deleteResult, getOwnPropertyDescriptorResult].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Proxy_Created_With_Revoked_Proxy_Target_Preserves_Typeof_Metadata()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var revokedObjectTarget = Proxy.revocable({}, {});
            revokedObjectTarget.revoke();

            var revokedFunctionTarget = Proxy.revocable(function () {}, {});
            revokedFunctionTarget.revoke();

            [
                typeof Proxy.revocable(revokedObjectTarget.proxy, {}).proxy,
                typeof Proxy.revocable(revokedFunctionTarget.proxy, {}).proxy
            ].join('|');
        ");

        Assert.Equal("object|function", result.ToString());
    }

    // ── M2: JSConsole tests ──────────────────────────────────────────

    [Fact]
    public void Console_TypeOf_IsObject()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof console");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void Console_Log_IsFunction()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof console.log");
        Assert.Equal("function", result.ToString());
    }

    [Fact]
    public void Console_Warn_IsFunction()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof console.warn");
        Assert.Equal("function", result.ToString());
    }

    [Fact]
    public void Console_Error_IsFunction()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("typeof console.error");
        Assert.Equal("function", result.ToString());
    }

    [Fact]
    public void Console_Log_ReturnsValue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("console.log('test')");
        Assert.Equal("test", result.ToString());
    }

    [Fact]
    public void ConsoleFactory_WiredByModuleInitializer()
    {
        EnsureBuiltInsLoaded();
        Assert.NotNull(DefaultBuiltInRegistry.ConsoleFactory);
    }

    [Fact]
    public void PropertyIsEnumerable_Supports_Index_And_Symbol_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var indexTarget = [];
              Object.defineProperties(indexTarget, { "0": { enumerable: true } });

              var symbol = Symbol("enumerable");
              var symbolTarget = {};
              Object.defineProperty(symbolTarget, symbol, {
                value: 1,
                writable: true,
                enumerable: true,
                configurable: true
              });

              var accessorTarget = {};
              accessorTarget.__defineSetter__("stringAcsr", function (_) {});

              return [
                Object.prototype.propertyIsEnumerable.call(indexTarget, "0"),
                Object.prototype.propertyIsEnumerable.call(symbolTarget, symbol),
                Object.prototype.propertyIsEnumerable.call(accessorTarget, "stringAcsr")
              ].join("|");
            })();
            """);

        Assert.Equal("true|true|true", result.ToString());
    }

    [Fact]
    public void Object_DefineProperties_And_GetOwnPropertySymbols_Support_Symbol_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var target = {};
              var first = Symbol("first");
              var second = Symbol("second");
              var descriptors = {};

              descriptors[first] = {
                value: 1,
                writable: true,
                enumerable: true,
                configurable: true
              };
              descriptors[second] = {
                get: function () { return 2; },
                enumerable: false,
                configurable: true
              };

              Object.defineProperties(target, descriptors);
              var symbols = Object.getOwnPropertySymbols(target);

              return [
                first in target,
                target[first],
                second in target,
                target[second],
                symbols.length,
                symbols[0] === first,
                symbols[1] === second
              ].join("|");
            })();
            """);

        Assert.Equal("true|1|true|2|2|true|true", result.ToString());
    }

    [Fact]
    public void Symbol_Assignment_On_NonExtensible_Object_Is_Silent_Outside_Strict_Mode()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var symbol = Symbol("blocked");
              var obj = {};
              Object.preventExtensions(obj);

              try {
                obj[symbol] = 1;
                return String(symbol in obj);
              } catch (e) {
                return e.name;
              }
            })();
            """);

        Assert.Equal("false", result.ToString());
    }

    // ── M3: JSJSON tests ─────────────────────────────────────────────

    [Fact]
    public void JSON_Parse_ReturnsObject()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("JSON.parse('{\"a\":1}').a");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void JSON_Stringify_ReturnsString()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("JSON.stringify({ a: 1 })");
        Assert.Equal("{\"a\":1}", result.ToString());
    }

    [Fact]
    public void JSON_Parse_WithReviver()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            JSON.parse('{""a"":1,""b"":2}', function(key, value) {
                return typeof value === 'number' ? value * 2 : value;
            }).a;
        ");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void JSON_Parse_Invalid_Text_Throws_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var defaultContext = new JSContext();
        using var sourceContext = CreateContext(JavaScriptFeatureFlags.JsonParseSourceTextAccess);

        var script = """
            [
              (() => { try { JSON.parse('{'); return 'no error'; } catch (e) { return e.name; } })(),
              (() => { try { JSON.parse('{', function (k, v, c) { return v; }); return 'no error'; } catch (e) { return e.name; } })()
            ].join('|');
            """;

        Assert.Equal("SyntaxError|SyntaxError", defaultContext.Eval(script).ToString());
        Assert.Equal("SyntaxError|SyntaxError", sourceContext.Eval(script).ToString());
    }

    [Fact]
    public void JSON_Stringify_WithIndent()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("JSON.stringify({ a: 1 }, null, 2)");
        Assert.Contains("\"a\"", result.ToString());
    }

    [Fact]
    public void JSON_Parse_SourceTextAccess_IsDisabled_WhenFlagIsOff()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.AllExperimentalEs2026 & ~JavaScriptFeatureFlags.JsonParseSourceTextAccess);
        var result = ctx.Eval("""
            var seen = [];
            JSON.parse('{"a":1}', function(key, value, context) {
                seen.push(context === undefined ? 'missing' : 'present');
                return value;
            });
            seen.join(',');
            """);
        Assert.Equal("missing,missing", result.ToString());
    }

    [Fact]
    public void JSON_Parse_SourceTextAccess_IsEnabled_WhenFlagIsOn()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.JsonParseSourceTextAccess);
        var result = ctx.Eval("""
            var seen = [];
            JSON.parse('{"a":1}', function(key, value, context) {
                if (key === 'a') {
                    seen.push(context === undefined ? 'missing' : 'present');
                    seen.push(context.source);
                }
                return value;
            });
            seen.join('|');
            """);
        Assert.Equal("present|1", result.ToString());
    }

    [Fact]
    public void Error_IsError_IsDisabled_WhenFlagIsOff()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.AllExperimentalEs2026 & ~JavaScriptFeatureFlags.ErrorIsError);
        var result = ctx.Eval("typeof Error.isError");
        Assert.Equal("undefined", result.ToString());
    }

    [Fact]
    public void Error_IsError_IsEnabled_WhenFlagIsOn()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.ErrorIsError);
        var result = ctx.Eval("[Error.isError(new Error('x')), Error.isError(new TypeError('y')), Error.isError({})].join('|')");
        Assert.Equal("true|true|false", result.ToString());
    }

    [Fact]
    public void Error_Constructors_Preserve_Names_Prototypes_And_Messages()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            var error = new Error('boom');
            var typeError = new TypeError('type boom');
            var referenceError = new ReferenceError('ref boom');

            return [
                Error.name,
                TypeError.name,
                ReferenceError.name,
                error.constructor === Error,
                typeError.constructor === TypeError,
                referenceError.constructor === ReferenceError,
                Object.getPrototypeOf(TypeError.prototype) === Error.prototype,
                Object.getPrototypeOf(ReferenceError.prototype) === Error.prototype,
                error instanceof Error,
                typeError instanceof TypeError,
                typeError instanceof Error,
                referenceError instanceof ReferenceError,
                referenceError instanceof Error,
                error.message,
                typeError.message,
                referenceError.message
            ].join('|');
        })();").ToString().Split('|');
        Assert.Equal(16, parts.Length);
        Assert.Equal("Error", parts[0]);
        Assert.Equal("TypeError", parts[1]);
        Assert.Equal("ReferenceError", parts[2]);
        Assert.Equal("true", parts[3]);
        Assert.Equal("true", parts[4]);
        Assert.Equal("true", parts[5]);
        Assert.Equal("true", parts[6]);
        Assert.Equal("true", parts[7]);
        Assert.Equal("true", parts[8]);
        Assert.Equal("true", parts[9]);
        Assert.Equal("true", parts[10]);
        Assert.Equal("true", parts[11]);
        Assert.Equal("true", parts[12]);
        Assert.Equal("boom", parts[13]);
        Assert.Equal("type boom", parts[14]);
        Assert.Equal("ref boom", parts[15]);
    }

    [Fact]
    public void Error_Constructors_Expose_Non_Writable_Prototype_Descriptors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var constructors = [Error, TypeError, SyntaxError, RangeError, ReferenceError, EvalError, URIError, AggregateError];
              return constructors.every(function (C) {
                var d = Object.getOwnPropertyDescriptor(C, 'prototype');
                return d
                  && d.value !== undefined
                  && d.writable === false
                  && d.enumerable === false
                  && d.configurable === false;
              });
            })();
            """);

        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void Function_Family_And_Error_Prototype_Metadata_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var asyncPrototype = Object.getPrototypeOf(async function () {});
              var generatorPrototype = Object.getPrototypeOf(function* () {});
              var asyncGeneratorPrototype = Object.getPrototypeOf(async function* () {});
              var AsyncFunction = asyncPrototype.constructor;
              var GeneratorFunction = generatorPrototype.constructor;
              var AsyncGeneratorFunction = asyncGeneratorPrototype.constructor;
              var errorInstance = new Error();
              var errorMessage = Object.getOwnPropertyDescriptor(Error.prototype, 'message');

              return [
                AsyncFunction.prototype === asyncPrototype,
                Object.isExtensible(AsyncFunction.prototype),
                GeneratorFunction.prototype === generatorPrototype,
                Object.isExtensible(GeneratorFunction.prototype),
                AsyncGeneratorFunction.prototype === asyncGeneratorPrototype,
                Object.isExtensible(AsyncGeneratorFunction.prototype),
                errorMessage.value === '',
                errorMessage.writable,
                errorMessage.enumerable,
                errorMessage.configurable,
                !errorInstance.hasOwnProperty('message'),
                errorInstance.message === Error.prototype.message
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true|true|true|true|true|false|true|true|true", result.ToString());
    }

    [Fact]
    public void Error_Undefined_Does_Not_Create_Own_Message_Property()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              new Error().hasOwnProperty('message'),
              new Error(undefined).hasOwnProperty('message'),
              new Error(null).message
            ].join('|');
            """);

        Assert.Equal("false|false|null", result.ToString());
    }

    [Fact]
    public void Built_In_Constructors_Expose_Non_Writable_Prototype_Descriptors_Without_Changing_User_Functions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              function UserCtor() {}
              var asyncPrototype = Object.getPrototypeOf(async function () {});
              var generatorPrototype = Object.getPrototypeOf(function* () {});
              var asyncGeneratorPrototype = Object.getPrototypeOf(async function* () {});
              var descriptors = [
                Object.getOwnPropertyDescriptor(Array, 'prototype'),
                Object.getOwnPropertyDescriptor(BigInt, 'prototype'),
                Object.getOwnPropertyDescriptor(DataView, 'prototype'),
                Object.getOwnPropertyDescriptor(Date, 'prototype'),
                Object.getOwnPropertyDescriptor(FinalizationRegistry, 'prototype'),
                Object.getOwnPropertyDescriptor(Map, 'prototype'),
                Object.getOwnPropertyDescriptor(Number, 'prototype'),
                Object.getOwnPropertyDescriptor(Promise, 'prototype'),
                Object.getOwnPropertyDescriptor(TypedArray, 'prototype'),
                Object.getOwnPropertyDescriptor(Int8Array, 'prototype'),
                Object.getOwnPropertyDescriptor(asyncPrototype.constructor, 'prototype'),
                Object.getOwnPropertyDescriptor(generatorPrototype.constructor, 'prototype'),
                Object.getOwnPropertyDescriptor(asyncGeneratorPrototype.constructor, 'prototype')
              ];
              var userDescriptor = Object.getOwnPropertyDescriptor(UserCtor, 'prototype');

              return [
                descriptors.every(function (descriptor) {
                  return descriptor
                    && descriptor.value
                    && descriptor.writable === false
                    && descriptor.enumerable === false
                    && descriptor.configurable === false;
                }),
                userDescriptor.writable,
                userDescriptor.enumerable,
                userDescriptor.configurable
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|false|false", result.ToString());
    }

    [Fact]
    public void Intl_Constructors_Expose_Non_Writable_Prototype_Descriptors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var descriptors = [
                Object.getOwnPropertyDescriptor(Intl.DateTimeFormat, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.DisplayNames, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.DurationFormat, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.ListFormat, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.Locale, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.NumberFormat, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.PluralRules, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.RelativeTimeFormat, 'prototype'),
                Object.getOwnPropertyDescriptor(Intl.Segmenter, 'prototype')
              ];

              return descriptors.every(function (descriptor) {
                return descriptor
                  && descriptor.value
                  && descriptor.writable === false
                  && descriptor.enumerable === false
                  && descriptor.configurable === false;
              });
            })();
            """);

        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void Custom_Error_Subclass_Chains_Preserve_Instanceof_And_Message()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            class BaseCustomError extends Error {}
            class DerivedCustomError extends BaseCustomError {}

            var error = new DerivedCustomError('custom boom');
            return [
                error.constructor === DerivedCustomError,
                Object.getPrototypeOf(DerivedCustomError.prototype) === BaseCustomError.prototype,
                Object.getPrototypeOf(BaseCustomError.prototype) === Error.prototype,
                error instanceof DerivedCustomError,
                error instanceof BaseCustomError,
                error instanceof Error,
                error.constructor.name,
                error.message
            ].join('|');
        })();").ToString().Split('|');
        Assert.Equal(8, parts.Length);
        Assert.Equal("true", parts[0]);
        Assert.Equal("true", parts[1]);
        Assert.Equal("true", parts[2]);
        Assert.Equal("true", parts[3]);
        Assert.Equal("true", parts[4]);
        Assert.Equal("true", parts[5]);
        Assert.Equal("DerivedCustomError", parts[6]);
        Assert.Equal("custom boom", parts[7]);
    }

    [Fact]
    public void Promise_Task_Preserves_Custom_Error_Subclass_In_Rejection()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var promise = Assert.IsType<JSPromise>(ctx.Eval(@"(function () {
            class Test262Error extends Error {}
            return Promise.reject(new Test262Error('custom boom'));
        })();"));

        var ex = Assert.Throws<JSException>(() =>
        {
            _ = promise.Task;
        });

        Assert.Equal("Test262Error", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("custom boom", ex.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Spread_Iterator_Abrupt_Completion_Preserves_Custom_Error_Subclass()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            class Test262Error extends Error {}
            var iter = {};
            iter[Symbol.iterator] = function() {
                return {
                    next: function() {
                        throw new Test262Error('custom boom');
                    }
                };
            };

            try {
                (function() {}(...iter));
                return 'no-throw';
            } catch (e) {
                return [e.constructor.name, e instanceof Test262Error, e.message].join('|');
            }
        })();");

        Assert.Equal("Test262Error|true|custom boom", result.ToString());
    }

    [Fact]
    public void Promise_Finally_On_Thenable_Preserves_Custom_Error_Subclass()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            class Test262Error extends Error {}
            var thrower = new Promise(function() {});
            thrower.then = function() {
                throw new Test262Error('custom boom');
            };

            try {
                Promise.prototype.finally.call(thrower);
                return 'no-throw';
            } catch (e) {
                return [e.constructor.name, e instanceof Test262Error, e.message].join('|');
            }
        })();");

        Assert.Equal("Test262Error|true|custom boom", result.ToString());
    }

    [Fact]
    public void Error_Constructor_Is_Callable_And_Global_Descriptor_Is_Not_Enumerable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            Error('boom') instanceof Error,
            Object.getPrototypeOf(Error) === Function.prototype,
            Object.prototype.propertyIsEnumerable.call(this, 'Error'),
            Error.name,
            Error.length
        ].join('|');");

        Assert.Equal("true|true|false|Error|1", result.ToString());
    }

    [Fact]
    public void NativeError_Constructors_Have_Error_As_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            Object.getPrototypeOf(TypeError) === Error,
            Object.getPrototypeOf(SyntaxError) === Error,
            Object.getPrototypeOf(RangeError) === Error,
            Object.getPrototypeOf(ReferenceError) === Error,
            Object.getPrototypeOf(URIError) === Error,
            Object.getPrototypeOf(EvalError) === Error,
            Object.getPrototypeOf(AggregateError) === Error
        ].join('|');");

        Assert.Equal("true|true|true|true|true|true|true", result.ToString());
    }

    [Fact]
    public void Intl_Constructors_Expose_Function_Metadata()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            Object.isExtensible(Intl),
            Object.getPrototypeOf(Intl) === Object.prototype,
            Object.prototype.toString.call(Intl.DateTimeFormat),
            Object.getPrototypeOf(Intl.DateTimeFormat) === Function.prototype,
            Object.prototype.toString.call(Intl.RelativeTimeFormat),
            Object.getPrototypeOf(Intl.RelativeTimeFormat) === Function.prototype,
            Intl.RelativeTimeFormat.name,
            Intl.RelativeTimeFormat.length
        ].join('|');");

        Assert.Equal("true|true|[object Function]|true|[object Function]|true|RelativeTimeFormat|0", result.ToString());
    }

    [Fact]
    public void BuiltIn_Functions_Keep_Function_Class_Tag()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            Object.getOwnPropertyDescriptor(Object.prototype, Symbol.toStringTag) === undefined,
            Object.prototype.toString.call(new Intl.NumberFormat().format),
            Object.prototype.toString.call(new Intl.DateTimeFormat().format),
            Object.prototype.toString.call(Intl.NumberFormat.supportedLocalesOf),
            Object.prototype.toString.call(String.prototype.localeCompare)
        ].join('|');");

        Assert.Equal("true|[object Function]|[object Function]|[object Function]|[object Function]", result.ToString());
    }

    [Fact]
    public void Array_FromAsync_IsDisabled_WhenFlagIsOff()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.AllExperimentalEs2026 & ~JavaScriptFeatureFlags.ArrayFromAsync);
        var result = ctx.Eval("typeof Array.fromAsync");
        Assert.Equal("undefined", result.ToString());
    }

    [Fact]
    public async Task Array_FromAsync_IsEnabled_WhenFlagIsOn()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.ArrayFromAsync);
        var promise = Assert.IsType<JSPromise>(ctx.Eval("Array.fromAsync([1, 2, 3], value => value * 2)"));
        var result = Assert.IsType<JSArray>(await promise.Task);
        ctx["resultArray"] = result;
        Assert.Equal("2,4,6", ctx.Eval("resultArray.join(',')").ToString());
    }

    [Fact]
    public void Object_And_Map_GroupBy_Are_Disabled_WhenFlagIsOff()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.AllExperimentalEs2026 & ~JavaScriptFeatureFlags.ObjectMapGroupBy);
        var result = ctx.Eval("typeof Object.groupBy + '|' + typeof Map.groupBy");
        Assert.Equal("undefined|undefined", result.ToString());
    }

    [Fact]
    public void Object_And_Map_GroupBy_Are_Enabled_WhenFlagIsOn()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.ObjectMapGroupBy);
        var result = ctx.Eval("""
            var objectGroups = Object.groupBy([1, 2, 3, 4], function(value) {
                return value % 2 === 0 ? 'even' : 'odd';
            });
            var mapGroups = Map.groupBy([1, 2, 3, 4], function(value) {
                return value % 2;
            });
            [
                objectGroups.odd.join(','),
                objectGroups.even.join(','),
                mapGroups.get(1).join(','),
                mapGroups.get(0).join(',')
            ].join('|');
            """);
        Assert.Equal("1,3|2,4|1,3|2,4", result.ToString());
    }

    [Fact]
    public void Iterator_Concat_IsDisabled_WhenFlagIsOff()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.AllExperimentalEs2026 & ~JavaScriptFeatureFlags.IteratorConcat);
        var result = ctx.Eval("typeof Iterator.concat + '|' + typeof Iterator.from");
        Assert.Equal("undefined|function", result.ToString());
    }

    [Fact]
    public void Iterator_Concat_IsEnabled_WhenFlagIsOn()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.IteratorConcat);
        var result = ctx.Eval("Iterator.concat([1,2], new Set([3,4]).values()).reduce((sum, value) => sum + value, 0);");
        Assert.Equal(10.0, result.DoubleValue);
    }

    // ── M3: DataView tests ───────────────────────────────────────────

    [Fact]
    public void DataView_Construct_Succeeds()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var buf = new ArrayBuffer(16); var dv = new DataView(buf); dv.byteLength;");
        Assert.Equal(16.0, result.DoubleValue);
    }

    [Fact]
    public void DataView_SetAndGetInt8()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var buf = new ArrayBuffer(4);
            var dv = new DataView(buf);
            dv.setInt8(0, 42);
            dv.getInt8(0);
        ");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void DataView_SetAndGetFloat32()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var buf = new ArrayBuffer(4);
            var dv = new DataView(buf);
            dv.setFloat32(0, 3.14, true);
            Math.round(dv.getFloat32(0, true) * 100) / 100;
        ");
        Assert.Equal(3.14, result.DoubleValue, 2);
    }

    [Fact]
    public void DataView_Methods_Throw_RangeError_For_Negative_Infinite_ByteOffset()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var dv = new DataView(new ArrayBuffer(8));
            [
                'getFloat16', 'getFloat32', 'getFloat64', 'getInt16', 'getInt32', 'getInt8', 'getUint16', 'getUint32', 'getUint8',
                'setFloat16', 'setFloat32', 'setFloat64', 'setInt16', 'setInt32', 'setInt8', 'setUint16', 'setUint32', 'setUint8'
            ].map(function (name) {
                try {
                    dv[name](-Infinity, 0, true);
                    return 'none';
                } catch (e) {
                    return e.constructor.name;
                }
            }).join('|');
        ");
        Assert.Equal("RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError", result.ToString());
    }

    [Fact]
    public void DataView_Fractional_Negative_ByteOffset_Truncates_Toward_Zero()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var dv = new DataView(new ArrayBuffer(1));
            dv.setInt8(-0.5, 42);
            dv.getInt8(-0.5);
        ");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void ArrayBuffer_And_DataView_Constructors_Expose_Spec_Length()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("[ArrayBuffer.length, DataView.length].join('|');");
        Assert.Equal("1|1", result.ToString());
    }

    [Fact]
    public void TypedArray_Includes_Propagates_FromIndex_ValueOf_Exception()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            try {
                new Float64Array([1]).includes(1, {
                    valueOf() {
                        throw thrown;
                    }
                });
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
        Assert.True(result.BooleanValue);
    }
    [Fact]
    public void Object_Proto_Assignment_Falls_Back_To_Own_Data_Property_After_Accessor_Deletion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              delete Object.prototype.__proto__;
              var subject = {};
              subject.__proto__ = 5;
              var descriptor = Object.getOwnPropertyDescriptor(subject, '__proto__');
              return descriptor.value === 5
                && descriptor.writable === true
                && descriptor.enumerable === true
                && descriptor.configurable === true
                && Object.prototype.hasOwnProperty.call(subject, '__proto__');
            })();
            """);

        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_From_Propagates_Source_Length_Exception()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            try {
                Float64Array.from({
                    get length() {
                        throw thrown;
                    }
                });
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_Construct_Propagates_NewTarget_Prototype_Getter_Exception()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var newTarget = function() {}.bind(null);
            Object.defineProperty(newTarget, 'prototype', {
                get() {
                    throw thrown;
                }
            });

            try {
                Reflect.construct(Float64Array, [], newTarget);
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_From_Calls_Custom_Constructor()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var called = 0;
            function ctor() {
                called++;
                throw thrown;
            }

            try {
                Float64Array.from.call(ctor, []);
                false;
            } catch (e) {
                e === thrown && called === 1;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_Of_Calls_Custom_Constructor()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var called = 0;
            function ctor() {
                called++;
                throw thrown;
            }

            try {
                Float64Array.of.call(ctor, 42);
                false;
            } catch (e) {
                e === thrown && called === 1;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_ToLocaleString_Propagates_ValueOf_Coercion_Exception()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            Number.prototype.toLocaleString = function() {
                return {
                    toString: undefined,
                    valueOf: function() {
                        throw thrown;
                    }
                };
            };

            try {
                new Float64Array([42]).toLocaleString();
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_ToLocaleString_Visits_Next_Element()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var calls = 0;
            Number.prototype.toLocaleString = function() {
                return {
                    toString: function() {
                        calls++;
                        if (calls > 1) {
                            throw thrown;
                        }
                        return '' + calls;
                    }
                };
            };

            try {
                new Float64Array([42, 0]).toLocaleString();
                false;
            } catch (e) {
                e === thrown && calls === 2;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_Set_Propagates_Value_Coercion_For_Invalid_Numeric_String_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var value = {
                valueOf: function() {
                    throw thrown;
                }
            };
            var sample = new Float64Array([42]);

            try {
                sample['1.1'] = value;
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_Set_Propagates_Value_Coercion_For_Out_Of_Bounds_Integer_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var value = {
                valueOf: function() {
                    throw thrown;
                }
            };
            var sample = new Float64Array([42]);

            try {
                sample['1'] = value;
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_DefineProperty_Propagates_Value_Coercion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var thrown = new Error('boom');
            var value = {
                valueOf: function() {
                    throw thrown;
                }
            };
            var sample = new Float64Array([42]);

            try {
                Object.defineProperty(sample, '0', { value: value });
                false;
            } catch (e) {
                e === thrown;
            }
        ");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void TypedArray_Constructs_From_Iterable_Object()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var iterable = {};
            iterable[Symbol.iterator] = function () {
                return [42][Symbol.iterator]();
            };

            var sample = new Float64Array(iterable);
            sample.length === 1 && sample[0] === 42;
        ");
        Assert.True(result.BooleanValue);
    }

    // ── M2: ArrayBuffer transfer tests ───────────────────────────────

    [Fact]
    public void ArrayBuffer_Transfer_Copies_Bytes_And_Detaches_Source()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var source = new ArrayBuffer(4);
            var sourceView = new Uint8Array(source);
            sourceView[0] = 7;
            sourceView[1] = 11;
            var moved = source.transfer(6);
            var movedView = new Uint8Array(moved);
            [source.detached, moved.detached, moved.byteLength, movedView[0], movedView[1], movedView[4]].join('|');
        ");
        Assert.Equal("true|false|6|7|11|0", result.ToString());
    }

    [Fact]
    public void ArrayBuffer_TransferToFixedLength_Returns_Transferred_Buffer()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var source = new ArrayBuffer(4);
            new Uint8Array(source)[0] = 99;
            var moved = source.transferToFixedLength(2);
            [source.detached, moved.detached, moved.byteLength, new Uint8Array(moved)[0]].join('|');
        ");
        Assert.Equal("true|false|2|99", result.ToString());
    }

    [Fact]
    public void ArrayBuffer_Transferred_Source_Throws_On_ByteLength_Access()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval(@"
            var source = new ArrayBuffer(4);
            source.transfer(2);
            source.byteLength;
        "));
    }

    // ── M2: Hashbang grammar tests ────────────────────────────────────

    [Fact]
    public void Hashbang_At_Start_Of_Source_Is_Ignored()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("#!/usr/bin/env node\n1 + 2;");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void Hashbang_Not_At_Start_Of_Source_Is_Rejected()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval("0;\n#!/usr/bin/env node\n1;"));
    }

    [Fact]
    public void Eval_Var_Can_Coexist_With_Global_Let()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("let value = 1; [eval('var value = 2; value;'), value].join('|');");
        Assert.Equal("2|1", result.ToString());
    }

    [Fact]
    public void Eval_Var_Can_Coexist_With_Global_Const()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("const value = 1; [eval('var value = 2; value;'), value].join('|');");
        Assert.Equal("2|1", result.ToString());
    }

    [Fact]
    public void Eval_Var_Can_Be_Redeclared_Across_Direct_Evals()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("let value = 1; [eval('var value = 2; value;'), eval('var value = 3; value;'), value].join('|');");
        Assert.Equal("2|3|1", result.ToString());
    }

    [Fact]
    public void Direct_Eval_Can_Read_And_Update_Function_Local_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              var value = 1;
              var inner = eval('value = 2; typeof value + "|" + value;');
              return inner + "|" + value;
            }())
            """);

        Assert.Equal("number|2|2", result.ToString());
    }

    [Fact]
    public void Direct_Eval_Function_Local_Var_Declarations_Do_Not_Leak_To_Global()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var initial = "unset";
            var postAssignment = "unset";
            (function () {
              eval('initial = localValue; localValue = 4; postAssignment = localValue; var localValue;');
            }());
            [
              String(initial),
              String(postAssignment),
              typeof localValue
            ].join('|')
            """);

        Assert.Equal("undefined|4|undefined", result.ToString());
    }

    [Fact]
    public void Direct_Eval_Block_Function_Declarations_Update_Visible_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              eval('{ function f() { return "declaration"; } } typeof f + "|" + f();'),
              (function () {
                var after;
                eval('{ function g() { return "inner declaration"; } } after = g;');
                return typeof after + "|" + after();
              }()),
              (function () {
                var updated;
                eval('{ function h() { return "first declaration"; } }if (true) function h() { return "second declaration"; } else function _h() {}updated = h;');
                return typeof updated + "|" + updated();
              }())
            ].join("||")
            """);

        Assert.Equal("function|declaration||function|inner declaration||function|second declaration", result.ToString());
    }

    [Fact]
    public void Eval_Block_Function_Declarations_Update_Existing_Var_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            eval('{ function directGlobal() { return "direct declaration"; } }');
            var direct = typeof directGlobal + "|" + directGlobal();
            var functionLocal = (function () {
              eval('{ function local() { return "local declaration"; } }');
              return typeof local + "|" + local();
              var local = 123;
            }());
            (0, eval)('{ function indirectGlobal() { return "indirect declaration"; } }');
            var indirect = typeof indirectGlobal + "|" + indirectGlobal();
            var directGlobal = 123;
            var indirectGlobal = 123;
            direct + "||" + functionLocal + "||" + indirect
            """);

        Assert.Equal("function|direct declaration||function|local declaration||function|indirect declaration", result.ToString());
    }

    [Fact]
    public void Direct_Eval_Block_Function_Declarations_Create_Configurable_Global_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            eval('var descriptor = Object.getOwnPropertyDescriptor(globalThis, "f");
              var summary = [
                typeof descriptor.value,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable
              ].join("|");
              if (true) function f() { return 1; }
              summary;');
            """);

        Assert.Equal("undefined|true|true|true", result.ToString());
    }

    [Fact]
    public void Eval_Function_Declarations_Create_Configurable_Global_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              eval('var descriptor = Object.getOwnPropertyDescriptor(globalThis, "f");
                var summary = [
                  typeof descriptor.value,
                  descriptor.writable,
                  descriptor.enumerable,
                  descriptor.configurable
                ].join("|");
                function f() { return 234; }
                summary;'),
              (0, eval)('var descriptor = Object.getOwnPropertyDescriptor(globalThis, "g");
                var summary = [
                  typeof descriptor.value,
                  descriptor.writable,
                  descriptor.enumerable,
                  descriptor.configurable
                ].join("|");
                if (true) function g() { return 1; }
                summary;')
            ].join("||")
            """);

        Assert.Equal("function|true|true|true||undefined|true|true|true", result.ToString());
    }

    [Fact]
    public void AnnexB_Block_Function_Declarations_Update_Function_Scope_Bindings()
    {
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
              var value = 1;
              { function value() { return "block"; } }
              return typeof value + "|" + value();
            }())
            """);

        Assert.Equal("function|block", result.ToString());
    }

    [Fact]
    public void AggregateError_Has_Global_Function_Descriptor()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var descriptor = Object.getOwnPropertyDescriptor(this, 'AggregateError');
            [
              typeof descriptor.value,
              descriptor.writable,
              descriptor.enumerable,
              descriptor.configurable,
              new AggregateError([1, 2], 'boom').errors.join(',')
            ].join('|');
            """);

        Assert.Equal("function|true|false|true|1,2", result.ToString());
    }

    [Fact]
    public void BuiltIn_Globals_Are_NonEnumerable_And_Configurable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              "Array",
              "Reflect",
              "Symbol"
            ].map(function (name) {
              var descriptor = Object.getOwnPropertyDescriptor(globalThis, name);
              return [
                name,
                typeof descriptor.value,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable
              ].join("|");
            }).join("||");
            """);

        Assert.Equal(
            "Array|function|true|false|true||Reflect|object|true|false|true||Symbol|function|true|false|true",
            result.ToString());
    }

    [Fact]
    public void AsyncIteratorPrototype_Exposes_SymbolAsyncIterator()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var prototype = Object.getPrototypeOf(
              Object.getPrototypeOf(
                Object.getPrototypeOf((async function* () {})())
              )
            );
            var descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.asyncIterator);
            [
              typeof descriptor.value,
              descriptor.writable,
              descriptor.enumerable,
              descriptor.configurable,
              descriptor.value.call(42)
            ].join('|');
            """);

        Assert.Equal("function|true|false|true|42", result.ToString());
    }

    [Fact]
    public void Common_Test262_ScriptHost_BuiltIns_Are_Exposed_And_Callable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            function snapshot(target, key) {
              var descriptor = Object.getOwnPropertyDescriptor(target, key);
              return [typeof descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable].join(',');
            }

            var dateLike = {
              valueOf: function () { return 1; },
              toString: function () { return '2'; }
            };

            var invalidHint;
            try {
              Date.prototype[Symbol.toPrimitive].call(dateLike, 'bogus');
              invalidHint = 'no-throw';
            } catch (e) {
              invalidHint = e.constructor.name;
            }

            var invalidThis;
            try {
              Date.prototype[Symbol.toPrimitive].call(undefined, 'number');
              invalidThis = 'no-throw';
            } catch (e) {
              invalidThis = e.constructor.name;
            }

            [
              snapshot(Object, 'hasOwn'),
              snapshot(String.prototype, 'at'),
              snapshot(String.prototype, Symbol.iterator),
              snapshot(Date.prototype, Symbol.toPrimitive),
              snapshot(TypedArray.prototype, 'findLast'),
              snapshot(TypedArray.prototype, 'findLastIndex'),
              'abc'.at(-1),
              Array.from(String.prototype[Symbol.iterator].call('ab')).join(','),
              Date.prototype[Symbol.toPrimitive].call(dateLike, 'number'),
              Date.prototype[Symbol.toPrimitive].call(dateLike, 'string'),
              invalidHint,
              invalidThis,
              Object.hasOwn({ answer: 42 }, 'answer'),
              new Uint8Array([1, 2, 3, 4]).findLast(function (value) { return value % 2 === 0; }),
              new Uint8Array([1, 2, 3, 4]).findLastIndex(function (value) { return value % 2 === 0; })
            ].join('|');
            """);

        Assert.Equal(
            "function,true,false,true|function,true,false,true|function,true,false,true|function,false,false,true|function,true,false,true|function,true,false,true|c|a,b|1|2|TypeError|TypeError|true|4|3",
            result.ToString());
    }

    [Fact]
    public void Direct_Eval_In_Function_Context_Rejects_Arguments_Declaration()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            try {
              async function * f(p = eval("var arguments")) {}
              f();
              'no error';
            } catch (e) {
              e.name;
            }
            """);

        Assert.Equal("SyntaxError", result.ToString());
    }

    [Fact]
    public void Direct_Eval_In_Parameter_Defaults_Rejects_Function_Body_Arguments_And_Eval_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            function getErrorName(run) {
              try {
                run();
                return "no error";
              } catch (e) {
                return e.name;
              }
            }

            [
              getErrorName(function () {
                async function * f(p = eval("var arguments = 'param'")) {
                  function arguments() {}
                }
                f();
              }),
              getErrorName(function () {
                ({
                  method(p = eval("var eval = 'param'")) {
                    let eval;
                  }
                }).method();
              }),
              getErrorName(function () {
                function f(p = eval("var arguments = 'param'")) {
                  var arguments;
                }
                f();
              })
            ].join("|");
            """);

        Assert.Equal("SyntaxError|SyntaxError|SyntaxError", result.ToString());
    }

    [Fact]
    public void Direct_Eval_In_Function_Activation_Persists_Local_Eval_Binding()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var originalEval = eval;

            function run() {
              eval("var eval = function (value) { return value + 1; }");
              return [eval(1), eval(2), eval === originalEval].join("|");
            }

            [run(), eval === originalEval].join("||");
            """);

        Assert.Equal("2|3|false||true", result.ToString());
    }

    // ── M2: JSMap tests ──────────────────────────────────────────────

    [Fact]
    public void Map_SetAndGet()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var m = new Map(); m.set('key', 42); m.get('key');");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Map_Size()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var m = new Map(); m.set('a', 1); m.set('b', 2); m.size;");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void Map_Has_ReturnsTrue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var m = new Map(); m.set('x', 1); m.has('x');");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Map_Delete_RemovesEntry()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        // Verify delete returns true when key exists
        var result = ctx.Eval("var m = new Map(); m.set('x', 1); m['delete']('x');");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Map_ForEach_Iterates()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var m = new Map();
            m.set('a', 1);
            m.set('b', 2);
            var sum = 0;
            m.forEach(function(k, v) { sum += v; });
            sum;
        ");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void Map_Delete_Removes_Number_BigInt_And_NaN_Keys_From_Lookup()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var number = 9007199254740991;
            var bigint = 9007199254740991n;
            var nanKey = -/a/g.missingProperty;
            var map = new Map([[number, number], [bigint, bigint]]);
            map.set(nanKey, 17);
            map.delete(number);
            var afterNumberDelete = [map.size, map.has(number), map.has(bigint)].join('|');
            map.delete(NaN);
            var afterNaNDelete = [map.has(nanKey), map.has(-nanKey), map.has(NaN)].join('|');
            afterNumberDelete + ';' + afterNaNDelete;
            """);

        Assert.Equal("2|false|true;false|false|false", result.ToString());
    }

    // ── M3: JSWeakMap tests ──────────────────────────────────────────

    [Fact]
    public void WeakMap_SetAndGet()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var wm = new WeakMap(); var k = {}; wm.set(k, 99); wm.get(k);");
        Assert.Equal(99.0, result.DoubleValue);
    }

    [Fact]
    public void WeakMap_Has_ReturnsTrue()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var wm = new WeakMap(); var k = {}; wm.set(k, 1); wm.has(k);");
        Assert.True(result.BooleanValue);
    }

    // ── M3: JSSet tests ──────────────────────────────────────────────

    [Fact]
    public void Set_Add_And_Has()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var s = new Set(); s.add(42); s.has(42);");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Set_Size()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var s = new Set(); s.add(1); s.add(2); s.add(2); s.size;");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void Set_Delete()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        // Verify delete returns true when element exists
        var result = ctx.Eval("var s = new Set(); s.add(1); s['delete'](1);");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Set_Union()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var a = new Set([1,2]); var b = new Set([2,3]); a.union(b).size;");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void Set_Intersection()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var a = new Set([1,2,3]); var b = new Set([2,3,4]); a.intersection(b).size;");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void Set_Difference()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var result = new Set([1,2,3]).difference(new Set([2,4]));
            [result.has(1), result.has(2), result.has(3), result.size].join('|');
        ");
        Assert.Equal("true|false|true|2", result.ToString());
    }

    [Fact]
    public void Set_SymmetricDifference()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var result = new Set([1,2,3]).symmetricDifference(new Set([2,4]));
            [result.has(1), result.has(2), result.has(3), result.has(4), result.size].join('|');
        ");
        Assert.Equal("true|false|true|true|3", result.ToString());
    }

    [Fact]
    public void Set_Subset_Superset_And_Disjoint_Methods()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var a = new Set([1, 2]);
            var b = new Set([1, 2, 3]);
            var c = new Set([4, 5]);
            [a.isSubsetOf(b), b.isSupersetOf(a), a.isDisjointFrom(c)].join('|');
        ");
        Assert.Equal("true|true|true", result.ToString());
    }

    [Fact]
    public void Set_Methods_Accept_SetLike_Objects()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var other = {
                size: 2,
                has(value) { return value === 2 || value === 4; },
                keys: function* () { yield 2; yield 4; }
            };
            var baseSet = new Set([1, 2, 3]);
            var union = baseSet.union(other);
            var intersection = baseSet.intersection(other);
            var difference = baseSet.difference(other);
            var symmetric = baseSet.symmetricDifference(other);
            [
                union.size, union.has(1), union.has(4),
                intersection.size, intersection.has(2),
                difference.size, difference.has(1), difference.has(3),
                symmetric.size, symmetric.has(1), symmetric.has(3), symmetric.has(4),
                baseSet.isSubsetOf(other), baseSet.isSupersetOf(other), baseSet.isDisjointFrom(other)
            ].join('|');
        ");
        Assert.Equal("4|true|true|1|true|2|true|true|3|true|true|true|false|false|false", result.ToString());
    }

    // ── M3: ES2025 built-in coverage ──────────────────────────────────

    [Fact]
    public async Task Promise_Try_Resolves_Return_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Promise.try((a, b) => a + b, 19, 23);");
        var promise = Assert.IsType<JSPromise>(result);
        var resolved = await promise.Task;
        Assert.Equal(42.0, resolved.DoubleValue);
    }

    [Fact]
    public async Task Promise_Try_Rejects_Synchronous_Exception()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Promise.try(() => { throw new Error('boom'); });");
        var promise = Assert.IsType<JSPromise>(result);
        await Assert.ThrowsAsync<JSException>(async () => await promise.Task);
    }

    [Fact]
    public void Promise_Try_Uses_The_Receiver_Constructor()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var captured = null;
            var calls = 0;
            function CustomPromise(executor) {
                captured = executor;
                calls++;
            }

            var instance = Promise.try.call(CustomPromise, () => 42);
            [instance.constructor === CustomPromise, calls, typeof captured].join('|');
        ");
        Assert.Equal("true|1|function", result.ToString());
    }

    [Fact]
    public void Promise_Invalid_Executors_Receivers_And_Then_Constructors_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var promise = Promise.resolve(1);
                promise.constructor = null;

                return [
                    thrownCtor(function () { Promise(function () {}); }),
                    thrownCtor(function () { Promise.call(null, function () {}); }),
                    thrownCtor(function () { new Promise({}); }),
                    thrownCtor(function () { Promise.resolve.call(1, 0); }),
                    thrownCtor(function () { Promise.resolve.call({}, 0); }),
                    thrownCtor(function () { Promise.all.call(1, []); }),
                    thrownCtor(function () { Promise.all.call(function InvalidPromiseConstructor() {}, []); }),
                    thrownCtor(function () { Promise.allSettled.call({}, []); }),
                    thrownCtor(function () { Promise.allSettledKeyed.call({}, {}); }),
                    thrownCtor(function () { Promise.any.call(true, []); }),
                    thrownCtor(function () { Promise.reject.call(undefined, 1); }),
                    thrownCtor(function () { Promise.reject.call(function InvalidPromiseConstructor() {}, 1); }),
                    thrownCtor(function () { promise.then(function (value) { return value; }); })
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Promise_And_Iterator_Validate_TypeErrors_Before_Observable_User_Code()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.IteratorConcat);
        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var prototypeGetterCalls = 0;
                var bound = (function () {}).bind();
                Object.defineProperty(bound, 'prototype', {
                    get() {
                        prototypeGetterCalls++;
                        throw new Test262Error();
                    }
                });

                var iteratorGetterCalls = 0;
                var iterable1 = {
                    get [Symbol.iterator]() {
                        iteratorGetterCalls++;
                        return function () {
                            throw new Test262Error();
                        };
                    }
                };
                var iterable2 = {
                    get [Symbol.iterator]() {
                        throw new Test262Error();
                    }
                };

                return [
                    thrownCtor(function () {
                        Reflect.construct(Promise, [], bound);
                    }) + '|' + prototypeGetterCalls,
                    thrownCtor(function () {
                        Iterator.concat(iterable1, null, iterable2);
                    }) + '|' + iteratorGetterCalls
                ].join('||');
            })();
            """);

        Assert.Equal("TypeError|0||TypeError|1", result.ToString());
    }

    [Fact]
    public void Reflect_Construct_And_Promise_Capability_Functions_Respect_Constructibility()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                function isConstructor(fn) {
                    try {
                        Reflect.construct(function () {}, [], fn);
                        return true;
                    } catch (e) {
                        return false;
                    }
                }

                var resolveFunction;
                var rejectFunction;
                new Promise(function (resolve, reject) {
                    resolveFunction = resolve;
                    rejectFunction = reject;
                });

                return [
                    Object.prototype.hasOwnProperty.call(resolveFunction, 'prototype'),
                    Object.prototype.hasOwnProperty.call(rejectFunction, 'prototype'),
                    isConstructor(function () {}),
                    isConstructor(() => {}),
                    isConstructor(resolveFunction),
                    isConstructor(rejectFunction),
                    thrownCtor(function () { new resolveFunction(); }),
                    thrownCtor(function () { new rejectFunction(); })
                ].join('|');
            })();
            """);

        Assert.Equal("false|false|true|false|false|false|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Generator_Functions_Object_Literal_Constructor_Methods_And_Reflect_Argument_Lists_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var objectLiteral = { constructor() { } };
                function* generator() {}
                async function* asyncGenerator() {}

                return [
                    thrownCtor(function () { new objectLiteral.constructor; }),
                    thrownCtor(function () { class A extends generator {} }),
                    thrownCtor(function () { class B extends asyncGenerator {} }),
                    thrownCtor(function () { Reflect.apply(Math.min, undefined); }),
                    thrownCtor(function () { Reflect.construct(Object); }),
                    thrownCtor(function () { Reflect.apply(Math.min, undefined, 1); }),
                    thrownCtor(function () { Reflect.construct(Object, 1); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Promise_Reactions_Run_After_Synchronous_Code()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Execute(@"
            var order = [];
            var promise = Promise.resolve('ok').then(value => {
                order.push('then:' + value);
                return order.join('|');
            });
            order.push('sync');
            promise;
        ");

        Assert.Equal("sync|then:ok", result.ToString());
    }

    [Fact]
    public void Promise_Nested_Resolution_Assimilates_Inner_Promise()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Execute(@"
            Promise.resolve('outer')
                .then(value => Promise.resolve(value + ':inner'))
                .then(value => value);
        ");

        Assert.Equal("outer:inner", result.ToString());
    }

    [Fact]
    public void Async_Await_Continuation_Runs_After_Synchronous_Code()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Execute(@"
            var order = [];
            async function run() {
                order.push('start');
                await Promise.resolve('step');
                order.push('after');
                return order.join('|');
            }

            var promise = run();
            order.push('sync');
            promise;
        ");

        Assert.Equal("start|sync|after", result.ToString());
    }

    [Fact]
    public void Async_Arrow_And_Method_Await_Do_Not_Mark_Program_As_Top_Level_Await()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Execute(@"
            var order = [];
            var arrow = async value => {
                order.push('arrow-start');
                var awaited = await Promise.resolve(value + ':arrow');
                order.push(awaited);
            };
            var obj = {
                async method(value) {
                    order.push('method-start');
                    var awaited = await Promise.resolve(value + ':method');
                    order.push(awaited);
                }
            };

            var done = Promise.resolve()
                .then(() => arrow('ok'))
                .then(() => obj.method('ok'))
                .then(() => order.join('|'));
            order.push('sync');
            done;
        ");

        Assert.Equal("sync|arrow-start|ok:arrow|method-start|ok:method", result.ToString());
    }

    [Fact]
    public void Promise_Rejection_Handlers_Run_In_Microtask_Order()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Execute(@"
            var order = [];
            var promise = Promise.reject('boom').then(value => value, reason => {
                order.push('reject:' + reason);
                return order.join('|');
            });
            order.push('sync');
            promise;
        ");

        Assert.Equal("sync|reject:boom", result.ToString());
    }

    [Fact]
    public void RegExp_Escape_Escapes_Syntax_Characters()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"RegExp.escape('hello.world?+[test]{x}(y)|/\\^$');");
        Assert.Equal(@"\x68ello\.world\?\+\[test\]\{x\}\(y\)\|\/\\\^\$", result.ToString());
    }

    [Fact]
    public void RegExp_Escape_Escapes_Whitespace_And_Rejects_NonStrings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"RegExp.escape('\uFEFF \u00A0\u202F');");
        Assert.Equal(@"\ufeff\x20\xa0\u202f", result.ToString());
        Assert.Throws<JSException>(() => ctx.Eval("RegExp.escape(123);"));
    }

    [Fact]
    public void RegExp_Escape_Exposes_Expected_Metadata_And_Descriptors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            var descriptor = Object.getOwnPropertyDescriptor(RegExp, 'escape');
            var lengthDescriptor = Object.getOwnPropertyDescriptor(RegExp.escape, 'length');
            var nameDescriptor = Object.getOwnPropertyDescriptor(RegExp.escape, 'name');

            return [
                RegExp.escape.length,
                RegExp.escape.name,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable,
                lengthDescriptor.writable,
                lengthDescriptor.enumerable,
                lengthDescriptor.configurable,
                nameDescriptor.writable,
                nameDescriptor.enumerable,
                nameDescriptor.configurable
            ].join('|');
        })();").ToString().Split('|');
        Assert.Equal(11, parts.Length);
        Assert.Equal("1", parts[0]);
        Assert.Equal("escape", parts[1]);
        Assert.Equal("true", parts[2]);
        Assert.Equal("false", parts[3]);
        Assert.Equal("true", parts[4]);
        Assert.Equal("false", parts[5]);
        Assert.Equal("false", parts[6]);
        Assert.Equal("true", parts[7]);
        Assert.Equal("false", parts[8]);
        Assert.Equal("false", parts[9]);
        Assert.Equal("true", parts[10]);
    }

    [Fact]
    public void Primitive_Wrapper_Addition_Uses_The_Wrapped_Primitive_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            new Number(1) + 1,
            new Boolean(true) + 1,
            '' + Object(1n),
            Object(1n) == '1'
        ].join('|');");
        Assert.Equal("2|2|1|true", result.ToString());
    }

    [Fact]
    public void Primitive_Wrappers_Respect_Overrides_And_Remain_Truthy()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            (function () {
                var boxed = new Number();
                boxed.valueOf = function () { return 17; };
                return boxed == 17;
            })(),
            (function () {
                var boxed = new Number();
                boxed.valueOf = function () { return 17; };
                return boxed + 3;
            })(),
            (function () {
                var boxed = new String();
                boxed.valueOf = function () { return 'foo'; };
                return boxed == 'foo';
            })(),
            !!new Number(0),
            !!new Boolean(false),
            !!new String('')
        ].join('|');");
        Assert.Equal("true|20|true|true|true|true", result.ToString());
    }

    [Fact]
    public void BigInt_Wrapper_Addition_Preserves_BigInt_Mixing_Rules()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            (function () {
                try {
                    return Object(1n) + 1;
                } catch (e) {
                    return e.constructor.name + '|' + e.message;
                }
            })(),
            (function () {
                var value = Object(1n) + 1n;
                return typeof value + '|' + String(value);
            })()
        ].join('||');");
        Assert.Equal("TypeError|Cannot mix BigInt and other types, use explicit conversions||bigint|2", result.ToString());
    }

    [Fact]
    public void Array_IsArray_Recognizes_ArrayPrototype_And_Proxy_Targets()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            Array.isArray(Array.prototype),
            Array.isArray(new Proxy([], {})),
            (function () {
                var revoked = Proxy.revocable([], {});
                revoked.revoke();
                try {
                    Array.isArray(revoked.proxy);
                    return 'no-throw';
                } catch (e) {
                    return e instanceof TypeError;
                }
            })()
        ].join('|');");
        Assert.Equal("true|true|true", result.ToString());
    }

    [Fact]
    public void Array_IsArray_Exposes_Expected_Metadata_And_Non_Array_Results()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            var descriptor = Object.getOwnPropertyDescriptor(Array, 'isArray');
            var lengthDescriptor = Object.getOwnPropertyDescriptor(Array.isArray, 'length');
            var nameDescriptor = Object.getOwnPropertyDescriptor(Array.isArray, 'name');

            return [
                Array.isArray([]),
                Array.isArray({ length: 0 }),
                Array.isArray(new Proxy({}, {})),
                Array.isArray(new Proxy(new Proxy([], {}), {})),
                Array.isArray.length,
                Array.isArray.name,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable,
                lengthDescriptor.writable,
                lengthDescriptor.enumerable,
                lengthDescriptor.configurable,
                nameDescriptor.writable,
                nameDescriptor.enumerable,
                nameDescriptor.configurable
            ].join('|');
        })();").ToString().Split('|');
        Assert.Equal(15, parts.Length);
        Assert.Equal("true", parts[0]);
        Assert.Equal("false", parts[1]);
        Assert.Equal("false", parts[2]);
        Assert.Equal("true", parts[3]);
        Assert.Equal("1", parts[4]);
        Assert.Equal("isArray", parts[5]);
        Assert.Equal("true", parts[6]);
        Assert.Equal("false", parts[7]);
        Assert.Equal("true", parts[8]);
        Assert.Equal("false", parts[9]);
        Assert.Equal("false", parts[10]);
        Assert.Equal("true", parts[11]);
        Assert.Equal("false", parts[12]);
        Assert.Equal("false", parts[13]);
        Assert.Equal("true", parts[14]);
    }

    [Fact]
    public void Function_Length_Metadata_Matches_Test262_Regression_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            function snapshot(fn) {
                var descriptor = Object.getOwnPropertyDescriptor(fn, 'length');
                return [descriptor.value, fn.length, descriptor.writable, descriptor.enumerable, descriptor.configurable].join(',');
            }

            return [
                snapshot(async function () {}.constructor),
                snapshot(BigInt),
                snapshot(DataView.prototype.getFloat16),
                snapshot(DataView.prototype.getFloat32),
                snapshot(DataView.prototype.getFloat64),
                snapshot(DataView.prototype.getInt16),
                snapshot(DataView.prototype.getInt32),
                snapshot(DataView.prototype.getUint16),
                snapshot(DataView.prototype.getUint32),
                snapshot(Date.parse),
                snapshot(FinalizationRegistry),
                snapshot(FinalizationRegistry.prototype.unregister),
                snapshot(Iterator.from),
                snapshot(Iterator.prototype.drop),
                snapshot(Iterator.prototype.every),
                snapshot(Iterator.prototype.filter),
                snapshot(Iterator.prototype.find),
                snapshot(Iterator.prototype.flatMap),
                snapshot(Iterator.prototype.forEach),
                snapshot(Iterator.prototype.map)
            ].join('|');
        })();").ToString().Split('|');

        Assert.All(parts, part => Assert.Equal("1,1,false,false,true", part));
    }

    [Fact]
    public void Additional_BuiltIn_Length_Metadata_Matches_Test262_Regression_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var parts = ctx.Eval(@"(function () {
            function snapshot(fn) {
                var descriptor = Object.getOwnPropertyDescriptor(fn, 'length');
                return [descriptor.value, fn.length, descriptor.writable, descriptor.enumerable, descriptor.configurable].join(',');
            }

            return [
                snapshot(Promise),
                snapshot(Promise.resolve),
                snapshot(Promise.reject),
                snapshot(String.prototype.padEnd),
                snapshot(String.prototype.padStart),
                snapshot(TypedArray.from),
                snapshot(Uint8Array.fromBase64),
                snapshot(Uint8Array.prototype.setFromBase64),
                snapshot(WeakRef)
            ].join('|');
        })();").ToString().Split('|');

        Assert.All(parts, part => Assert.Equal("1,1,false,false,true", part));
    }

    [Fact]
    public void Generated_BuiltIn_Method_Length_Metadata_Matches_Map_And_Math_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            function snapshot(fn) {
                var descriptor = Object.getOwnPropertyDescriptor(fn, 'length');
                return [descriptor.value, fn.length, descriptor.writable, descriptor.enumerable, descriptor.configurable].join(',');
            }

            return [
                snapshot(Map.prototype.clear),
                snapshot(Map.prototype.delete),
                snapshot(Map.prototype.forEach),
                snapshot(Map.prototype.get),
                snapshot(Map.prototype.has),
                snapshot(Math.abs),
                snapshot(Math.acos),
                snapshot(Math.atan2),
                snapshot(Math.f16round),
                snapshot(Math.floor),
                snapshot(Math.max),
                snapshot(Math.min),
                snapshot(Math.random)
            ].join('|');
        })();").ToString().Split('|');

        Assert.Equal(
            [
                "0,0,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "2,2,false,false,true",
                "1,1,false,false,true",
                "1,1,false,false,true",
                "2,2,false,false,true",
                "2,2,false,false,true",
                "0,0,false,false,true"
            ],
            parts);
    }

    [Fact]
    public void Dynamic_Function_Constructors_Preserve_Kind_And_Length_Metadata()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var GeneratorFunction = Object.getPrototypeOf(function* () {}).constructor;
              var AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
              var AsyncGeneratorFunction = Object.getPrototypeOf(async function* () {}).constructor;
              var generator = GeneratorFunction('x, y', 'yield x + y;');
              var asyncFunction = AsyncFunction('x', 'return x;');
              var asyncGenerator = AsyncGeneratorFunction('x', 'yield x;');

              return [
                Function('x, y', 'return x + y;').length,
                generator.length,
                asyncFunction.length,
                asyncGenerator.length,
                typeof generator(1, 2).next,
                generator(1, 2).next().value,
                typeof asyncFunction(1).then,
                typeof asyncGenerator(1).next
              ].join('|');
            })();
            """);

        Assert.Equal("2|2|1|1|function|3|function|function", result.ToString());
    }

    [Fact]
    public void Symbol_And_Generator_Instance_Prototype_Metadata_Match_Test262_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              return [
                Symbol.length,
                Object.getOwnPropertyNames(function* () {}.prototype).length,
                Object.getOwnPropertyNames(async function* () {}.prototype).length
              ].join('|');
            })();
            """);

        Assert.Equal("0|0|0", result.ToString());
    }

    [Fact]
    public void Array_Iteration_Methods_Preserve_Abrupt_Completions_From_Length_Property_And_Predicate()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            var lengthThrows = {};
            Object.defineProperty(lengthThrows, 'length', {
                get: function() { throw new RangeError('length'); },
                configurable: true
            });

            var propertyThrows = { length: 1 };
            Object.defineProperty(propertyThrows, '0', {
                get: function() { throw new SyntaxError('property'); },
                configurable: true
            });

            return [
                typeof Array.prototype.findLast,
                typeof Array.prototype.findLastIndex,
                thrownCtor(function () { Array.prototype.every.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.filter.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.find.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.findLast.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.findLastIndex.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.findLast.call(propertyThrows, function () { return false; }); }),
                thrownCtor(function () { Array.prototype.findLastIndex.call(propertyThrows, function () { return false; }); }),
                thrownCtor(function () { Array.prototype.findLast.call([1], function () { throw new URIError('predicate'); }); }),
                thrownCtor(function () { Array.prototype.findLastIndex.call([1], function () { throw new URIError('predicate'); }); })
            ].join('|');
        })();");

        Assert.Equal(
            "function|function|RangeError|RangeError|RangeError|RangeError|RangeError|SyntaxError|SyntaxError|URIError|URIError",
            result.ToString());
    }

    [Fact]
    public void Function_Prototype_Methods_Inherit_Bind_In_A_Fresh_Context()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            typeof Function.prototype.call.bind,
            Function.prototype.call.bind(Array.prototype.join)(['a', 'b'], ',')
        ].join('|');");
        Assert.Equal("function|a,b", result.ToString());
    }

    [Fact]
    public void Array_IsArray_Is_Not_A_Constructor_In_ReflectConstruct()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            try {
                Reflect.construct(function () {}, [], Array.isArray);
                return 'no-throw';
            } catch (e) {
                return e instanceof TypeError;
            }
        })();");
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void Array_From_Uses_Constructable_This_And_Falls_Back_For_NonConstructors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var date = Array.from.call(Date, ['A', 'B']);
                var obj = Array.from.call(Object, []);

                function C(arg) {
                    this.arg = arg;
                }

                var custom = Array.from.call(C, { length: 1, 0: 'zero' });
                var fallback = Array.from.call(() => ({}), [3, 4, 5]);

                return [
                    Array.isArray(date),
                    Object.prototype.toString.call(date),
                    Object.getPrototypeOf(date) === Date.prototype,
                    date.length,
                    date[0],
                    date[1],
                    Array.isArray(obj),
                    Object.getPrototypeOf(obj) === Object.prototype,
                    Object.getOwnPropertyNames(obj).join(','),
                    obj.length,
                    custom instanceof C,
                    custom.arg,
                    custom.length,
                    custom[0],
                    Array.isArray(fallback),
                    fallback.join(',')
                ].join('|');
            })();
            """);

        Assert.Equal("false|[object Date]|true|2|A|B|false|true|length|0|true|1|1|zero|true|3,4,5", result.ToString());
    }

    [Fact]
    public void Array_From_ArrayLike_Reads_Length_Before_Construct_And_Element_Access()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var log = "";
                var created;
                function C() {
                    log += "C";
                    created = this;
                }
                var arrayLike = {
                    get length() { log += "l"; return 1; },
                    get 0() { log += "0"; return "q"; }
                };
                var marker = {};
                try {
                    Array.from.call(C, arrayLike, function () { throw marker; });
                    return "no-throw";
                } catch (e) {
                    return [e === marker, log, created instanceof C].join("|");
                }
            })();
            """);

        Assert.Equal("true|lC0|true", result.ToString());
    }

    [Fact]
    public void Object_Assign_Throws_TypeError_For_Primitive_Frozen_And_NonExtensible_Targets()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            var sym = Symbol();
            var frozen = { [sym]: 1 };
            Object.freeze(frozen);

            var nonExtensible = {};
            Object.preventExtensions(nonExtensible);

            return [
                thrownCtor(function () { Object.assign('a', [1]); }),
                thrownCtor(function () { Object.assign(frozen, { [sym]: 1 }); }),
                thrownCtor(function () { Object.assign(nonExtensible, { bar: 1 }); })
            ].join('|');
        })();");

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Object_Integrity_And_DefineProperty_TypeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              var toStringAccessed = false;
              var valueOfAccessed = false;
              var ownProp = {
                toString: function() {
                  toStringAccessed = true;
                  return {};
                },
                valueOf: function() {
                  valueOfAccessed = true;
                  return {};
                }
              };

              var arr = [0, 1, 2];
              Object.defineProperty(arr, '1', { configurable: false });

              var trapFalse = new Proxy({}, {
                preventExtensions: function() {
                  return false;
                }
              });

              return [
                thrownCtor(function () { Object.preventExtensions(trapFalse); }),
                thrownCtor(function () { Object.seal(new Proxy({}, { preventExtensions: function() { return false; } })); }),
                thrownCtor(function () { Object.freeze(new Proxy({}, { preventExtensions: function() { return false; } })); }),
                thrownCtor(function () { Object.defineProperty({}, ownProp, {}); }) + ':' + toStringAccessed + ':' + valueOfAccessed,
                thrownCtor(function () { Object.defineProperty(arr, 'length', { value: 1 }); }) + ':' + arr.length,
                (function () {
                  var target = [1, 2, 3];
                  Object.defineProperty(target, 'length', { writable: false });
                  return thrownCtor(function () { Object.defineProperty(target, 3, { value: 'abc' }); });
                })(),
                (function () {
                  var target = [1];
                  return thrownCtor(function () { Object.defineProperty(target, 'length', { configurable: true }); });
                })()
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError:true:true|TypeError:2|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Object_IsFrozen_And_IsSealed_Match_Test262_For_Primitives_And_NonExtensible_Objects()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var frozenEmpty = Object.preventExtensions({});
              var nonFrozenObject = { x: 1 };
              Object.preventExtensions(nonFrozenObject);

              return [
                Object.isFrozen(),
                Object.isFrozen(undefined),
                Object.isFrozen(1),
                Object.isFrozen(Symbol()),
                Object.isFrozen(frozenEmpty),
                Object.isFrozen(nonFrozenObject),
                Object.isSealed(),
                Object.isSealed(null),
                Object.isSealed('foo'),
                Object.isSealed(frozenEmpty),
                Object.isSealed(nonFrozenObject)
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true|true|false|true|true|true|true|false", result.ToString());
    }

    [Fact]
    public void TypedArray_Seal_And_Freeze_Follow_IntegerIndexed_Object_Rules()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            'use strict';
            var sealedEmpty = Object.isSealed(Object.preventExtensions(new Int32Array(0)));
            var nonEmpty = new Int32Array(1);
            Object.preventExtensions(nonEmpty);
            var sealedNonEmpty = Object.isSealed(nonEmpty);
            var frozenNonEmpty = Object.isFrozen(nonEmpty);

            var sealTarget = new Int32Array(2);
            var sealError;
            try {
              Object.seal(sealTarget);
              sealError = 'none';
            } catch (e) {
              sealError = e.name;
            }

            var freezeTarget = new Int32Array(1);
            var freezeError;
            try {
              Object.freeze(freezeTarget);
              freezeError = 'none';
            } catch (e) {
              freezeError = e.name;
            }

            [
              sealedEmpty,
              sealedNonEmpty,
              frozenNonEmpty,
              sealError,
              Object.isExtensible(sealTarget),
              Object.isSealed(sealTarget),
              freezeError,
              Object.isExtensible(freezeTarget),
              Object.isFrozen(freezeTarget)
            ].join('|');
            """);

        Assert.Equal("true|false|false|TypeError|false|false|TypeError|false|false", result.ToString());
    }

    [Fact]
    public void Strict_Arguments_Callee_Uses_Frozen_ThrowTypeError_Getter()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var throwTypeError = Broiler.JavaScript.BuiltIns.Function.JSFunction.CreateFrozenThrowTypeErrorFunction(
            "ThrowTypeError",
            "Cannot access callee in strict mode");
        ref var ownProperties = ref throwTypeError.GetOwnProperties(false);
        ref var lengthProperty = ref ownProperties.GetValue(KeyStrings.length.Key);
        ref var nameProperty = ref ownProperties.GetValue(KeyStrings.name.Key);

        Assert.True(throwTypeError.IsFrozen());
        Assert.False(throwTypeError.IsExtensible());
        Assert.False(lengthProperty.IsConfigurable);
        Assert.True(lengthProperty.IsReadOnly);
        Assert.False(nameProperty.IsConfigurable);
        Assert.True(nameProperty.IsReadOnly);
        Assert.Equal(string.Empty, throwTypeError[KeyStrings.name].ToString());
    }

    [Fact]
    public void Anonymous_BuiltIn_Helper_Functions_Expose_Empty_Name_Metadata()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function snapshot(fn) {
                    var descriptor = Object.getOwnPropertyDescriptor(fn, 'name');
                    return JSON.stringify([descriptor.value, fn.name, descriptor.writable, descriptor.enumerable, descriptor.configurable]);
                }

                var executorFunction;
                function NotPromise(executor) {
                    executorFunction = executor;
                    executor(function () {}, function () {});
                }

                Promise.resolve.call(NotPromise);

                var resolveFunction;
                var rejectFunction;
                new Promise(function (resolve, reject) {
                    resolveFunction = resolve;
                    rejectFunction = reject;
                });

                return [
                    snapshot(executorFunction),
                    snapshot(resolveFunction),
                    snapshot(rejectFunction),
                    snapshot(Proxy.revocable({}, {}).revoke),
                    snapshot(new Intl.NumberFormat().format),
                    snapshot(new Intl.DateTimeFormat().format)
                ].join('|');
            })()
            """);

        Assert.Equal(
            """["","",false,false,true]|["","",false,false,true]|["","",false,false,true]|["","",false,false,true]|["","",false,false,true]|["","",false,false,true]""",
            result.ToString());
    }

    [Fact]
    public void Object_DefineProperty_Uses_Array_Length_Invariants()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              var arr = [];
              var first = thrownCtor(function () {
                Object.defineProperty(arr, 'length', {
                  get: function () {
                    return 2;
                  }
                });
              });

              Object.defineProperty(arr, 'length', { writable: false });
              var second = thrownCtor(function () {
                Object.defineProperty(arr, 'length', { writable: true });
              });

              return [first, second].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Array_Length_Remains_An_Own_Property_After_Prototype_Changes()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var array = [1, 2, 3];
              var descriptor = Object.getOwnPropertyDescriptor(array, 'length');
              array.__proto__ = null;

              return [
                descriptor.value,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable,
                'length' in array,
                Object.prototype.hasOwnProperty.call(array, 'length'),
                array.length,
                delete array.length
              ].join('|');
            })();
            """);

        Assert.Equal("3|true|false|false|true|true|3|false", result.ToString());
    }

    [Fact]
    public void Array_IsArray_And_Name_Properties_Are_Actually_Configurable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            var fn = Array.isArray;
            var deleteMethod = delete Array.isArray;
            var deleteName = delete fn.name;

            return [
                deleteMethod,
                Object.prototype.hasOwnProperty.call(Array, 'isArray'),
                deleteName,
                Object.prototype.hasOwnProperty.call(fn, 'name')
            ].join('|');
        })();");
        Assert.Equal("true|false|true|false", result.ToString());
    }

    [Fact]
    public void BuiltIn_Functions_Inherit_Length_From_FunctionPrototype_After_Delete()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
              var fun = Math.sin;
              delete fun.length;
              return [
                Function.prototype.hasOwnProperty('length'),
                'length' in fun,
                fun.length
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|0", result.ToString());
    }

    [Fact]
    public void Sloppy_Assignment_To_Readonly_Indexed_Properties_Does_Not_Throw()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var sloppy = ctx.Eval("""
            (function () {
              var target = [];
              Object.defineProperty(target, 1, { value: 1, writable: false });
              try {
                return 'ok:' + (target[1] = 42);
              } catch (e) {
                return 'throw:' + e.constructor.name;
              }
            })();
            """);
        var strict = ctx.Eval("""
            "use strict";
            (function () {
              var target = [];
              Object.defineProperty(target, 1, { value: 1, writable: false });
              try {
                return 'ok:' + (target[1] = 42);
              } catch (e) {
                return 'throw:' + e.constructor.name;
              }
            })();
            """);

        Assert.Equal("ok:42", sloppy.ToString());
        Assert.Equal("throw:TypeError", strict.ToString());
    }

    [Fact]
    public void Date_Prototype_SetYear_Is_Installed_With_AnnexB_Metadata_And_Behavior()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            var descriptor = Object.getOwnPropertyDescriptor(Date.prototype, 'setYear');
            var date = new Date(1970, 1, 2, 3, 4, 5);
            var expected = new Date(1971, 1, 2, 3, 4, 5).valueOf();

            return [
                Object.prototype.hasOwnProperty.call(Date.prototype, 'setYear'),
                typeof Date.prototype.setYear,
                descriptor.writable,
                descriptor.enumerable,
                descriptor.configurable,
                date.setYear(71) === expected,
                date.valueOf() === expected
            ].join('|');
        })();").ToString().Split('|');
        Assert.Equal(7, parts.Length);
        Assert.Equal("true", parts[0]);
        Assert.Equal("function", parts[1]);
        Assert.Equal("true", parts[2]);
        Assert.Equal("false", parts[3]);
        Assert.Equal("true", parts[4]);
        Assert.Equal("true", parts[5]);
        Assert.Equal("true", parts[6]);
    }

    [Fact]
    public void Date_Prototype_SetYear_TimeClip_Preserves_Valid_Extreme_Time_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            var valid = new Date(1970, 8, 10, 0, 0, 0, 0);
            var validReturn = valid.setYear(275760);
            var validValue = valid.valueOf();

            var invalid = new Date(1970, 8, 14, 0, 0, 0, 0);
            var invalidReturn = invalid.setYear(275760);
            var invalidValue = invalid.valueOf();

            return [
                validReturn === validReturn,
                validValue === validValue,
                invalidReturn !== invalidReturn,
                invalidValue !== invalidValue
            ].join('|');
        })();");

        Assert.Equal("true|true|true|true", result.ToString());
    }

    [Fact]
    public void Date_Prototype_ToGMTString_Is_The_ToUTCString_Alias()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"Date.prototype.toGMTString === Date.prototype.toUTCString;");
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void Escape_And_Unescape_Are_Installed_With_AnnexB_Metadata_And_Basic_Behavior()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var parts = ctx.Eval(@"(function () {
            var escapeDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'escape');
            var unescapeDescriptor = Object.getOwnPropertyDescriptor(globalThis, 'unescape');

            return [
                typeof escape,
                typeof unescape,
                escape.length,
                unescape.length,
                escape('A B©/'),
                unescape('%41%20%42%A9%2F'),
                escapeDescriptor.writable,
                escapeDescriptor.enumerable,
                escapeDescriptor.configurable,
                unescapeDescriptor.writable,
                unescapeDescriptor.enumerable,
                unescapeDescriptor.configurable
            ].join('|');
        })();").ToString().Split('|');

        Assert.Equal(12, parts.Length);
        Assert.Equal("function", parts[0]);
        Assert.Equal("function", parts[1]);
        Assert.Equal("1", parts[2]);
        Assert.Equal("1", parts[3]);
        Assert.Equal("A%20B%A9/", parts[4]);
        Assert.Equal("A B©/", parts[5]);
        Assert.Equal("true", parts[6]);
        Assert.Equal("false", parts[7]);
        Assert.Equal("true", parts[8]);
        Assert.Equal("true", parts[9]);
        Assert.Equal("false", parts[10]);
        Assert.Equal("true", parts[11]);
    }

    [Fact]
    public void Escape_And_Unescape_Propagate_TypeErrors_From_ToString_Coercion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            (function () {
                try {
                    escape(Symbol('x'));
                    return 'no-throw';
                } catch (e) {
                    return e.name + '|' + e.message;
                }
            })(),
            (function () {
                try {
                    unescape({
                        toString() { throw new Error('unreachable'); },
                        valueOf() { throw new Error('unreachable'); },
                        [Symbol.toPrimitive]() { return function () {}; }
                    });
                    return 'no-throw';
                } catch (e) {
                    return e.name;
                }
            })()
        ].join('||');");

        Assert.Equal(
            "TypeError|Cannot convert a Symbol value to a string.||TypeError",
            result.ToString());
    }

    [Fact]
    public void DecodeURIComponent_Decodes_Reserved_Characters_And_Rejects_Malformed_Four_Byte_Sequences()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        Assert.Equal(";/?:@&=+$,#", ctx.Eval("decodeURIComponent('%3B%2F%3F%3A%40%26%3D%2B%24%2C%23');").ToString());

        for (int index = 0xF0; index <= 0xF7; index++)
        {
            var hex = $"%{index:X2}";
            foreach (var malformed in new[]
            {
                hex,
                $"{hex}111%A0%A0",
                $"{hex}%00%A0%A0",
                $"{hex}%A0%00%A0",
                $"{hex}%A0%A0%00"
            })
            {
                var ex = Assert.Throws<JSException>(() => ctx.Eval($"decodeURIComponent('{malformed}');"));
                Assert.Equal("URIError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
            }
        }
    }

    [Fact]
    public void RegExp_Escape_Handles_Initial_Characters_And_Punctuators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            RegExp.escape('foo'),
            RegExp.escape('1abc'),
            RegExp.escape(','),
            RegExp.escape('!'),
            RegExp.escape('\uD800')
        ].join('|');");
        Assert.Equal(@"\x66oo|\x31abc|\x2c|\x21|\ud800", result.ToString());
    }

    [Fact]
    public void RegExp_Compile_Exists_And_Preserves_ToString_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            var badPattern = {
                toString: function() { throw new RangeError('pattern'); }
            };
            var badFlags = {
                toString: function() { throw new SyntaxError('flags'); }
            };

            return [
                typeof /./.compile,
                thrownCtor(function () { /./.compile(badPattern); }),
                thrownCtor(function () { /./.compile('', badFlags); }),
                thrownCtor(function () { /./.compile('', Symbol('x')); })
            ].join('|');
        })();");

        Assert.Equal("function|RangeError|SyntaxError|TypeError", result.ToString());
    }

    [Fact]
    public void String_Legacy_Html_Wrappers_Exist_And_Preserve_ToString_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            return [
                typeof String.prototype.big,
                typeof String.prototype.blink,
                typeof String.prototype.fontsize,
                typeof String.prototype.italics,
                typeof String.prototype.link,
                typeof String.prototype.sub,
                thrownCtor(function () { String.prototype.big.call({ toString: function () { throw new RangeError('this'); } }); }),
                thrownCtor(function () { String.prototype.blink.call({ toString: function () { throw new RangeError('this'); } }); }),
                thrownCtor(function () { String.prototype.italics.call({ toString: function () { throw new RangeError('this'); } }); }),
                thrownCtor(function () { String.prototype.sub.call({ toString: function () { throw new RangeError('this'); } }); }),
                thrownCtor(function () { String.prototype.fontsize.call('x', { toString: function () { throw new SyntaxError('attr'); } }); }),
                thrownCtor(function () { String.prototype.link.call('x', { toString: function () { throw new SyntaxError('attr'); } }); })
            ].join('|');
        })();");

        Assert.Equal(
            "function|function|function|function|function|function|RangeError|RangeError|RangeError|RangeError|SyntaxError|SyntaxError",
            result.ToString());
    }

    [Fact]
    public void Test262_Abrupt_Completions_For_Json_Object_RegExp_And_String_BuiltIns_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () {
                        var badDefine = new Proxy({ 0: null }, {
                            defineProperty: function () {
                                throw new Test262Error();
                            }
                        });

                        JSON.parse('["first", null]', function (_, value) {
                            if (value === 'first') {
                                this[1] = badDefine;
                            }
                            return value;
                        });
                    }),
                    thrownCtor(function () {
                        var abruptLength = new Proxy([], {
                            get: function (_target, key) {
                                if (key === 'length') {
                                    throw new Test262Error();
                                }
                            }
                        });

                        JSON.stringify(null, abruptLength);
                    }),
                    thrownCtor(function () {
                        JSON.stringify({}, function () {
                            throw new Test262Error();
                        });
                    }),
                    thrownCtor(function () {
                        var abruptLength = new Proxy([], {
                            get: function (_target, key) {
                                if (key === 'length') {
                                    throw new Test262Error();
                                }
                            }
                        });

                        JSON.stringify(abruptLength);
                    }),
                    thrownCtor(function () {
                        var source = new Proxy({ attr: null }, {
                            getOwnPropertyDescriptor: function () {
                                throw new Test262Error();
                            }
                        });

                        Object.assign({}, source);
                    }),
                    thrownCtor(function () {
                        var subject = new Proxy({}, {
                            setPrototypeOf: function () {
                                throw new Test262Error();
                            }
                        });

                        subject.__proto__ = {};
                    }),
                    thrownCtor(function () {
                        var r = /./g;
                        r.exec = function () {
                            return {
                                0: {
                                    toString: function () {
                                        throw new Test262Error();
                                    }
                                }
                            };
                        };

                        r[Symbol.match]('');
                    }),
                    thrownCtor(function () {
                        var regexp = /./;
                        Object.defineProperty(regexp, 'constructor', {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        regexp[Symbol.matchAll]('');
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.match, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.endsWith(obj);
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.match, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.includes(obj);
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.match, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.startsWith(obj);
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.match, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.match(obj);
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.replace, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.replace(obj);
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error",
            result.ToString());
    }

    [Fact]
    public void Test262_Harness_Abrupt_Completions_For_ReplaceAll_And_Intl_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function Test262Error(message) {
                    this.message = message || "";
                }

                Test262Error.prototype.toString = function () {
                    return "Test262Error: " + this.message;
                };

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () {
                        function custom() {
                            throw new Test262Error();
                        }

                        'a'.replaceAll('a', custom);
                    }),
                    (function () {
                        var searchValue = {
                            get [Symbol.match]() {
                                throw new Test262Error();
                            },
                            toString() {
                                throw new Error('unreachable');
                            }
                        };

                        var poisoned = 0;
                        var poison = {
                            toString() {
                                poisoned += 1;
                                throw new Error('unreachable');
                            }
                        };

                        return [
                            thrownCtor(function () {
                                ''.replaceAll.call(poison, searchValue, poison);
                            }),
                            String(poisoned)
                        ].join(',');
                    })(),
                    thrownCtor(function () {
                        var locales = {
                            '0': 'en-US',
                            length: 2
                        };

                        Object.defineProperty(locales, '1', {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        Intl.getCanonicalLocales(locales);
                    }),
                    thrownCtor(function () {
                        var locales = {
                            '0': 'en-US',
                            '1': 'pt-BR',
                            length: 2
                        };

                        var proxy = new Proxy(locales, {
                            has: function (target, key) {
                                if (key === '0') {
                                    throw new Test262Error();
                                }

                                return key in target;
                            }
                        });

                        Intl.getCanonicalLocales(proxy);
                    }),
                    thrownCtor(function () {
                        var options = {};
                        Object.defineProperty(options, 'granularity', {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        new Intl.Segmenter(undefined, options);
                    }),
                    thrownCtor(function () {
                        var custom = new Proxy(function () {}, {
                            get: function (target, key) {
                                if (key === 'prototype') {
                                    throw new Test262Error();
                                }

                                return target[key];
                            }
                        });

                        Reflect.construct(Intl.Segmenter, [], custom);
                    })
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|Test262Error,0|Test262Error|Test262Error|Test262Error|Test262Error", result.ToString());
    }

    [Fact]
    public void ReplaceAll_Throws_TypeError_For_Symbol_String_Coercions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                function symbolResult() {
                    return {
                        toString() {
                            return Symbol();
                        }
                    };
                }

                return [
                    thrownCtor(function () { 'a'.replaceAll(Symbol(), 'b'); }),
                    thrownCtor(function () { 'a'.replaceAll('a', Symbol()); }),
                    thrownCtor(function () { 'a'.replaceAll('a', symbolResult); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Symbol_Relational_Comparisons_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { return Symbol() < 0; }),
                    thrownCtor(function () { return Symbol() <= 0; }),
                    thrownCtor(function () { return Symbol() > 0; }),
                    thrownCtor(function () { return Symbol() >= 0; }),
                    thrownCtor(function () { return Object(Symbol()) < 'x'; })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void RegExp_Test_And_Exec_Throw_When_LastIndex_Is_Not_Writable()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var regex = /0/g;
                Object.freeze(regex);

                return [
                    thrownCtor(function () { regex.test('abc000'); }),
                    thrownCtor(function () { regex.exec('abc000'); }),
                    Object.getOwnPropertyDescriptor(regex, 'lastIndex').value
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|0", result.ToString());
    }

    [Fact]
    public void Test262_Abrupt_Completions_For_Date_Error_Object_And_Promise_BuiltIns_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var coercibleKey = {
                    toString: function () {
                        throw new Test262Error();
                    }
                };

                var poisonedThen = Object.defineProperty({}, 'then', {
                    get: function () {
                        throw new Test262Error();
                    }
                });

                function CustomPromise() {
                    throw new Test262Error();
                }

                return [
                    thrownCtor(function () {
                        Date.prototype.toJSON.call({
                            get valueOf() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        Date.prototype.toJSON.call({
                            toString: function () {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        Error.prototype.toString.call({
                            get name() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        Error.prototype.toString.call({
                            get message() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        Object.prototype.hasOwnProperty.call(null, coercibleKey);
                    }),
                    thrownCtor(function () {
                        Promise.prototype.catch.call(poisonedThen);
                    }),
                    thrownCtor(function () {
                        Promise.allSettled.call(CustomPromise);
                    }),
                    thrownCtor(function () {
                        Promise.any.call(CustomPromise);
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error",
            result.ToString());
    }

    [Fact]
    public void Error_Subclass_Default_Constructor_Uses_Derived_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}
                var error = new Test262Error('boom');
                return [
                    error instanceof Test262Error,
                    error instanceof Error,
                    error.constructor.name
                ].join('|');
            })();
            """);

        Assert.Equal("true|true|Test262Error", result.ToString());
    }

    [Fact]
    public void Legacy_Object_Prototype_Helpers_Preserve_Abrupt_Completions_And_Key_Coercion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var key = {
                    toString: function () {
                        throw new Test262Error();
                    }
                };
                var noop = function () {};
                var root = Object.defineProperty({}, 'target', { get: function () {} });
                var thrower = function () {
                    throw new Test262Error();
                };
                var lookupSubject = new Proxy(root, { getOwnPropertyDescriptor: thrower });
                var defineSubject = new Proxy({}, { defineProperty: thrower });

                return [
                    thrownCtor(function () {
                        ({}).__defineGetter__(key, noop);
                    }),
                    thrownCtor(function () {
                        ({}).__defineSetter__(key, noop);
                    }),
                    thrownCtor(function () {
                        ({}).__lookupGetter__(key);
                    }),
                    thrownCtor(function () {
                        ({}).__lookupSetter__(key);
                    }),
                    thrownCtor(function () {
                        lookupSubject.__lookupGetter__('target');
                    }),
                    thrownCtor(function () {
                        defineSubject.__defineSetter__('attr', noop);
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error",
            result.ToString());
    }

    [Fact]
    public void Legacy_Object_Prototype_Helpers_Handle_Uninterned_String_Property_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function getter() {}
                function setter() {}
                var subject = {};

                subject.__defineGetter__('baz', getter);
                subject.__defineSetter__('baz', setter);
                Object.defineProperty(subject, 'baz', { enumerable: false });

                var descriptor = Object.getOwnPropertyDescriptor(subject, 'baz');
                return [
                    Object.prototype.hasOwnProperty.call(subject, 'baz'),
                    Object.prototype.hasOwnProperty.call(subject, 'undefined'),
                    descriptor.get === getter,
                    descriptor.set === setter,
                    descriptor.enumerable,
                    descriptor.configurable
                ].join('|');
            })();
            """);

        Assert.Equal("true|false|true|true|false|true", result.ToString());
    }

    [Fact]
    public void Legacy_Object_Prototype_Helpers_Throw_On_Non_Extensible_Targets()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var subject = Object.preventExtensions({ existing: null });
                var noop = function () {};

                subject.__defineGetter__('existing', noop);
                subject.__defineSetter__('existing', noop);

                return [
                    thrownCtor(function () { subject.__defineGetter__('brand new getter', noop); }),
                    thrownCtor(function () { subject.__defineSetter__('brand new setter', noop); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Unresolved_Identifier_Reads_Throw_ReferenceError_In_Binary_Expressions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            [
                (function () { try { missingValue + 1; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { missingValue === 1; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })()
            ].join('||');
        ");

        Assert.Equal("ReferenceError|missingValue is not defined||ReferenceError|missingValue is not defined", result.ToString());
    }

    [Fact]
    public void Unresolved_Identifier_Reads_Throw_ReferenceError_In_Grouped_And_Reversed_Binary_Expressions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            [
                (function () { try { 1 + missingValue; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { (missingValue) + 1; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { 1 + (missingValue); return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { 1 === missingValue; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { (missingValue) === 1; return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })(),
                (function () { try { 1 === (missingValue); return 'no-throw'; } catch (e) { return e.constructor.name + '|' + e.message; } })()
            ].join('||');
        ");

        var expected = string.Join("||", Enumerable.Repeat("ReferenceError|missingValue is not defined", 6));
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void Typeof_Unresolved_Identifier_Still_Returns_Undefined()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("typeof missingValue;");

        Assert.Equal("undefined", result.ToString());
    }

    [Fact]
    public void NewFunction_Can_Assign_To_Global_Var_Identifier()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            var ret;
            (new Function('ret = ""ok"";'))();
            this['ret'];
        ");

        Assert.Equal("ok", result.ToString());
    }

    [Fact]
    public void NewFunction_Uses_Global_Object_As_Default_This()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            this.__functionCtorGlobal = 7;
            var globalObject = Function('return this')();
            globalObject.__functionCtorGlobal = globalObject.__functionCtorGlobal + 1;
            [globalObject === this, this.__functionCtorGlobal].join('|');
        ");

        Assert.Equal("true|8", result.ToString());
    }

    [Fact]
    public void NewFunction_Invalid_Parameter_Syntax_Throws_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            try {
              Function('-->', '');
              'no error';
            } catch (e) {
              e.name;
            }
            """);

        Assert.Equal("SyntaxError", result.ToString());
    }

    [Fact]
    public void NewFunction_AnnexB_Html_Comment_Parameters_Parse_Correctly()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        Assert.Equal(1.0, ctx.Eval("Function('<!--', 'return 1;')()").DoubleValue);
        Assert.Equal(1.0, ctx.Eval("Function('\\n-->', 'return 1;')()").DoubleValue);
        Assert.Equal("SyntaxError", ctx.Eval("""
            try {
              Function('-->', '');
              'no error';
            } catch (e) {
              e.name;
            }
            """).ToString());
    }

    [Fact]
    public void NewFunction_AnnexB_Html_Comment_Bodies_Parse_Correctly()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              typeof Function('<!--'),
              typeof Function('-->'),
              typeof Function('\n-->')
            ].join('|');
            """);

        Assert.Equal("function|function|function", result.ToString());
    }

    [Fact]
    public void NewFunction_Strict_Body_Rejects_Eval_Parameter()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            try {
              Function('eval', '"use strict";');
              'no error';
            } catch (e) {
              e.name;
            }
            """);

        Assert.Equal("SyntaxError", result.ToString());
    }

    [Fact]
    public void NewFunction_Strict_Body_Makes_Direct_Eval_Strict()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            try {
              var funObj = new Function("a", "'use strict'; eval('public = 1;');");
              funObj();
              'no error';
            } catch (e) {
              e.name;
            }
            """);

        Assert.Equal("SyntaxError", result.ToString());
    }

    [Fact]
    public void NewFunction_Rejects_Invalid_Source_And_Strict_Nested_Function_Early_Errors()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              (() => { try { Function('@error'); return 'no error'; } catch (e) { return e.name; } })(),
              (() => {
                try {
                  Function("'use strict'; var f = function () { var o = {}; with (o) {}; };");
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })()
            ].join('|');
            """);

        Assert.Equal("SyntaxError|SyntaxError", result.ToString());
    }

    [Fact]
    public void BigInt_Invalid_String_Syntax_Throws_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              (() => { try { BigInt('10n'); return 'no error'; } catch (e) { return e.name; } })(),
              (() => { try { BigInt('-0x1'); return 'no error'; } catch (e) { return e.name; } })(),
              (() => { try { BigInt('0b'); return 'no error'; } catch (e) { return e.name; } })()
            ].join('|');
            """);

        Assert.Equal("SyntaxError|SyntaxError|SyntaxError", result.ToString());
    }

    [Fact]
    public void Uint8Array_Base64_Alphabet_Mismatches_Throw_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.Uint8ArrayBase64);

        var result = ctx.Eval("""
            [
              (() => { try { Uint8Array.fromBase64('x+/y', { alphabet: 'base64url' }); return 'no error'; } catch (e) { return e.name; } })(),
              (() => {
                var value = Uint8Array.fromBase64('x-_y', { alphabet: 'base64url' });
                return [value[0], value[1], value[2]].join(',');
              })(),
              (() => {
                try {
                  new Uint8Array([255, 255, 255, 255]).setFromBase64('x+/y', { alphabet: 'base64url' });
                  return 'no error';
                } catch (e) {
                  return e.name;
                }
              })(),
              (() => {
                var target = new Uint8Array([255, 255, 255, 255]);
                var read = target.setFromBase64('x-_y', { alphabet: 'base64url' });
                return [read.read, read.written, target[0], target[1], target[2], target[3]].join(',');
              })()
            ].join('|');
            """);

        Assert.Equal("SyntaxError|199,239,242|SyntaxError|4,3,199,239,242,255", result.ToString());
    }

    [Fact]
    public void GlobalThis_Resolves_To_The_Current_Global_Object()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("globalThis === this;");

        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void With_Statement_Unqualified_Identifiers_Resolve_And_Assign_Against_The_With_Object()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var p1 = 'x1';
                var target = { p1: 1 };

                with (target) {
                    var before = p1;
                    p1 = 2;
                    return before + '|' + p1 + '|' + target.p1 + '|' + this.p1;
                }
            })();
            """);

        Assert.Equal("1|2|2|x1", result.ToString());
    }

    [Fact]
    public void With_Statement_Strict_Read_And_Write_Respect_Unscopables_Deletion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function capture(action) {
                try {
                  action();
                  return "no throw";
                } catch (e) {
                  return e.name + "|" + e.message;
                }
              }

              var readCalls = 0;
              var readEnv = {
                binding: 0,
                get [Symbol.unscopables]() {
                  readCalls++;
                  delete readEnv.binding;
                  return null;
                }
              };

              var writeCalls = 0;
              var writeEnv = {
                binding: 0,
                get [Symbol.unscopables]() {
                  writeCalls++;
                  delete writeEnv.binding;
                  return null;
                }
              };

              var readResult;
              with (readEnv) {
                readResult = capture(function () {
                  "use strict";
                  return binding;
                });
              }

              var writeResult;
              with (writeEnv) {
                writeResult = capture(function () {
                  "use strict";
                  binding = 123;
                });
              }

              return [
                readResult,
                writeResult,
                readCalls,
                writeCalls,
                String(Object.getOwnPropertyDescriptor(readEnv, "binding")),
                String(Object.getOwnPropertyDescriptor(writeEnv, "binding"))
              ].join("||");
            })();
            """);

        Assert.Equal(
            "ReferenceError|binding is not defined||ReferenceError|binding is not defined||1||1||undefined||undefined",
            result.ToString());
    }

    [Fact]
    public void Bare_Function_Calls_And_Implicit_Global_Assignments_Use_NonStrict_Global_Semantics()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"[
            (function () { return this === globalThis; })(),
            (function () { implicitGlobalValue = 1; return globalThis.implicitGlobalValue === 1; })(),
            (function () { var declared = 1; return [delete declared, delete globalThis.declared].join('|'); })()
        ].join('|');");

        Assert.Equal("true|true|false|true", result.ToString());
        ctx.Eval("delete globalThis.implicitGlobalValue;");
    }

    [Fact]
    public async Task ForAwait_Over_Sync_And_AsyncIterator_Facades_Awaits_Each_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var syncResult = ctx.Execute(@"
            async function run() {
                var values = [];
                for await (var value of [Promise.resolve('a'), Promise.resolve('b')]) {
                    values.push(value);
                }
                return values.join('|');
            }

            run();
        ");

        Assert.Equal("a|b", syncResult.ToString());

        var asyncFacadeResult = ctx.Execute(@"
            async function run() {
                var values = [];
                var iterable = {
                    [Symbol.asyncIterator]() {
                        return [Promise.resolve('x'), Promise.resolve('y')][Symbol.iterator]();
                    }
                };

                for await (var value of iterable) {
                    values.push(value);
                }

                return values.join('|');
            }

            run();
        ");

        Assert.Equal("x|y", asyncFacadeResult.ToString());
    }

    [Fact]
    public async Task AsyncGenerator_YieldStar_Prefers_SymbolAsyncIterator_Over_SymbolIterator()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Execute(@"
            async function run() {
                class Test262Error extends Error {}

                async function* delegated() {
                    yield* {
                        get [Symbol.iterator]() {
                            throw new Test262Error('it should not get Symbol.iterator');
                        },
                        [Symbol.asyncIterator]() {
                            return [Promise.resolve('x'), Promise.resolve('y')][Symbol.iterator]();
                        }
                    };
                }

                var values = [];
                for await (var value of delegated()) {
                    values.push(value);
                }

                return values.join('|');
            }

            run();
        ");

        Assert.Equal("x|y", result.ToString());
    }

    [Fact]
    public void Object_Symbol_Wrapper_Uses_Symbol_Coercion_Path()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            (function () {
                try {
                    return Object(Symbol('x')) + '';
                } catch (e) {
                    return [
                        typeof Object(Symbol('x')),
                        e.constructor && e.constructor.name,
                        e.message
                    ].join('|');
                }
            })();
        ");

        Assert.Equal("object|TypeError|Cannot convert a Symbol value to a string.", result.ToString());
    }

    [Fact]
    public void Prefixed_BigInt_Literals_Parse_And_Compare_Correctly()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            [
                typeof 0x1n, 0x1n === 1n,
                typeof 0b1n, 0b1n === 1n,
                typeof 0o1n, 0o1n === 1n
            ].join('|');
        ");

        Assert.Equal("bigint|true|bigint|true|bigint|true", result.ToString());
    }

    [Fact]
    public void BigInt_Relational_Comparisons_Work_For_BigInt_And_Number_Operands()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            [
                10n < 2n,
                10n > 2n,
                10n <= 10n,
                10n >= 10n,
                1n < 2,
                2 > 1n,
                1n < 2.5,
                2.5 > 1n,
                1n < Number.NaN,
                Number.NaN < 1n
            ].join('|');
        ");

        Assert.Equal("false|true|true|true|true|true|true|true|false|false", result.ToString());
    }

    [Fact]
    public void BigInt_Relational_Comparisons_Respect_Equality_Precedence()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"
            [
                1n < 2 === true,
                1n === 1n < 2,
                1n < 2n === true,
                1n === 1n < 2n
            ].join('|');
        ");

        Assert.Equal("true|false|true|false", result.ToString());
    }

    [Fact]
    public void String_IsWellFormed_Detects_Paired_And_Lone_Surrogates()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            [
                'plain'.isWellFormed(),
                '\uD83C\uDF89'.isWellFormed(),
                '\uD800'.isWellFormed(),
                '\uDC00'.isWellFormed()
            ].join('|');
        ");
        Assert.Equal("true|true|false|false", result.ToString());
    }

    [Fact]
    public void String_ToWellFormed_Replaces_Lone_Surrogates_With_Replacement_Character()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            [
                '\uD800A\uDC00'.toWellFormed(),
                '\uD83C\uDF89'.toWellFormed()
            ].join('|');
        ");
        Assert.Equal("\uFFFDA\uFFFD|🎉", result.ToString());
    }

    [Fact]
    public void String_ToWellFormed_Always_Produces_A_WellFormed_String()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"'\uD800'.toWellFormed().isWellFormed();");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Finally_Return_Overrides_Try_And_Catch_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            [
                (function() { try { return 'try'; } finally { return 'finally'; } })(),
                (function() { try { throw new Error('boom'); } catch (e) { return 'catch'; } finally { return 'finally'; } })()
            ].join('|');
        ");
        Assert.Equal("finally|finally", result.ToString());
    }

    [Fact]
    public void Finally_Return_Skips_Remaining_Finally_Statements()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function() {
                var order = [];
                try {
                    order.push('try');
                    return 'try';
                } finally {
                    order.push('finally-before');
                    return order.join(',');
                    order.push('finally-after');
                }
            })();
        ");
        Assert.Equal("try,finally-before", result.ToString());
    }

    [Fact]
    public void Finally_Normal_Completion_Preserves_Pending_Return_Value()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function() {
                try {
                    return 'try';
                } finally {
                    if (false) {
                        'unused';
                    } else {
                        true;
                    }
                }
            })();
        ");
        Assert.Equal("try", result.ToString());
    }

    [Fact]
    public void RegExp_V_Flag_Exposes_UnicodeSets_Metadata()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var re = new RegExp('a', 'v');
            [re.flags, re.unicodeSets, re.unicode].join('|');
        ");
        Assert.Equal("v|true|false", result.ToString());
    }

    [Fact]
    public void RegExp_V_Flag_Works_For_Literals()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var re = /a/v;
            [re.flags, re.unicodeSets, re.test('a')].join('|');
        ");
        Assert.Equal("v|true|true", result.ToString());
    }

    [Fact]
    public void RegExp_V_Flag_Cannot_Be_Combined_With_U()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval("new RegExp('a', 'uv');"));
        Assert.Throws<JSException>(() => ctx.Eval("/a/vu;"));
    }

    [Fact]
    public void RegExp_V_Flag_Cannot_Be_Specified_Twice()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval("new RegExp('a', 'vv');"));
        Assert.Throws<JSException>(() => ctx.Eval("/a/vv;"));
    }

    [Fact]
    public void RegExp_Flags_Are_Normalized_And_Metadata_Is_Exposed()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var re = new RegExp('a', 'yidg');
            [re.flags, re.hasIndices, re.sticky, re.global, re.ignoreCase].join('|');
        ");
        Assert.Equal("dgiy|true|true|true|true", result.ToString());
    }

    [Fact]
    public void RegExp_Prototype_Flags_Getter_Supports_Generic_HasIndices_Coercion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var get = Object.getOwnPropertyDescriptor(RegExp.prototype, "flags").get;
                return get.call({ hasIndices: Symbol() });
            })();
            """);
        Assert.Equal("d", result.ToString());
    }

    [Fact]
    public void RegExp_D_And_Y_Flags_Cannot_Be_Specified_Twice()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval("new RegExp('a', 'dd');"));
        Assert.Throws<JSException>(() => ctx.Eval("new RegExp('a', 'yy');"));
    }

    [Fact]
    public void RegExp_Literal_Duplicate_D_And_Y_Flags_Are_Rejected()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval("/a/dd"));
        Assert.Throws<JSException>(() => ctx.Eval("/a/yy"));
    }

    [Fact]
    public void RegExp_Sticky_Exec_Uses_LastIndex_And_Resets_On_Failure()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var re = /a/y;
            re.lastIndex = 1;
            var first = re.exec('ba');
            var afterFirst = re.lastIndex;
            var second = re.exec('ba');
            [
                first[0],
                first.index,
                afterFirst,
                second === null,
                re.lastIndex
            ].join('|');
        ");
        Assert.Equal("a|1|2|true|0", result.ToString());
    }

    [Fact]
    public void RegExp_Sticky_Test_Does_Not_Scan_Past_LastIndex()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var re = /a/y;
            var first = re.test('ba');
            var afterFirst = re.lastIndex;
            re.lastIndex = 1;
            var second = re.test('ba');
            var afterSecond = re.lastIndex;
            var third = re.test('ba');
            [first, afterFirst, second, afterSecond, third, re.lastIndex].join('|');
        ");
        Assert.Equal("false|0|true|2|false|0", result.ToString());
    }

    [Fact]
    public void RegExp_Observable_LastIndex_Writes_Throw_For_Exec_And_Match()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    (function () {
                        var re = /b/y;
                        Object.defineProperty(re, 'lastIndex', { value: 1, writable: false });
                        return thrownCtor(function () {
                            re.exec('ab');
                        });
                    })(),
                    (function () {
                        var re = /a/g;
                        Object.defineProperty(re, 'lastIndex', { value: 0, writable: false });
                        return thrownCtor(function () {
                            'a'.match(re);
                        });
                    })()
                ].join('|');
            })();
        ");

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void RegExp_Exec_And_Iterator_Close_Read_Observable_Properties_Exactly_Once()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                var lastIndexReads = 0;
                var re = /./;
                re.lastIndex = {
                    valueOf: function () {
                        lastIndexReads++;
                        return 0;
                    }
                };
                re.exec('abc');

                var returnGets = 0;
                var iterable = {
                    next: function () {
                        return { value: 1, done: false };
                    },
                    get return() {
                        returnGets++;
                        return null;
                    }
                };
                iterable[Symbol.iterator] = function () {
                    return iterable;
                };

                function* generator() {
                    yield* iterable;
                }

                var iterator = generator();
                iterator.next();
                var completion = iterator.return(2);

                return [
                    lastIndexReads,
                    completion.value,
                    completion.done,
                    returnGets
                ].join('|');
            })();
            """);

        Assert.Equal("1|2|true|1", result.ToString());
    }

    [Fact]
    public void Generator_YieldStar_MissingThrow_Closes_Iterator_Without_Arguments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                var throwGets = 0;
                var returnCount = 0;
                var returnArgsLength = -1;
                var iterable = {
                    next: function() {
                        return { value: 1, done: false };
                    },
                    get throw() {
                        throwGets += 1;
                        return null;
                    },
                    return: function(...args) {
                        returnCount += 1;
                        returnArgsLength = args.length;
                        return { done: true };
                    }
                };

                iterable[Symbol.iterator] = function() {
                    return iterable;
                };

                function* generator() {
                    yield* iterable;
                }

                var iterator = generator();
                iterator.next();

                try {
                    iterator.throw('boom');
                    return 'no-throw';
                } catch (e) {
                    return [e.constructor.name, throwGets, returnCount, returnArgsLength].join('|');
                }
            })();
            """);

        Assert.Equal("TypeError|1|1|0", result.ToString());
    }

    [Fact]
    public void Iterator_FlatMap_Return_Forwards_To_Mapper_Result_Once()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                var returnCount = 0;

                function* source() {
                    yield 0;
                }

                var iter = source().flatMap(function () {
                    return {
                        next: function () {
                            return { done: false, value: 1 };
                        },
                        return: function () {
                            returnCount += 1;
                            return {};
                        }
                    };
                });

                iter.next();
                iter.return();
                iter.return();

                return String(returnCount);
            })();
            """);

        Assert.Equal("1", result.ToString());
    }

    [Fact]
    public void JSON_Stringify_Passes_Property_Key_To_ToJSON()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                var argsLength = -1;
                var receivedKey = 'missing';
                var obj = {
                    p: {
                        toJSON: function (...args) {
                            argsLength = args.length;
                            receivedKey = args[0];
                            return 17;
                        }
                    }
                };

                return [JSON.stringify(obj), argsLength, receivedKey].join('|');
            })();
            """);

        Assert.Equal("{\"p\":17}|1|p", result.ToString());
    }

    [Fact]
    public void MatchAll_RegExp_LastIndex_And_SetLike_Iterator_Return_Regressions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                var callCount = 0;
                var arg;
                var receiver = {
                  [Symbol.toPrimitive]: function () {
                    callCount++;
                    return 'abc';
                  }
                };
                RegExp.prototype[Symbol.matchAll] = function (string) {
                  arg = string;
                };

                String.prototype.matchAll.call(receiver, null);

                var lastIndexReads = 0;
                var r = /a/g;
                r.lastIndex = {
                  valueOf: function () {
                    lastIndexReads++;
                    return -1;
                  }
                };
                r.exec('nbc');

                var iter = {
                  a: [4, 5, 6],
                  nextCalls: 0,
                  returnCalls: 0,
                  next() {
                    var done = this.nextCalls >= this.a.length;
                    var value = this.a[this.nextCalls];
                    this.nextCalls++;
                    return { done: done, value: value };
                  },
                  return() {
                    this.returnCalls++;
                    return this;
                  }
                };
                var setlike = {
                  size: iter.a.length,
                  has(v) { return iter.a.includes(v); },
                  keys() { return iter; }
                };

                var disjoint = new Set([4, 5, 6, 7]).isDisjointFrom(setlike);
                var disjointSnapshot = iter.nextCalls + ',' + iter.returnCalls;
                iter.nextCalls = iter.returnCalls = 0;
                var superset = new Set([4, 5, 6, 7]).isSupersetOf(setlike);
                var supersetSnapshot = iter.nextCalls + ',' + iter.returnCalls;

                return [
                  callCount,
                  arg,
                  r.lastIndex,
                  lastIndexReads,
                  disjoint,
                  disjointSnapshot,
                  superset,
                  supersetSnapshot
                ].join('|');
            })();
            """);

        Assert.Equal("1|abc|0|1|false|1,1|true|4,0", result.ToString());
    }

    [Fact]
    public void RegExp_Exec_Returns_Undefined_For_Unmatched_Optional_Captures()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var match = /(a)?b/.exec('b');
            [
                match[0],
                match[1] === undefined,
                match.index,
                match.input
            ].join('|');
        ");
        Assert.Equal("b|true|0|b", result.ToString());
    }

    [Fact]
    public void Iterator_From_Map_Filter_Take_ToArray()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("Iterator.from([1,2,3,4]).map(v => v * 2).filter(v => v > 4).take(2).toArray().join(',');");
        Assert.Equal("6,8", result.ToString());
    }

    [Fact]
    public void Iterator_Concat_Reduce_Across_Iterables()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.IteratorConcat);
        var result = ctx.Eval("Iterator.concat([1,2], new Set([3,4]).values()).reduce((sum, value) => sum + value, 0);");
        Assert.Equal(10.0, result.DoubleValue);
    }

    [Fact]
    public void Generator_Instances_Inherit_Iterator_Helpers()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            function* values() { yield 1; yield 2; yield 3; }
            values().drop(1).find(v => v === 2);
        ");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void Iterator_Helper_Callbacks_Receive_Counters()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            function* letters() {
                yield 'a';
                yield 'b';
                yield 'c';
            }

            var map = letters().map((value, count) => value + count).toArray().join(',');
            var filterCounts = [];
            [...letters().filter((value, count) => { filterCounts.push(count); return true; })];
            var flatMapCounts = [];
            [...letters().flatMap((value, count) => { flatMapCounts.push(count); return [value]; })];
            var forEachCounts = [];
            letters().forEach((value, count) => forEachCounts.push(count));
            var reduceCounts = [];
            letters().reduce((memo, value, count) => { reduceCounts.push(count); return value; });
            [map, filterCounts.join(','), flatMapCounts.join(','), forEachCounts.join(','), reduceCounts.join(',')].join('|');
        ");
        Assert.Equal("a0,b1,c2|0,1,2|0,1,2|0,1,2|1,2", result.ToString());
    }

    [Fact]
    public void Iterator_FlatMap_And_Number_ToExponential_TypeErrors_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function* g() { yield 0; }
              function* h() { yield 0; yield 1; yield 2; }

              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              var fallback = Array.from(g().flatMap(function () {
                var n = h();
                return {
                  [Symbol.iterator]: null,
                  next: function () { return n.next(); }
                };
              })).join(',');

              return [
                thrownCtor(function () {
                  for (var unused of g().flatMap(function () { return 'string'; })) { }
                }),
                thrownCtor(function () {
                  var iter = g().flatMap(function () {
                    var n = h();
                    return {
                      [Symbol.iterator]: 0,
                      next: function () { return n.next(); }
                    };
                  });
                  iter.next();
                }),
                fallback,
                thrownCtor(function () { NaN.toExponential(Symbol('1')); })
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|0,1,2|TypeError", result.ToString());
    }

    // ── M3: JSWeakSet tests ──────────────────────────────────────────

    [Fact]
    public void WeakSet_Add_And_Has()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        // WeakSet.add returns the WeakSet itself per spec
        var result = ctx.Eval("var ws = new WeakSet(); var o = {}; typeof ws.add(o);");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void WeakSet_Delete()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("var ws = new WeakSet(); var o = {}; ws.add(o); ws['delete'](o);");
        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void WeakSet_TypeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    (function () {
                        var original = WeakSet.prototype.add;
                        try {
                            Object.defineProperty(WeakSet.prototype, 'add', { value: null, configurable: true });
                            return thrownCtor(function () {
                                new WeakSet([]);
                            });
                        } finally {
                            Object.defineProperty(WeakSet.prototype, 'add', { value: original, configurable: true });
                        }
                    })(),
                    thrownCtor(function () {
                        new WeakSet({});
                    })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    // ── M3: StructuredClone with Map/Set ─────────────────────────────

    [Fact]
    public void StructuredClone_Map()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(@"
            var m = new Map();
            m.set('a', 1);
            var clone = structuredClone(m);
            clone.get('a');
        ");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void StructuredClone_Set()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(@"
            var s = new Set([1, 2, 3]);
            var clone = structuredClone(s);
            clone.size;
        ");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void StructuredClone_Transfer_ArrayBuffer_Detaches_Source_And_Returns_Clone()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(@"
            var source = new ArrayBuffer(4);
            var sourceView = new Uint8Array(source);
            sourceView[0] = 7;
            sourceView[1] = 11;
            var clone = structuredClone(source, { transfer: [source] });
            var cloneView = new Uint8Array(clone);
            [source.detached, clone.detached, clone.byteLength, cloneView[0], cloneView[1], clone !== source].join('|');
        ");
        Assert.Equal("true|false|4|7|11|true", result.ToString());
    }

    [Fact]
    public void StructuredClone_TypedArray_Preserves_View_And_Copies_Buffer()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(@"
            var source = new ArrayBuffer(6);
            var bytes = new Uint8Array(source);
            bytes[2] = 7;
            bytes[3] = 11;
            bytes[4] = 13;
            var sourceView = new Uint8Array(source, 2, 3);
            var clone = structuredClone(sourceView);
            bytes[2] = 99;
            [
              clone instanceof Uint8Array,
              clone.buffer !== source,
              clone.byteOffset,
              clone.length,
              clone[0],
              clone[2]
            ].join('|');
        ");
        Assert.Equal("true|true|2|3|7|13", result.ToString());
    }

    [Fact]
    public void StructuredClone_Transfer_Reuses_Transferred_Buffer_For_TypedArray_And_DataView()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        var result = ctx.Eval(@"
            var source = new ArrayBuffer(6);
            var bytes = new Uint8Array(source);
            bytes.set([1, 2, 3, 4, 5, 6]);
            var payload = {
              buffer: source,
              typed: new Uint8Array(source, 1, 3),
              view: new DataView(source, 2, 2)
            };
            var clone = structuredClone(payload, { transfer: [source] });
            [
              source.detached,
              clone.buffer !== source,
              clone.typed instanceof Uint8Array,
              clone.typed.buffer === clone.buffer,
              clone.typed.byteOffset,
              clone.typed.length,
              clone.typed[0],
              clone.typed[2],
              clone.view instanceof DataView,
              clone.view.buffer === clone.buffer,
              clone.view.byteOffset,
              clone.view.byteLength,
              clone.view.getUint8(0),
              clone.view.getUint8(1)
            ].join('|');
        ");
        Assert.Equal("true|true|true|true|1|3|2|4|true|true|2|2|3|4", result.ToString());
    }

    [Fact]
    public void StructuredClone_Transfer_Rejects_NonArrayBuffer_Entries()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        Assert.Throws<JSException>(() => ctx.Eval(@"
            structuredClone({ ok: true }, { transfer: [{}] });
        "));
    }

    [Fact]
    public void StructuredClone_Transfer_Rejects_Duplicate_ArrayBuffers()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone);
        Assert.Throws<JSException>(() => ctx.Eval(@"
            var source = new ArrayBuffer(4);
            structuredClone(source, { transfer: [source, source] });
        "));
    }

    [Fact]
    public void ExperimentalEs2026Features_CanBeDisabled_PerContext()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              typeof structuredClone,
              typeof Math.sumPrecise,
              typeof Uint8Array.fromBase64,
              typeof Uint8Array.prototype.toBase64,
              typeof Map.prototype.getOrInsert,
              typeof WeakMap.prototype.getOrInsert
            ].join('|');
            """);
        Assert.Equal("undefined|undefined|undefined|undefined|undefined|undefined", result.ToString());
    }

    [Fact]
    public void ExperimentalEs2026Features_CanBeEnabled_Selectively()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.StructuredClone | JavaScriptFeatureFlags.MapUpsert);
        var result = ctx.Eval("""
            [
              typeof structuredClone,
              typeof Math.sumPrecise,
              typeof Map.prototype.getOrInsert,
              typeof Uint8Array.fromBase64
            ].join('|');
            """);
        Assert.Equal("function|undefined|function|undefined", result.ToString());
    }

    /// <summary>
    /// Forces the BuiltIns and Clr assemblies to load by referencing types from them,
    /// which triggers their ModuleInitializers.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public void JSBoolean_TrueAndFalse()
    {
        Assert.True(JSBoolean.True.BooleanValue);
        Assert.False(JSBoolean.False.BooleanValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public void Object_Boolean_Wrapping_Produces_Correct_Prototype()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        // Object(true) must produce an object whose [[Prototype]] is Boolean.prototype
        var result = ctx.Eval(@"[
            Object(true) instanceof Boolean,
            Object(false) instanceof Boolean,
            Object.getPrototypeOf(Object(true)) === Boolean.prototype,
            new Boolean(true) instanceof Boolean,
            new Boolean(false) instanceof Boolean,
            // Lenient-mode bind(true) should box to Boolean wrapper
            (function lenient() { return this; }).bind(true)() instanceof Boolean
        ]");
        var arr = (JSArray)result;
        for (int i = 0; i < 6; i++)
            Assert.True(arr[(uint)i].BooleanValue, $"assertion {i} failed");
    }

    [Fact]
    public void Array_Prototype_TypeError_Scenarios_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"[
            (function () {
                try {
                    Array.prototype.copyWithin.call(undefined, 0, 0);
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })(),
            (function () {
                var o = { length: 43 };
                Object.defineProperty(o, '42', { configurable: false, writable: true });

                try {
                    Array.prototype.copyWithin.call(o, 42, 0);
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })(),
            (function () {
                try {
                    Array.prototype.entries.call(null);
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })(),
            (function () {
                var accessed = false;
                var arr = [];

                Object.defineProperty(arr, '0', {
                    get: function() {
                        throw new TypeError('boom');
                    },
                    configurable: true
                });
                Object.defineProperty(arr, '1', {
                    get: function() {
                        accessed = true;
                        return true;
                    },
                    configurable: true
                });

                try {
                    arr.indexOf(true);
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name + '|' + accessed;
                }
            })()
        ].join('||');");

        Assert.Equal("TypeError||TypeError||TypeError||TypeError|false", result.ToString());
    }

    [Fact]
    public void Array_Prototype_TypeError_Regressions_Cover_ArrayLike_And_Species_Scenarios()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            function nonExtensibleSpecies() {
                var target = [];
                Object.preventExtensions(target);
                return function() { return target; };
            }

            function nonConfigurableSpecies() {
                var target = [];
                Object.defineProperty(target, '0', {
                    value: 0,
                    writable: true,
                    configurable: false,
                    enumerable: true
                });
                return function() { return target; };
            }

            function withSpecies(factory) {
                var source = [1];
                source.constructor = {};
                source.constructor[Symbol.species] = factory();
                return source;
            }

            var lengthThrows = {};
            Object.defineProperty(lengthThrows, 'length', {
                get: function() { throw new RangeError('length'); },
                configurable: true
            });

            return [
                thrownCtor(function () { Array.prototype.fill.call(undefined, 0); }),
                thrownCtor(function () { Array.prototype.forEach.call(lengthThrows, undefined); }),
                thrownCtor(function () { Array.prototype.includes.call({ length: Symbol('x') }, 0); }),
                thrownCtor(function () { Array.prototype.indexOf.call(lengthThrows, 0); }),
                thrownCtor(function () { Array.prototype.keys.call(null); }),
                thrownCtor(function () { Array.prototype.lastIndexOf.call(lengthThrows, 0); }),
                thrownCtor(function () { withSpecies(nonExtensibleSpecies).filter(function () { return true; }); }),
                thrownCtor(function () { withSpecies(nonConfigurableSpecies).filter(function () { return true; }); }),
                thrownCtor(function () { withSpecies(nonExtensibleSpecies).flat(); }),
                thrownCtor(function () { withSpecies(nonConfigurableSpecies).flat(); }),
                thrownCtor(function () { withSpecies(nonExtensibleSpecies).flatMap(function (x) { return [x]; }); }),
                thrownCtor(function () { withSpecies(nonConfigurableSpecies).flatMap(function (x) { return [x]; }); })
            ].join('|');
        })();");

        Assert.Equal(
            "TypeError|RangeError|TypeError|RangeError|TypeError|RangeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void Array_Prototype_Length_Coercion_And_Modification_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    return fn();
                } catch (e) {
                    return e.constructor.name;
                }
            }

            function nonPrimitiveNumberLike() {
                return {
                    valueOf: function() { return {}; },
                    toString: function() { return {}; }
                };
            }

            var revoked = Proxy.revocable([], {});
            revoked.revoke();

            function SpeciesCtor(mode) {
                if (mode === 'nonExtensible') {
                    this.length = 0;
                    Object.preventExtensions(this);
                    return;
                }

                Object.defineProperty(this, '0', {
                    set: function(_value) {},
                    configurable: false
                });
            }

            var speciesSource = [1];
            speciesSource.constructor = {};
            speciesSource.constructor[Symbol.species] = function() {
                return new SpeciesCtor('nonExtensible');
            };

            var lockedSpeciesSource = [1];
            lockedSpeciesSource.constructor = {};
            lockedSpeciesSource.constructor[Symbol.species] = function() {
                return new SpeciesCtor('lockedProperty');
            };

            return [
                thrownCtor(function() { Array.prototype.every.call({ length: nonPrimitiveNumberLike() }, function() { return true; }); }),
                thrownCtor(function() { Array.prototype.filter.call({ length: nonPrimitiveNumberLike() }, function() { return true; }); }),
                thrownCtor(function() { Array.prototype.forEach.call({ length: nonPrimitiveNumberLike() }, function() {}); }),
                thrownCtor(function() { Array.prototype.indexOf.call({ length: nonPrimitiveNumberLike() }, 1); }),
                thrownCtor(function() { [0, true].indexOf(true, nonPrimitiveNumberLike()); }),
                thrownCtor(function() { Array.prototype.lastIndexOf.call({ length: nonPrimitiveNumberLike() }, 1); }),
                thrownCtor(function() { [0, true].lastIndexOf(true, nonPrimitiveNumberLike()); }),
                thrownCtor(function() { Array.prototype.map.call({ length: nonPrimitiveNumberLike() }, function() { return 1; }); }),
                thrownCtor(function() { Array.prototype.map.call(revoked.proxy, function() { return 1; }); }),
                thrownCtor(function() { speciesSource.map(function() { return 1; }); }),
                thrownCtor(function() { lockedSpeciesSource.map(function() { return 1; }); }),
                (function() {
                    var array = [];
                    var setterCalls = 0;
                    Object.defineProperty(Array.prototype, '0', {
                        configurable: true,
                        set: function(_value) {
                            Object.defineProperty(array, 'length', { writable: false });
                            setterCalls++;
                        }
                    });

                    try {
                        return [
                            thrownCtor(function() { array.push(1); }),
                            [Object.prototype.hasOwnProperty.call(array, '0'), setterCalls].join(',')
                        ].join('|');
                    } finally {
                        delete Array.prototype[0];
                    }
                })(),
                (function() {
                    var array = new Array(1);
                    var getterCalls = 0;
                    Object.defineProperty(Array.prototype, '0', {
                        configurable: true,
                        get: function() {
                            Object.defineProperty(array, 'length', { writable: false });
                            getterCalls++;
                        }
                    });

                    try {
                        return [
                            thrownCtor(function() { array.pop(); }),
                            getterCalls
                        ].join('|');
                    } finally {
                        delete Array.prototype[0];
                    }
                })(),
                thrownCtor(function() { Object.freeze([]).pop(); }),
                thrownCtor(function() {
                    var array = [];
                    Object.defineProperty(array, 'length', { writable: false });
                    return array.pop();
                }),
                thrownCtor(function() {
                    var arrayLike = { length: Number.MAX_SAFE_INTEGER - 3 };
                    Object.defineProperty(arrayLike, Number.MAX_SAFE_INTEGER - 1, {
                        value: 33,
                        writable: false,
                        enumerable: true,
                        configurable: true
                    });
                    Array.prototype.push.call(arrayLike, 1, 2, 3);
                })
            ].join('|');
        })();");

        Assert.Equal(
            "TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|false,1|TypeError|1|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void Array_Prototype_TypeError_Regressions_For_Length_Species_And_Reduction_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    return fn();
                } catch (e) {
                    return e.constructor.name;
                }
            }

            function nonPrimitiveNumberLike() {
                return {
                    valueOf: function() { return {}; },
                    toString: function() { return {}; }
                };
            }

            return [
                (function() {
                    var array = [];
                    Object.defineProperty(array, 'length', { writable: false });

                    return [
                        thrownCtor(function() { array.push(1); }),
                        Object.prototype.hasOwnProperty.call(array, '0')
                    ].join('|');
                })(),
                thrownCtor(function() {
                    var array = [];
                    Object.defineProperty(array, 'length', { writable: false });
                    return array.shift();
                }),
                (function() {
                    var array = new Array(1);
                    var getterCalls = 0;
                    Object.defineProperty(Array.prototype, '0', {
                        configurable: true,
                        get: function() {
                            Object.defineProperty(array, 'length', { writable: false });
                            getterCalls++;
                        }
                    });

                    try {
                        return [
                            thrownCtor(function() { array.shift(); }),
                            array.length,
                            getterCalls
                        ].join('|');
                    } finally {
                        delete Array.prototype[0];
                    }
                })(),
                thrownCtor(function() { Array.prototype.some.call({ 0: 11, length: nonPrimitiveNumberLike() }, function() { return true; }); }),
                thrownCtor(function() { Array.prototype.reduce.call({ 0: 11, length: nonPrimitiveNumberLike() }, function() { return true; }, 1); }),
                thrownCtor(function() { Array.prototype.reduceRight.call({ 0: 11, length: nonPrimitiveNumberLike() }, function() { return true; }, 1); }),
                thrownCtor(function() {
                    var source = [1];
                    source.constructor = {};
                    source.constructor[Symbol.species] = function() {
                        this.length = 0;
                        Object.preventExtensions(this);
                    };
                    return source.slice(0, 1);
                }),
                thrownCtor(function() {
                    var source = [1];
                    source.constructor = {};
                    source.constructor[Symbol.species] = function() {
                        Object.defineProperty(this, '0', {
                            set: function(_value) {},
                            configurable: false
                        });
                    };
                    return source.slice(0, 1);
                }),
                thrownCtor(function() {
                    var revoked = Proxy.revocable([], {});
                    revoked.revoke();
                    return Array.prototype.slice.call(revoked.proxy);
                }),
                thrownCtor(function() {
                    var revoked = Proxy.revocable([], {});
                    revoked.revoke();
                    return Array.prototype.splice.call(revoked.proxy);
                })
            ].join('|');
        })();");

        Assert.Equal(
            "TypeError|false|TypeError|TypeError|1|1|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void Array_Prototype_Invalid_Species_Constructors_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e && e.constructor ? e.constructor.name : typeof e;
                    }
                }

                function invalidConstructorResult(method) {
                    var sample = [1];
                    sample.constructor = null;
                    return thrownCtor(function () { method(sample); });
                }

                function invalidSpeciesResult(method) {
                    var sample = [1];
                    sample.constructor = {};
                    sample.constructor[Symbol.species] = parseInt;
                    return thrownCtor(function () { method(sample); });
                }

                return [
                    invalidConstructorResult(function (sample) { sample.filter(function () { return true; }); }),
                    invalidConstructorResult(function (sample) { sample.flat(); }),
                    invalidConstructorResult(function (sample) { sample.flatMap(function (value) { return [value]; }); }),
                    invalidSpeciesResult(function (sample) { sample.filter(function () { return true; }); }),
                    invalidSpeciesResult(function (sample) { sample.flat(); }),
                    invalidSpeciesResult(function (sample) { sample.flatMap(function (value) { return [value]; }); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void TypeError_Regressions_For_Array_ArrayBuffer_Boolean_And_Async_Functions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e && e.constructor ? e.constructor.name : typeof e;
                }
            }

            return [
                thrownCtor(function() { Array.prototype.toReversed.call(null); }),
                thrownCtor(function() { Array.prototype.toReversed.call(undefined); }),
                (function() {
                    var array = [];
                    var calls = 0;
                    Object.defineProperty(Array.prototype, '0', {
                        configurable: true,
                        set: function(_value) {
                            Object.freeze(array);
                            calls++;
                        }
                    });

                    try {
                        return [
                            thrownCtor(function() { array.unshift(1); }),
                            Object.prototype.hasOwnProperty.call(array, '0'),
                            array.length,
                            calls
                        ].join(',');
                    } finally {
                        delete Array.prototype[0];
                    }
                })(),
                (function() {
                    var array = [];
                    var calls = 0;
                    Object.defineProperty(Array.prototype, '0', {
                        configurable: true,
                        set: function(_value) {
                            Object.defineProperty(array, 'length', { writable: false });
                            calls++;
                        }
                    });

                    try {
                        return [
                            thrownCtor(function() { array.unshift(1); }),
                            Object.prototype.hasOwnProperty.call(array, '0'),
                            array.length,
                            calls
                        ].join(',');
                    } finally {
                        delete Array.prototype[0];
                    }
                })(),
                thrownCtor(function() {
                    var speciesConstructor = {};
                    speciesConstructor[Symbol.species] = function() { return {}; };
                    var arrayBuffer = new ArrayBuffer(8);
                    arrayBuffer.constructor = speciesConstructor;
                    arrayBuffer.slice();
                }),
                thrownCtor(function() {
                    var speciesConstructor = {};
                    var arrayBuffer = new ArrayBuffer(8);
                    speciesConstructor[Symbol.species] = function() { return arrayBuffer; };
                    arrayBuffer.constructor = speciesConstructor;
                    arrayBuffer.slice();
                }),
                thrownCtor(function() {
                    var speciesConstructor = {};
                    speciesConstructor[Symbol.species] = function() { return new ArrayBuffer(4); };
                    var arrayBuffer = new ArrayBuffer(8);
                    arrayBuffer.constructor = speciesConstructor;
                    arrayBuffer.slice();
                }),
                (function() {
                    var log = [];
                    var newLength = {
                        toString: function() {
                            log.push('toString');
                            return {};
                        },
                        valueOf: function() {
                            log.push('valueOf');
                            return {};
                        }
                    };
                    var arrayBuffer = new ArrayBuffer(0);
                    return [thrownCtor(function() { arrayBuffer.transfer(newLength); }), log.join(',')].join(',');
                })(),
                (function() {
                    var log = [];
                    var newLength = {
                        toString: function() {
                            log.push('toString');
                            return {};
                        },
                        valueOf: function() {
                            log.push('valueOf');
                            return {};
                        }
                    };
                    var arrayBuffer = new ArrayBuffer(0);
                    return [thrownCtor(function() { arrayBuffer.transferToFixedLength(newLength); }), log.join(',')].join(',');
                })(),
                thrownCtor(function() {
                    var boxed = new String();
                    boxed.toString = Boolean.prototype.toString;
                    boxed.toString();
                }),
                thrownCtor(function() {
                    var boxed = new String();
                    boxed.valueOf = Boolean.prototype.valueOf;
                    boxed.valueOf();
                }),
                thrownCtor(function() {
                    async function foo() {}
                    new foo();
                }),
                thrownCtor(function() {
                    var AsyncGeneratorFunction = Object.getPrototypeOf(async function* () {}).constructor;
                    var instance = AsyncGeneratorFunction();
                    new instance();
                })
            ].join('|');
        })();");

        Assert.Equal(
            "TypeError|TypeError|TypeError,false,0,1|TypeError,false,0,1|TypeError|TypeError|TypeError|TypeError,valueOf,toString|TypeError,valueOf,toString|TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void BigInt_TypedArray_TypeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e && e.constructor ? e.constructor.name : typeof e;
                    }
                }

                return [
                    thrownCtor(function () { new BigInt64Array(Symbol('1')); }),
                    thrownCtor(function () { new BigInt64Array(0).join(Symbol('')); }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(2);
                        sample.constructor = {};
                        sample.constructor[Symbol.species] = Array;
                        sample.filter(function () { return true; });
                    }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(2);
                        sample.constructor = {};
                        sample.constructor[Symbol.species] = function () { return new BigInt64Array(); };
                        sample.map(function () { return 0n; });
                    }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(0);
                        sample.set(sample, Symbol('1'));
                    }),
                    thrownCtor(function () { BigInt64Array.from.call({ m() {} }.m, []); }),
                    thrownCtor(function () { BigInt64Array.from.call(function () {}, []); }),
                    thrownCtor(function () { BigInt64Array.of.call(function () {}, 42n); }),
                    thrownCtor(function () {
                        var source = [];
                        source.length = 2;
                        source[1] = 42n;
                        BigInt64Array.from(source);
                    }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array([0n]);
                        var desc = Object.getOwnPropertyDescriptor(sample, '0');
                        Object.defineProperty(sample, '1', desc);
                    }),
                    (function () {
                        var sample = new BigInt64Array(1);
                        return [
                            delete sample['-0'],
                            thrownCtor(function () {
                                (function () {
                                    'use strict';
                                    delete sample[-0];
                                })();
                            })
                        ].join(',');
                    })(),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(1);
                        sample[0] = 1;
                    }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(1);
                        sample[0] = undefined;
                    }),
                    thrownCtor(function () {
                        var sample = new BigInt64Array(1);
                        sample[0] = null;
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|true,TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void BigInt_TypedArray_DefineOwnProperty_Propagates_ToPrimitive_Exceptions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function Test262Error() {}

                var obj = {
                    valueOf: function () {
                        throw new Test262Error();
                    }
                };

                try {
                    Object.defineProperty(new BigInt64Array([42n]), '0', { value: obj });
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })();
            """);

        Assert.Equal("Test262Error", result.ToString());
    }

    [Fact]
    public void RegExp_Prototype_Compile_TypeError_Scenarios_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            var subject = /initial/;
            Object.defineProperty(subject, 'lastIndex', { value: 45, writable: false });

            return [
                (function () { /./.compile(); return 'ok'; })(),
                thrownCtor(function () {
                    var subclassRegExp = new (class extends RegExp {})('');
                    subclassRegExp.compile();
                }),
                thrownCtor(function () {
                    var subclassRegExp = new (class extends RegExp {})('');
                    RegExp.prototype.compile.call(subclassRegExp);
                }),
                thrownCtor(function () {
                    subject.compile(/updated/gi);
                }),
                subject.source + '/' + subject.flags,
                String(subject.lastIndex)
            ].join('|');
        })();");

        Assert.Equal("ok|TypeError|TypeError|TypeError|updated/gi|45", result.ToString());
    }

    [Fact]
    public void RegExp_Unicode_CodePoint_Literal_Source_RoundTrips_For_ScriptHost_Test262_Cases()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            var source = /\u{1d306}/u.source;
            var recreated = eval('/' + source + '/u');
            return [source, recreated.test('\ud834\udf06'), recreated.test('𝌆')].join('|');
        })();");

        Assert.Equal("\\ud834\\udf06|true|true", result.ToString());
    }

    [Fact]
    public void Object_WeakRef_And_Function_TypeError_Scenarios_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"[
            (function () {
                var obj = {};
                Object.defineProperty(obj, 'prop', {
                    value: 11,
                    configurable: false
                });

                try {
                    Object.defineProperties(obj, {
                        prop: {
                            value: 12,
                            configurable: true
                        }
                    });
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })(),
            (function () {
                var array = new Array(1);
                var calls = 0;
                var proto = Object.create(Array.prototype);

                Object.defineProperty(proto, '0', {
                    configurable: true,
                    get() {
                        Object.freeze(array);
                        calls++;
                    }
                });
                Object.setPrototypeOf(array, proto);

                try {
                    array.shift();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name + '|' + array.length + '|' + calls;
                } finally {
                    try {
                        Object.setPrototypeOf(array, Array.prototype);
                    } catch (_) {
                    }
                }
            })(),
            (function () {
                try {
                    let fr = new FinalizationRegistry(() => {});
                    let token = {};
                    fr.register(token);
                    new fr.unregister(token);
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })(),
            (function () {
                function f() {
                    'use strict';
                    gNonStrict();
                }

                function gNonStrict() {
                    return gNonStrict.caller || gNonStrict.caller.throwTypeError;
                }

                try {
                    f.bind()();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })()
        ].join('||');");

        Assert.Equal("TypeError||TypeError|1|1||TypeError||TypeError", result.ToString());
    }

    [Fact]
    public void Bound_Function_Restricted_Property_Setters_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                function target() {}
                var bound = target.bind(null);

                return [
                    thrownCtor(function () { bound.caller = {}; }),
                    thrownCtor(function () { bound.arguments = {}; })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Intl_And_RegExp_TypeError_Regressions_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                const localeToString = Intl.Locale.prototype.toString;
                const pluralRules = new Intl.PluralRules();
                const regExp = /./;
                regExp.exec = function () {
                    return {
                        length: 1,
                        0: '',
                        index: 0,
                        groups: null
                    };
                };

                return [
                    thrownCtor(function () { localeToString.call(undefined); }),
                    thrownCtor(function () { localeToString.call({}); }),
                    new Intl.Locale('en-US').toString(),
                    thrownCtor(function () { pluralRules.selectRange(undefined, 201); }),
                    thrownCtor(function () { pluralRules.selectRange(102, undefined); }),
                    thrownCtor(function () { regExp[Symbol.replace]('bar', ''); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|en-US|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void CopyWithin_Date_Bind_And_Json_Parse_Preserve_Test262_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var copyWithinHas = thrownCtor(function () {
                    var proxy = new Proxy({ 0: 42, length: 1 }, {
                        has: function () {
                            throw new Test262Error();
                        }
                    });
                    Array.prototype.copyWithin.call(proxy, 0, 0);
                });

                var dateToPrimitiveGet = thrownCtor(function () {
                    var value = Object.defineProperty({}, Symbol.toPrimitive, {
                        get: function () {
                            throw new Test262Error();
                        }
                    });
                    new Date(value);
                });

                var dateToPrimitiveCall = thrownCtor(function () {
                    var value = {};
                    value[Symbol.toPrimitive] = function () {
                        throw new Test262Error();
                    };
                    new Date(value);
                });

                var copiedDate = (function () {
                    var poisonedDate = new Date(1234);
                    Object.defineProperty(poisonedDate, Symbol.toPrimitive, {
                        get: function () {
                            throw new Test262Error();
                        }
                    });
                    return String(new Date(poisonedDate).valueOf() === 1234);
                })();

                var bindName = thrownCtor(function () {
                    var target = Object.defineProperty(function () {}, 'name', {
                        get: function () {
                            throw new Test262Error();
                        }
                    });
                    target.bind();
                });

                var jsonArrayLength = thrownCtor(function () {
                    var badLength = new Proxy([], {
                        get: function (_, name) {
                            if (name === 'length') {
                                throw new Test262Error();
                            }
                        }
                    });

                    JSON.parse('[0,0]', function () {
                        this[1] = badLength;
                    });
                });

                var jsonArrayDefine = thrownCtor(function () {
                    var badDefine = new Proxy([null], {
                        defineProperty: function () {
                            throw new Test262Error();
                        }
                    });

                    JSON.parse('["first", null]', function (_, value) {
                        if (value === 'first') {
                            this[1] = badDefine;
                        }
                        return value;
                    });
                });

                var jsonObjectKeys = thrownCtor(function () {
                    var badKeys = new Proxy({}, {
                        ownKeys: function () {
                            throw new Test262Error();
                        }
                    });

                    JSON.parse('[0,0]', function () {
                        this[1] = badKeys;
                    });
                });

                return [
                    copyWithinHas,
                    dateToPrimitiveGet,
                    dateToPrimitiveCall,
                    copiedDate,
                    bindName,
                    jsonArrayLength,
                    jsonArrayDefine,
                    jsonObjectKeys
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|Test262Error|Test262Error|true|Test262Error|Test262Error|Test262Error|Test262Error", result.ToString());
    }

    [Fact]
    public void For_Statement_Const_Object_Destructuring_With_Omitted_Test_Preserves_Test262_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function Test262Error(message) {
                    this.message = message || "";
                }

                Test262Error.prototype.toString = function () {
                    return "Test262Error: " + this.message;
                };

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var poisonedProperty = Object.defineProperty({}, 'poisoned', {
                    get: function () {
                        throw new Test262Error();
                    }
                });

                return thrownCtor(function () {
                    for (const { poisoned } = poisonedProperty; ; ) {
                        break;
                    }
                });
            })();
            """);

        Assert.Equal("Test262Error", result.ToString());
    }

    [Fact]
    public void GroupBy_Object_Assign_Object_Prototype_Promise_And_RegExp_Preserve_Test262_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.ObjectMapGroupBy);

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var objectGroupBy = thrownCtor(function () {
                    Object.groupBy({
                        [Symbol.iterator]: function () { return this; },
                        next: function () { throw new Test262Error(); }
                    }, function () {
                        return 'group';
                    });
                });

                var mapGroupBy = thrownCtor(function () {
                    Map.groupBy({
                        [Symbol.iterator]: function () { return this; },
                        next: function () { throw new Test262Error(); }
                    }, function () {
                        return 'group';
                    });
                });

                var assignOwnKeys = thrownCtor(function () {
                    Object.assign({}, new Proxy({}, {
                        ownKeys: function () {
                            throw new Test262Error();
                        }
                    }));
                });

                var objectToString = thrownCtor(function () {
                    Object.defineProperty({}, Symbol.toStringTag, {
                        get: function () {
                            throw new Test262Error();
                        }
                    }).toString();
                });

                var protoGet = thrownCtor(function () {
                    var get = Object.getOwnPropertyDescriptor(Object.prototype, '__proto__').get;
                    var subject = new Proxy({}, {
                        getPrototypeOf: function () {
                            throw new Test262Error();
                        }
                    });
                    get.call(subject);
                });

                var promiseResolve = thrownCtor(function () {
                    var P = function (executor) {
                        return new Promise(function () {
                            executor(function () {
                                throw new Test262Error();
                            }, function () {});
                        });
                    };

                    Promise.resolve.call(P);
                });

                var regExpFlags = thrownCtor(function () {
                    var obj = {};
                    Object.defineProperty(obj, 'flags', {
                        get: function () {
                            throw new Test262Error();
                        }
                    });
                    obj[Symbol.match] = true;
                    new RegExp(obj);
                });

                var regExpMatch = thrownCtor(function () {
                    var re = /./;
                    Object.defineProperty(re, 'global', {
                        get: function () {
                            throw new Test262Error();
                        }
                    });

                    RegExp.prototype[Symbol.match].call(re);
                });

                return [
                    objectGroupBy,
                    mapGroupBy,
                    assignOwnKeys,
                    objectToString,
                    protoGet,
                    promiseResolve,
                    regExpFlags,
                    regExpMatch
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error", result.ToString());
    }

    [Fact]
    public void Native_BuiltIn_Write_Failures_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var target = {};
                Object.preventExtensions(target);

                return [
                    thrownCtor(function () {
                        Object.assign(target, { x: 1 });
                    }),
                    thrownCtor(function () {
                        var bad = new Proxy([null], {
                            defineProperty: function () {
                                return false;
                            }
                        });

                        JSON.parse('["first", null]', function (_, value) {
                            if (value === 'first') {
                                this[1] = bad;
                            }
                            return value;
                        });
                    }),
                    thrownCtor(function () {
                        var array = [];
                        Object.defineProperty(array, 'length', { writable: false });
                        array.pop();
                    }),
                    thrownCtor(function () {
                        var array = [];
                        Object.defineProperty(array, 'length', { writable: false });
                        array.push(1);
                    })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Numeric_String_Property_Access_Resolves_Element_Backed_Object_Properties()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                return [
                    String(({ 0: null })['0']),
                    String((new Proxy({ 0: null }, {}))['0'])
                ].join('|');
            })();
            """);

        Assert.Equal("null|null", result.ToString());
    }

    [Fact]
    public void Generator_Throw_Propagates_And_Resumes_Like_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                var uncaught = (function () {
                    var unreachable = 0;
                    function* g() {
                        yield 1;
                        unreachable += 1;
                        try {
                            yield 2;
                        } catch (e) {
                            yield e;
                        }
                    }

                    var iter = g();
                    iter.next();

                    try {
                        iter.throw(new Test262Error());
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name + '|' + unreachable;
                    }
                })();

                var caught = (function () {
                    function* g() {
                        try {
                            yield 1;
                        } catch (e) {
                            return e.constructor.name;
                        }
                    }

                    var iter = g();
                    iter.next();
                    var result = iter.throw(new Test262Error());
                    return result.value + '|' + result.done;
                })();

                return uncaught + '||' + caught;
            })();
            """);

        Assert.Equal("Test262Error|0||Test262Error|true", result.ToString());
    }

    [Fact]
    public void Generator_Delegated_Throw_Follows_Test262_For_InnerThrow_Methods()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function callErr() {
                    var thrown = { marker: 'callErr' };
                    var badIter = {};
                    var caught;
                    badIter[Symbol.iterator] = function() {
                      return {
                        next: function() { return { done: false }; },
                        throw: function() { throw thrown; }
                      };
                    };

                    function* g() {
                      try {
                        yield * badIter;
                      } catch (err) {
                        caught = err;
                      }
                    }

                    var iter = g();
                    iter.next();
                    var result = iter.throw();
                    return String(result.value) + '|' + result.done + '|' + String(caught === thrown);
                }

                function callNonObj() {
                    var badIter = {};
                    var caught;
                    badIter[Symbol.iterator] = function() {
                      return {
                        next: function() { return { done: false }; },
                        throw: function() { return 23; }
                      };
                    };

                    function* g() {
                      try {
                        yield * badIter;
                      } catch (err) {
                        caught = err;
                      }
                    }

                    var iter = g();
                    iter.next();
                    var result = iter.throw();
                    return String(result.value) + '|' + result.done + '|' + typeof caught + '|' + caught.constructor.name;
                }

                function getErr() {
                    var thrown = { marker: 'getErr' };
                    var badIter = {};
                    var caught;
                    var poisonedThrow = {
                      next: function() { return { done: false }; }
                    };
                    Object.defineProperty(poisonedThrow, 'throw', {
                      get: function() { throw thrown; }
                    });
                    badIter[Symbol.iterator] = function() {
                      return poisonedThrow;
                    };

                    function* g() {
                      try {
                        yield * badIter;
                      } catch (err) {
                        caught = err;
                      }
                    }

                    var iter = g();
                    iter.next();
                    var before = String(caught === undefined);
                    var result = iter.throw();
                    return before + '|' + String(result.value) + '|' + result.done + '|' + String(caught === thrown);
                }

                return [callErr(), callNonObj(), getErr()].join('||');
            })();
            """);

        Assert.Equal("undefined|true|true||undefined|true|object|TypeError||true|undefined|true|true", result.ToString());
    }

    [Fact]
    public void Function_Constructor_Only_Rejects_Arguments_And_Eval_Assignment_In_Strict_Mode()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function parsesSuccessfully(code) {
                try {
                  Function(code);
                  return true;
                } catch (_error) {
                  return false;
                }
              }

              function parseRaisesException(exception) {
                return function (code) {
                  try {
                    Function(code);
                    return false;
                  } catch (actual) {
                    return exception.prototype.isPrototypeOf(actual);
                  }
                };
              }

              function testLenientAndStrict(code, lenient_pred, strict_pred) {
                return strict_pred("'use strict'; " + code) && lenient_pred(code);
              }

              return [
                testLenientAndStrict('arguments=1', parsesSuccessfully, parseRaisesException(SyntaxError)),
                testLenientAndStrict('eval=1', parsesSuccessfully, parseRaisesException(SyntaxError)),
                testLenientAndStrict('(arguments)=1', parsesSuccessfully, parseRaisesException(SyntaxError)),
                testLenientAndStrict('(eval)=1', parsesSuccessfully, parseRaisesException(SyntaxError))
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true", result.ToString());
    }

    [Fact]
    public void Direct_Eval_Preserves_Caller_This_And_Strict_Function_Constructor_Rejects_Legacy_Octal()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var callThis;
            var evalThis;

            function capture() {
              eval('evalThis = this');
              callThis = this;
            }

            function thrownCtor(fn) {
              try {
                fn();
                return 'no-throw';
              } catch (e) {
                return e.constructor.name;
              }
            }

            capture.call(true);

            [
              callThis === evalThis,
              thrownCtor(function () { Function('"use strict"; 010'); })
            ].join('|');
            """);

        Assert.Equal("true|SyntaxError", result.ToString());
    }

    [Fact]
    public void Strict_Mode_Rejects_Legacy_Octal_Escape_In_Strings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function tryParse(code) {
                try { Function(code); return 'ok'; }
                catch (e) { return e instanceof SyntaxError ? 'SyntaxError' : 'other'; }
              }

              return [
                // Legacy octal escapes must be rejected in strict mode
                tryParse("'use strict'; \"\\1\""),
                tryParse("'use strict'; \"\\00\""),
                tryParse("'use strict'; \"\\010\""),
                // Null escape (\0 not followed by octal digit) is fine
                tryParse("'use strict'; \"\\0\""),
                // Normal strings are fine
                tryParse("'use strict'; \"hello\""),
                // Non-strict mode allows octal escapes
                tryParse("\"\\010\""),
                tryParse("\"\\1\"")
              ].join('|');
            })();
            """);

        Assert.Equal("SyntaxError|SyntaxError|SyntaxError|ok|ok|ok|ok", result.ToString());
    }

    [Fact]
    public void ForIn_Skips_Array_Holes_After_Shift()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              var x = ['a', , 'b', , 'c', 'd', 'e', 'f', 'g'];

              for (var p in x) {
                if (!(p in x)) {
                  return 'bad:' + p;
                }

                x.shift();
              }

              return 'ok';
            })();
            """);

        Assert.Equal("ok", result.ToString());
    }

    [Fact]
    public void Array_Modification_Methods_Respect_Indexed_Setters_On_Prototype_Chain()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function ensureSetterCalledOnce(fn, value, index) {
                var setterCalled = false;
                Object.defineProperty(Array.prototype, index, {
                  configurable: true,
                  set: function(v) {
                    if (setterCalled || v !== value) {
                      setterCalled = 'bad';
                      return;
                    }

                    setterCalled = true;
                  }
                });

                try {
                  fn();
                  return String(setterCalled);
                } finally {
                  delete Array.prototype[index];
                }
              }

              return [
                ensureSetterCalledOnce(function() { [/* hole */, 'reverse'].reverse(); }, 'reverse', 0),
                ensureSetterCalledOnce(function() { ['reverse', /* hole */,].reverse(); }, 'reverse', 1),
                ensureSetterCalledOnce(function() { [/* hole */, 'shift'].shift(); }, 'shift', 0),
                ensureSetterCalledOnce(function() { [/* hole */, 'sort'].sort(); }, 'sort', 0),
                ensureSetterCalledOnce(function() { [/* hole */, undefined].sort(); }, undefined, 0),
                ensureSetterCalledOnce(function() { [].splice(0, 0, 'splice'); }, 'splice', 0),
                ensureSetterCalledOnce(function() { [/* hole */, 'splice'].splice(0, 1); }, 'splice', 0),
                ensureSetterCalledOnce(function() { ['splice', /* hole */,].splice(0, 0, 'item'); }, 'splice', 1)
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true|true|true|true|true", result.ToString());
    }

    [Fact]
    public void Array_Iteration_Methods_Recheck_Indexed_Presence_After_Mutation()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function forEachVisitsPrototypeElementAfterOwnDelete() {
                var seen = false;
                var obj = { 0: 0, 1: 111, length: 10 };

                Object.defineProperty(obj, '0', {
                  get: function() {
                    delete obj[1];
                    return 0;
                  },
                  configurable: true
                });

                Object.prototype[1] = 1;

                try {
                  Array.prototype.forEach.call(obj, function(value, index) {
                    if (index === 1 && value === 1) {
                      seen = true;
                    }
                  });

                  return seen;
                } finally {
                  delete Object.prototype[1];
                }
              }

              function reduceKeepsNonConfigurableElementAfterLengthShrink() {
                var seen = false;
                var arr = [0, 1, 2, 3];

                Object.defineProperty(arr, '2', {
                  get: function() {
                    return 'unconfigurable';
                  },
                  configurable: false
                });

                Object.defineProperty(arr, '0', {
                  get: function() {
                    arr.length = 2;
                    return 1;
                  },
                  configurable: true
                });

                arr.reduce(function(accum, value, index) {
                  if (index === 2 && value === 'unconfigurable') {
                    seen = true;
                  }

                  return accum;
                });

                return seen;
              }

              function reduceRightKeepsNonConfigurableElementAfterLengthShrink() {
                var seen = false;
                var arr = [0, 1, 2, 3];

                Object.defineProperty(arr, '2', {
                  get: function() {
                    return 'unconfigurable';
                  },
                  configurable: false
                });

                Object.defineProperty(arr, '3', {
                  get: function() {
                    arr.length = 2;
                    return 1;
                  },
                  configurable: true
                });

                arr.reduceRight(function(accum, value, index) {
                  if (index === 2 && value === 'unconfigurable') {
                    seen = true;
                  }

                  return accum;
                });

                return seen;
              }

              return [
                forEachVisitsPrototypeElementAfterOwnDelete(),
                reduceKeepsNonConfigurableElementAfterLengthShrink(),
                reduceRightKeepsNonConfigurableElementAfterLengthShrink()
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true", result.ToString());
    }

    [Fact]
    public void Generator_And_AsyncGenerator_Parameter_Abrupt_Completions_Throw_On_Call()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var generatorCallCount = 0;
                var asyncGeneratorCallCount = 0;

                var generator = function*(_ = (function () { throw new Test262Error(); }())) {
                    generatorCallCount += 1;
                };

                var asyncGenerator = async function*({ x = (function () { throw new Test262Error(); }()) } = {}) {
                    asyncGeneratorCallCount += 1;
                };

                return [
                    thrownCtor(function () { generator(); }),
                    String(generatorCallCount),
                    thrownCtor(function () { asyncGenerator(); }),
                    String(asyncGeneratorCallCount)
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|0|Test262Error|0", result.ToString());
    }

    [Fact]
    public void AsyncGenerator_Next_Rejects_With_Original_Object_On_YieldStar_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Execute("""
            (function () {
                var exprReason = {};
                var getIterReason = {};

                async function* exprAbrupt() {
                    yield* (function () {
                        throw exprReason;
                    })();
                }

                async function* getIterAbrupt() {
                    yield* {
                        get [Symbol.asyncIterator]() {
                            throw getIterReason;
                        }
                    };
                }

                function inspect(iter, expectedReason) {
                    return iter.next().then(
                        function () { return 'fulfilled'; },
                        function (reason) {
                            return (reason === expectedReason ? 'same' : 'different');
                        }
                    ).then(function (status) {
                        return iter.next().then(function (result) {
                            return status + '|' + result.done + '|' + (result.value === undefined);
                        });
                    });
                }

                return Promise.all([
                    inspect(exprAbrupt(), exprReason),
                    inspect(getIterAbrupt(), getIterReason)
                ]).then(function (results) {
                    return results.join('|');
                });
            })();
            """);

        Assert.Equal("same|true|true|same|true|true", result.ToString());
    }

    [Fact]
    public void Promise_Resolve_Poisoned_Then_Rejects_With_Thrown_Object()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Execute("""
            (function () {
                var reason = {};
                var resolve;
                var poisonedThen = Object.defineProperty({}, 'then', {
                    get: function () {
                        throw reason;
                    }
                });

                var promise = new Promise(function (_resolve) {
                    resolve = _resolve;
                });

                var returnValue = resolve(poisonedThen);

                return promise.then(
                    function () {
                        return 'fulfilled';
                    },
                    function (value) {
                        return [
                            value === reason ? 'same' : 'different',
                            returnValue === undefined ? 'undefined' : 'wrong'
                        ].join('|');
                    }
                );
            })();
            """);

        Assert.Equal("same|undefined", result.ToString());
    }

    [Fact]
    public void Array_FinalizationRegistry_And_Function_TypeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            return [
                thrownCtor(function () {
                    var object = {
                        valueOf() {
                            return {};
                        },
                        toString() {
                            return {};
                        }
                    };

                    [object].toString();
                }),
                thrownCtor(function () {
                    var finalizationRegistry = new FinalizationRegistry(function() {});
                    var target = {};
                    finalizationRegistry.register(target, target);
                }),
                (function () {
                    var reg = new FinalizationRegistry(function() {});
                    try {
                        return reg.register(Symbol('a description')) === undefined ? 'undefined' : 'wrong-value';
                    } catch (e) {
                        return e.constructor.name;
                    }
                })(),
                thrownCtor(function () {
                    'use strict';
                    function fn() {}
                    return fn.caller;
                })
            ].join('|');
        })();");

        Assert.Equal("TypeError|TypeError|undefined|TypeError", result.ToString());
    }

    [Fact]
    public void Function_Iterator_Global_And_Generator_TypeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            return [
                thrownCtor(function () {
                    (function () {}).apply(null, true);
                }),
                (function () {
                    return (function () {
                        return (function (a, b) {
                            return a + ',' + b;
                        }).apply(null, arguments);
                    })('x', 'y');
                })(),
                (function () {
                    return (function (a, b) {
                        return a + ',' + b;
                    }).apply(null, { 0: 'x', 1: 'y', length: 2 });
                })(),
                thrownCtor(function () {
                    var f = function () {};
                    f.prototype = 1;
                    f[Symbol.hasInstance]({});
                }),
                thrownCtor(function () {
                    isFinite({
                        [Symbol.toPrimitive]() {
                            return {};
                        }
                    });
                }),
                thrownCtor(function () {
                    isNaN({
                        [Symbol.toPrimitive]() {
                            return Symbol.iterator;
                        }
                    });
                }),
                thrownCtor(function () {
                    Iterator.from(0);
                }),
                (function () {
                    let n = [0, 1, 2][Symbol.iterator]();
                    let iter = {
                        [Symbol.iterator]: null,
                        next() {
                            return n.next();
                        }
                    };
                    return Array.from(Iterator.from(iter)).join(',');
                })(),
                thrownCtor(function () {
                    Iterator.from({
                        [Symbol.iterator]: 0,
                        next() {
                            return { done: true };
                        }
                    });
                }),
                thrownCtor(function () {
                    Iterator.prototype.every.call({ next: 0 }, function () { return true; });
                }),
                thrownCtor(function () {
                    var iter = Iterator.prototype.drop.call({ next: 0 }, 1);
                    iter.next();
                }),
                thrownCtor(function () {
                    Iterator.concat({
                        [Symbol.iterator]() {
                            return {
                                next() {
                                    return null;
                                }
                            };
                        }
                    }).next();
                })
            ].join('|');
        })();");

        Assert.Equal("TypeError|x,y|x,y|TypeError|TypeError|TypeError|TypeError|0,1,2|TypeError|TypeError|TypeError|TypeError", result.ToString());

        ctx.Eval("var iterReturn; function* gReturn() { iterReturn.return(42); } iterReturn = gReturn();");
        var returnEx = Assert.Throws<JSException>(() => ctx.Eval("iterReturn.next();"));
        Assert.Equal("TypeError", returnEx.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("true|true", ctx.Eval("var result = iterReturn.next(); String(result.done) + '|' + String(result.value === undefined);").ToString());

        ctx.Eval("var iterThrow; function* gThrow() { iterThrow.throw(42); } iterThrow = gThrow();");
        var throwEx = Assert.Throws<JSException>(() => ctx.Eval("iterThrow.next();"));
        Assert.Equal("TypeError", throwEx.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("true|true", ctx.Eval("var result = iterThrow.next(); String(result.done) + '|' + String(result.value === undefined);").ToString());

        var concatReturnResult = ctx.Eval("""
            (function () {
              let enterCount = 0;
              let iterator;

              let iterable = {
                [Symbol.iterator]() {
                  return {
                    next() {
                      return { done: false };
                    },
                    return() {
                      enterCount++;
                      iterator.return();
                      return { done: false };
                    }
                  };
                }
              };

              iterator = Iterator.concat(iterable);
              iterator.next();

              try {
                iterator.return();
                return 'no-throw';
              } catch (e) {
                return e.constructor.name + '|' + enterCount;
              }
            })();
            """);

        Assert.Equal("TypeError|1", concatReturnResult.ToString());
    }

    [Fact]
    public void Iterator_Wrappers_Call_Underlying_Return_With_Zero_Arguments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        static JSObject CreateIteratorResult(JSValue value, bool done)
        {
            var result = JSObject.NewWithProperties();
            result.FastAddValue(KeyStrings.value, value, JSPropertyAttributes.EnumerableConfigurableValue);
            result.FastAddValue(KeyStrings.done, done ? JSBoolean.True : JSBoolean.False, JSPropertyAttributes.EnumerableConfigurableValue);
            return result;
        }

        var wrapCallArgsLength = -1;
        var wrapTarget = JSObject.NewWithProperties();
        wrapTarget.FastAddValue(
            KeyStrings.next,
            new JSFunction((in Arguments a) => CreateIteratorResult(0.Marshal(), done: false), "next", 0),
            JSPropertyAttributes.ConfigurableValue);
        wrapTarget.FastAddValue(
            KeyStrings.@return,
            new JSFunction((in Arguments a) =>
            {
                wrapCallArgsLength = a.Length;
                return CreateIteratorResult(a.Length.Marshal(), done: true);
            }, "return", 0),
            JSPropertyAttributes.ConfigurableValue);

        var concatCallArgsLength = -1;
        var iterable = JSObject.NewWithProperties();
        iterable.FastAddValue(
            (IJSSymbol)JSSymbol.iterator,
            new JSFunction((in Arguments a) =>
            {
                var iterator = JSObject.NewWithProperties();
                iterator.FastAddValue(
                    KeyStrings.next,
                    new JSFunction((in Arguments inner) => CreateIteratorResult(0.Marshal(), done: false), "next", 0),
                    JSPropertyAttributes.ConfigurableValue);
                iterator.FastAddValue(
                    KeyStrings.@return,
                    new JSFunction((in Arguments inner) =>
                    {
                        concatCallArgsLength = inner.Length;
                        return CreateIteratorResult(inner.Length.Marshal(), done: true);
                    }, "return", 0),
                    JSPropertyAttributes.ConfigurableValue);
                return iterator;
            }, "Symbol.iterator", 0),
            JSPropertyAttributes.ConfigurableValue);

        ctx["wrapTarget"] = wrapTarget;
        ctx["concatIterable"] = iterable;

        var result = ctx.Eval("""
            (function () {
              var wrap = Iterator.from(wrapTarget);
              wrap.next();
              var wrapResult = wrap.return(1).value;

              var concat = Iterator.concat(concatIterable);
              concat.next();
              var concatResult = concat.return(1).value;

              return [wrapResult, concatResult].join('|');
            })();
            """);

        Assert.Equal(0, wrapCallArgsLength);
        Assert.Equal(0, concatCallArgsLength);
        Assert.Equal("0|0", result.ToString());
    }

    [Fact]
    public void Math_SumPrecise_Rejects_NonNumbers_Without_Coercion_And_Closes_Iterators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              var coercions = 0;
              var objectWithValueOf = {
                valueOf: function() {
                  ++coercions;
                  throw new Error('valueOf should not be called');
                },
                toString: function() {
                  ++coercions;
                  throw new Error('toString should not be called');
                }
              };

              var nextCalls = 0;
              var returnCalls = 0;
              var iterator = {
                next: function () {
                  ++nextCalls;
                  return { done: false, value: objectWithValueOf };
                },
                return: function () {
                  ++returnCalls;
                  return {};
                }
              };

              return [
                thrownCtor(function () { Math.sumPrecise([{}]); }),
                thrownCtor(function () { Math.sumPrecise([0n]); }),
                thrownCtor(function () { Math.sumPrecise([objectWithValueOf]); }),
                thrownCtor(function () { Math.sumPrecise([objectWithValueOf, NaN]); }),
                thrownCtor(function () { Math.sumPrecise([NaN, objectWithValueOf]); }),
                thrownCtor(function () { Math.sumPrecise([-Infinity, Infinity, objectWithValueOf]); }),
                coercions,
                thrownCtor(function () {
                  Math.sumPrecise({
                    [Symbol.iterator]: function () {
                      return iterator;
                    }
                  });
                }),
                nextCalls,
                returnCalls
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|0|TypeError|1|1", result.ToString());
    }

    [Fact]
    public void Iterator_Helper_ShortCircuit_And_ArgumentValidation_Close_Underlying_Iterators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              function snapshot(method, predicateResult) {
                function* g() {
                  yield 0;
                  yield 1;
                }

                var iter = g();
                var value = iter[method](function () { return predicateResult; });
                return String(value) + ',' + String(iter.next().done);
              }

              var closed = 0;
              var closable = {
                next: function () {
                  throw new Test262Error('next should not be read');
                },
                return: function () {
                  ++closed;
                  return {};
                }
              };

              return [
                snapshot('some', true),
                snapshot('every', false),
                snapshot('find', true),
                thrownCtor(function () {
                  Iterator.prototype.map.call(closable);
                }),
                thrownCtor(function () {
                  Iterator.prototype.reduce.call(closable, {});
                }),
                String(closed)
              ].join('|');
            })();
            """);

        Assert.Equal("true,true|false,true|0,true|TypeError|TypeError|2", result.ToString());
    }

    [Fact]
    public void Iterator_Take_Drop_And_Map_Set_Constructors_Close_Iterators_On_Abrupt_Completion()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              function outcome(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e && typeof e === 'object' ? e.constructor.name : String(e);
                }
              }

              function makeClosable(nextValue, returnThrows) {
                var iterable = { closed: false };
                iterable[Symbol.iterator] = function () {
                  return {
                    first: true,
                    next: function () {
                      if (this.first) {
                        this.first = false;
                        return nextValue;
                      }

                      return { value: undefined, done: true };
                    },
                    return: function () {
                      iterable.closed = true;
                      if (returnThrows) {
                        throw 'return throws';
                      }

                      return {};
                    }
                  };
                };

                return iterable;
              }

              var closed = false;
              var closable = {
                __proto__: Iterator.prototype,
                get next() {
                  throw new Test262Error('next should not be read');
                },
                return() {
                  closed = true;
                  throw new Test262Error('return should be ignored');
                }
              };

              function constructorSnapshot(ctor, methodName, nextValue) {
                var iterable = makeClosable(nextValue, true);
                var original = ctor.prototype[methodName];
                Object.defineProperty(ctor.prototype, methodName, {
                  value: function () {
                    throw methodName + ' throws';
                  },
                  configurable: true
                });

                try {
                  return outcome(function () {
                    new ctor(iterable);
                  }) + ',' + String(iterable.closed);
                } finally {
                  Object.defineProperty(ctor.prototype, methodName, {
                    value: original,
                    configurable: true
                  });
                }
              }

              return [
                outcome(function () { closable.take(); }) + ',' + String(closed),
                (closed = false, outcome(function () { closable.drop(-1); }) + ',' + String(closed)),
                (function () {
                  var iterable = makeClosable({ value: 'non object', done: false }, false);
                  return outcome(function () {
                    new Map(iterable);
                  }) + ',' + String(iterable.closed);
                })(),
                constructorSnapshot(Map, 'set', { value: [{}, {}], done: false }),
                constructorSnapshot(WeakMap, 'set', { value: [{}, {}], done: false }),
                constructorSnapshot(Set, 'add', { value: {}, done: false }),
                constructorSnapshot(WeakSet, 'add', { value: {}, done: false })
              ].join('|');
            })();
            """);

        Assert.Equal("RangeError,true|RangeError,true|TypeError,true|set throws,true|set throws,true|add throws,true|add throws,true", result.ToString());
    }

    [Fact]
    public void Object_Literal_Proto_Setter_Uses_Prototype_Without_Creating_Own_Property()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var proto = {
                getValue: function () {
                  return this.value;
                }
              };
              var child = {
                __proto__: proto,
                value: 42
              };
              var ignored = {
                __proto__: 1
              };
              var closable = {
                __proto__: Iterator.prototype,
                get next() {
                  throw new Test262Error('next should not be read');
                },
                return() {
                  return { done: true };
                }
              };

              return [
                String(Object.getPrototypeOf(child) === proto),
                String(child.getValue()),
                String(Object.prototype.hasOwnProperty.call(child, '__proto__')),
                String(Object.getPrototypeOf(ignored) === Object.prototype),
                String(Object.prototype.hasOwnProperty.call(ignored, '__proto__')),
                (function () {
                  try {
                    closable.every();
                    return 'no-throw';
                  } catch (e) {
                    return e.constructor.name;
                  }
                })()
              ].join('|');
            })();
            """);

        Assert.Equal("true|42|false|true|false|TypeError", result.ToString());
    }

    [Fact]
    public void Bound_HasInstance_Zero_Normalization_And_Proxy_IsPrototypeOf_Regressions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var BC = function () {};
              var bc = new BC();
              var bound = BC.bind();
              var map = new Map();
              map.set(-0, 42);
              var proto = [];
              var proxy = new Proxy({}, {
                getPrototypeOf: function () {
                  return proto;
                }
              });

              return [
                String(bound[Symbol.hasInstance](bc)),
                String(map.has(+0)),
                String(map.has(-0)),
                String(proto.isPrototypeOf(proxy))
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true", result.ToString());
    }

    [Fact]
    public void InstanceOf_Uses_SymbolHasInstance_For_Callable_And_NonCallable_Targets()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var passed = false;
              var obj = { foo: true };
              var C = function () {};
              Object.defineProperty(C, Symbol.hasInstance, {
                value: function (inst) {
                  passed = inst.foo;
                  return false;
                }
              });
              var custom = {
                [Symbol.hasInstance]: function () {
                  return true;
                }
              };

              return [
                String(obj instanceof C),
                String(passed),
                String(1 instanceof custom)
              ].join('|');
            })();
            """);

        Assert.Equal("false|true|true", result.ToString());
    }

    [Fact]
    public void Function_Bind_Name_Length_And_HasInstance_Descriptor_Regresions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("String(Object.getOwnPropertyDescriptor(Function.prototype, Symbol.hasInstance).configurable);");

        Assert.Equal("false", result.ToString());
    }

    [Fact]
    public void Property_Key_Canonicalization_Preserves_Empty_And_NonCanonical_String_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              function* hasEmptyKey() {
                return '' in (yield);
              }

              var iter = hasEmptyKey();
              iter.next();

              var weird = { '1.0': 4, ' 1 ': 5, '+0': 6, '-0': 7 };
              return [
                String(iter.next({ '': 0 }).value),
                String(Object.hasOwn({ '': 1 }, '')),
                String('' in { '': 1 }),
                String(Object.hasOwn(weird, '1.0')),
                String(Object.hasOwn(weird, 1)),
                String(weird[1]),
                String(weird['1.0']),
                String(Object.hasOwn(weird, '+0')),
                String(Object.hasOwn(weird, 0))
              ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true|false|undefined|4|true|false", result.ToString());
    }

    [Fact]
    public void Generator_Next_Forwards_First_Argument_To_Delegated_Iterator()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var received;
              var iterable = {
                [Symbol.iterator]: function () {
                  return {
                    next: function (...args) {
                      received = args.length + '|' + String(args[0]);
                      return { done: true };
                    }
                  };
                }
              };

              function* outer() {
                yield* iterable;
              }

              outer().next(123);
              return received;
            })();
            """);

        Assert.Equal("1|undefined", result.ToString());
    }

    [Fact]
    public void Function_Bind_Uses_Current_Name_And_Length_Descriptors_When_Creating_Bound_Functions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              var fun = function () {};
              Object.defineProperty(fun, 'name', { value: 1337 });
              Object.defineProperty(fun, 'length', { value: '15' });
              var nonStringName = fun.bind();

              Object.defineProperty(fun, 'length', { value: Number.MAX_SAFE_INTEGER });
              var maxSafeIntegerLength = fun.bind();

              Object.defineProperty(fun, 'length', { value: -100 });
              var negativeLength = fun.bind();

              return [
                nonStringName.name,
                String(nonStringName.length),
                String(maxSafeIntegerLength.length),
                String(negativeLength.length)
              ].join('|');
            })();
            """);

        Assert.Equal("bound |15|9007199254740991|0", result.ToString());
    }

    [Fact]
    public void Array_Includes_And_Date_DefaultValue_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
              Date.prototype.toString = function () { return 1; };
              Date.prototype.valueOf = function () { return 0; };

              return [
                String(new Date == true),
                String(new Date == false),
                String(true == new Date),
                String(false == new Date),
                String([1,,2].includes(2)),
                String([,].includes()),
                String([].includes.call({ __proto__: { 1: 2 }, length: 3 }, 2)),
                String([].includes.call(new Proxy([1], { get() { return 2; } }), 2))
              ].join('|');
            })();
            """);

        Assert.Equal("true|false|true|false|true|true|true|true", result.ToString());
    }

    [Fact]
    public void Iterable_And_Object_TypeError_Regressions_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            function* delegated() {
                yield* true;
            }

            return [
                thrownCtor(function () { Object.prototype.valueOf.call(null); }),
                Object.getPrototypeOf(Object.create(null)) === null ? 'null' : 'non-null',
                thrownCtor(function () { delegated().next(); }),
                thrownCtor(function () { for (var value of true) { return value; } }),
                thrownCtor(function () { [...true]; })
            ].join('|');
        })();");

        Assert.Equal("TypeError|null|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Object_Internal_Method_TypeError_Regressions_Match_Test262_Expectations()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            return [
                (function () {
                    var returned = false;
                    var iterable = {
                        [Symbol.iterator]: function () {
                            var advanced = false;
                            return {
                                next: function () {
                                    if (advanced) {
                                        throw new Error('should only advance once');
                                    }

                                    advanced = true;
                                    return {
                                        done: false,
                                        value: null
                                    };
                                },
                                return: function () {
                                    returned = true;
                                    return {};
                                }
                            };
                        }
                    };

                    return thrownCtor(function () {
                        Object.fromEntries(iterable);
                    }) + '|' + returned;
                })(),
                (function () {
                    var proto = {};
                    var subject = Object.create(proto);
                    Object.preventExtensions(subject);
                    subject.__proto__ = proto;

                    return thrownCtor(function () {
                        subject.__proto__ = {};
                    }) + '|' + (Object.getPrototypeOf(subject) === proto);
                })(),
                (function () {
                    var root = {};
                    var intermediary = Object.create(root);
                    var leaf = Object.create(intermediary);

                    return thrownCtor(function () {
                        root.__proto__ = leaf;
                    }) + '|' + (Object.getPrototypeOf(root) === Object.prototype);
                })(),
                thrownCtor(function () {
                    var obj = {};
                    Object.preventExtensions(obj);
                    Object.setPrototypeOf(obj, null);
                }),
                thrownCtor(function () { Object.getPrototypeOf(); }),
                thrownCtor(function () {
                    var handle = Proxy.revocable([], {});
                    handle.revoke();
                    Object.prototype.toString.call(handle.proxy);
                }),
                thrownCtor(function () {
                    var target = {};
                    var symbol = Symbol();
                    target[symbol] = 2;
                    var proxy = new Proxy(target, {
                        ownKeys: function() {
                            return [];
                        }
                    });

                    Object.preventExtensions(target);
                    Object.getOwnPropertyNames(proxy);
                }),
                thrownCtor(function () {
                    var target = {};
                    var proxy = new Proxy(target, {
                        ownKeys: function() {
                            return ['prop'];
                        }
                    });

                    Object.preventExtensions(target);
                    Object.getOwnPropertySymbols(proxy);
                })
            ].join('|');
        })();");

        Assert.Equal(
            "TypeError|true|TypeError|true|TypeError|true|TypeError|TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void Object_Binding_Patterns_Require_Object_Coercible_Input()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function thrownCtor(fn) {
                try {
                  fn();
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              function bindingTarget({}) {}
              var arrowNull = ({ } = null) => 0;
              var arrowUndefined = ({ } = undefined) => 0;

              return [
                thrownCtor(function () { bindingTarget(null); }),
                thrownCtor(function () { bindingTarget(undefined); }),
                thrownCtor(function () { arrowNull(); }),
                thrownCtor(function () { arrowUndefined(); })
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Class_Static_Prototype_Members_Throw_TypeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
              function thrownCtor(source) {
                try {
                  eval(source);
                  return 'no-throw';
                } catch (e) {
                  return e.constructor.name;
                }
              }

              return [
                thrownCtor("class C { static ['prototype']() {} }"),
                thrownCtor("class C { static get ['prototype']() { return 1; } }"),
                thrownCtor("class C { static set ['prototype'](value) {} }"),
                thrownCtor("class C { static *['prototype']() {} }")
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Map_Constructor_Uses_Iterable_Protocol_And_Rejects_Invalid_Entries()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval(@"(function () {
            function thrownCtor(fn) {
                try {
                    fn();
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            }

            return [
                thrownCtor(function () { new Map(true); }),
                thrownCtor(function () { new Map({ [Symbol.iterator]: function* () { yield 1; } }); }),
                (function () {
                    var original = Object.getOwnPropertyDescriptor(Map.prototype, 'set');
                    try {
                        Object.defineProperty(Map.prototype, 'set', { value: 1, configurable: true });
                        return thrownCtor(function () { new Map([['a', 1]]); });
                    } finally {
                        Object.defineProperty(Map.prototype, 'set', original);
                    }
                })()
            ].join('|');
        })();");

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Date_String_And_RegExp_Coercions_Propagate_TypeErrors_Like_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    (function () {
                        var value = {};
                        value[Symbol.toPrimitive] = function () {
                            return Symbol.toPrimitive;
                        };

                        return thrownCtor(function () {
                            new Date(value);
                        });
                    })(),
                    thrownCtor(function () { ''.charAt(Object.create(null)); }),
                    thrownCtor(function () { ''.charCodeAt(Object.create(null)); }),
                    thrownCtor(function () { String.prototype.codePointAt.call(Symbol(), 1); }),
                    thrownCtor(function () { ''.endsWith(Symbol()); }),
                    thrownCtor(function () { /./[Symbol.search](Symbol.search); }),
                    thrownCtor(function () { /./[Symbol.split](Symbol.split); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void String_Symbol_WeakMap_RegExp_And_Instanceof_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function outcome(fn) {
                    try {
                        return fn();
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    String(Symbol('66')),
                    outcome(function () { return new String(Symbol('66')); }),
                    outcome(function () { return new Symbol('x'); }),
                    outcome(function () { return new WeakMap({}); }),
                    outcome(function () {
                        var regex = /./;
                        regex.exec = function () { return 86; };
                        return regex[Symbol.match]('');
                    }),
                    outcome(function () {
                        function Custom() {}
                        var target = {};
                        var proxy = new Proxy(target, {
                            getPrototypeOf: function () {
                                return Custom.prototype;
                            }
                        });

                        Object.preventExtensions(target);
                        return proxy instanceof Custom;
                    })
                ].join('|');
            })();
            """);

        Assert.Equal("Symbol(66)|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Intl_TypeError_Regressions_Match_Test262_Basics()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Intl.DisplayNames(); }),
                    thrownCtor(function () { new Intl.DisplayNames([], null); }),
                    thrownCtor(function () { new Intl.DisplayNames([], Symbol()); }),
                    thrownCtor(function () { Intl.DurationFormat(); }),
                    thrownCtor(function () { new Intl.DurationFormat([], null); }),
                    thrownCtor(function () { Intl.supportedValuesOf(Symbol()); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void ScriptHost_Compatibility_BuiltIns_Expose_Missing_Test262_Functions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var regExpStringIteratorProto = Object.getPrototypeOf(/./[Symbol.matchAll](''));
                var duration = new Intl.DurationFormat();
                var format = duration.format;
                var formatToParts = duration.formatToParts;
                var resolvedOptions = duration.resolvedOptions;
                var supportedLocalesOf = Intl.DurationFormat.supportedLocalesOf;

                return [
                    typeof Object.getOwnPropertyDescriptor(regExpStringIteratorProto, 'next').value,
                    typeof BigInt64Array,
                    typeof BigUint64Array,
                    thrownCtor(function () { BigInt64Array.prototype.buffer; }),
                    typeof Intl.ListFormat,
                    thrownCtor(function () { Intl.ListFormat(); }),
                    thrownCtor(function () { new Intl.ListFormat([], null); }),
                    typeof Intl.Locale,
                    thrownCtor(function () { Intl.Locale(); }),
                    thrownCtor(function () { new Intl.Locale(true); }),
                    typeof Intl.DurationFormat.prototype.format,
                    typeof Intl.DurationFormat.prototype.formatToParts,
                    typeof Intl.DurationFormat.prototype.resolvedOptions,
                    typeof Intl.DurationFormat.supportedLocalesOf,
                    thrownCtor(function () { format({ hours: 1 }); }),
                    thrownCtor(function () { formatToParts({ hours: 1 }); }),
                    thrownCtor(function () { resolvedOptions(); }),
                    Array.isArray(supportedLocalesOf.call(null))
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|function|TypeError|function|TypeError|TypeError|function|TypeError|TypeError|function|function|function|function|TypeError|TypeError|TypeError|true", result.ToString());
    }

    [Fact]
    public void Intl_SupportedLocalesOf_Validates_Invalid_Locale_Arguments()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Intl.DurationFormat.supportedLocalesOf(Symbol()); }),
                    thrownCtor(function () { Intl.ListFormat.supportedLocalesOf(Symbol()); }),
                    thrownCtor(function () { Intl.RelativeTimeFormat.supportedLocalesOf(Symbol()); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Intl_DisplayNames_Exposes_Prototype_Methods_And_Resolved_Options()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                var displayNames = new Intl.DisplayNames('fr', {
                    type: 'language',
                    style: 'short',
                    fallback: 'none',
                    languageDisplay: 'standard'
                });
                function Custom() {}
                Custom.prototype = { marker: true };

                var custom = Reflect.construct(Intl.DisplayNames, ['en', { type: 'region' }], Custom);
                var resolved = displayNames.resolvedOptions();

                return [
                    typeof Intl.DisplayNames.prototype.of,
                    typeof Intl.DisplayNames.prototype.resolvedOptions,
                    Object.prototype.toString.call(displayNames),
                    Object.getPrototypeOf(displayNames) === Intl.DisplayNames.prototype,
                    Object.getPrototypeOf(custom) === Custom.prototype,
                    displayNames.of('en-US'),
                    resolved.locale,
                    resolved.style,
                    resolved.type,
                    resolved.fallback,
                    resolved.languageDisplay
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|[object Intl.DisplayNames]|true|true|en-US|fr|short|language|none|standard", result.ToString());
    }

    [Fact]
    public void Intl_DisplayNames_Validates_Options_Codes_And_Brands()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { new Intl.DisplayNames('en'); }),
                    thrownCtor(function () { new Intl.DisplayNames('en', { type: 'language', style: 'small' }); }),
                    thrownCtor(function () { new Intl.DisplayNames('en', { type: 'language', fallback: 'err' }); }),
                    thrownCtor(function () { new Intl.DisplayNames('en', { type: 'language', localeMatcher: 'bestfit' }); }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            get type() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () { Intl.DisplayNames.prototype.of.call({}, 'en'); }),
                    thrownCtor(function () { new Intl.DisplayNames('en', { type: 'region' }).of(''); }),
                    thrownCtor(function () { new Intl.DisplayNames('en', { type: 'dateTimeField' }).of(''); })
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|RangeError|RangeError|RangeError|Test262Error|TypeError|RangeError|RangeError", result.ToString());
    }

    [Fact]
    public void Array_ToSorted_Reads_Through_Holes_And_Creates_Undefined_Properties()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                var arr = [3, , 4, , 1];
                Array.prototype[3] = 2;
                try {
                    var sorted = arr.toSorted();
                    return [
                        sorted.join(','),
                        sorted.hasOwnProperty(4),
                        arr.hasOwnProperty(1),
                        arr.hasOwnProperty(3),
                        Array.prototype[3]
                    ].join('|');
                } finally {
                    delete Array.prototype[3];
                }
            })();
            """);

        Assert.Equal("1,2,3,4,|true|false|false|2", result.ToString());
    }

    [Fact]
    public void RangeError_Regressions_For_ArrayBuffer_BigInt_Date_And_Array_Creation_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var spliceTarget = Object.defineProperty({}, 'length', {
                    get: function() {
                        return 2 ** 32;
                    }
                });

                return [
                    thrownCtor(function () { Array.prototype.map.call({ length: Infinity }, function () { return 0; }); }),
                    thrownCtor(function () { Array.prototype.splice.call(spliceTarget, 0); }),
                    thrownCtor(function () { Array.prototype.toSorted.call({ length: 2 ** 32 }); }),
                    thrownCtor(function () { new ArrayBuffer(-1.1); }),
                    thrownCtor(function () { BigInt(1.1); }),
                    thrownCtor(function () { new Date(8.64e15 + 1).toISOString(); })
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|RangeError|RangeError|RangeError|RangeError|RangeError", result.ToString());
    }

    [Fact]
    public void Intl_DateTimeFormat_RangeError_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var format = new Intl.DateTimeFormat().format;
                return [
                    thrownCtor(function () { new Intl.DateTimeFormat('en', { timeZone: '−10:00' }); }),
                    thrownCtor(function () { format('2017-11-10T14:09:00.000Z'); }),
                    thrownCtor(function () { format(8.64e15 + 1); }),
                    typeof format(8.64e15)
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|RangeError|RangeError|string", result.ToString());
    }

    [Fact]
    public void Date_And_Intl_RangeError_Regressions_Match_Test262_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var dtf = new Intl.DateTimeFormat();
                var dateTimeString = '2017-11-10T14:09:00.000Z';
                var date = new Date(dateTimeString);
                var duration = new Intl.DurationFormat();

                return [
                    thrownCtor(function () { new Date(Infinity, 1, 70, 0, 0, 0).toISOString(); }),
                    thrownCtor(function () { new Date(-Infinity, 1, 70, 0, 0, 0).toISOString(); }),
                    thrownCtor(function () { Intl.getCanonicalLocales('en-us-'); }),
                    thrownCtor(function () { Intl.getCanonicalLocales('en-u-c0'); }),
                    thrownCtor(function () { dtf.formatRange(dateTimeString, date); }),
                    thrownCtor(function () { dtf.formatRangeToParts(date, dateTimeString); }),
                    thrownCtor(function () { new Intl.Locale('en', { collation: '' }); }),
                    thrownCtor(function () { new Intl.Locale('x-default', { language: 'fr', script: 'Cyrl', region: 'DE', numberingSystem: 'latn' }); }),
                    thrownCtor(function () { duration.format({ hours: -1, minutes: 10 }); }),
                    thrownCtor(function () { duration.formatToParts({ hours: 2, minutes: -10 }); }),
                    Intl.getCanonicalLocales('en-u-0c')[0]
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|en-u-0c", result.ToString());
    }

    [Fact]
    public void RangeError_Regressions_For_String_Number_Intl_And_TypedArray_Match_Test262_Samples()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var nf = new Intl.NumberFormat();

                return [
                    thrownCtor(function () { String.fromCodePoint('_1'); }),
                    thrownCtor(function () { 'foo'.normalize(null); }),
                    thrownCtor(function () { Intl.DurationFormat.supportedLocalesOf(['de-gregory-gregory']); }),
                    thrownCtor(function () { Intl.ListFormat.supportedLocalesOf(['de-gregory-gregory']); }),
                    thrownCtor(function () { Intl.RelativeTimeFormat.supportedLocalesOf(['de-gregory-gregory']); }),
                    thrownCtor(function () { nf.formatRange(NaN, 1); }),
                    thrownCtor(function () { nf.formatRangeToParts(1, NaN); }),
                    thrownCtor(function () { Number.prototype.toFixed.call(Infinity, 555); }),
                    thrownCtor(function () { (5).toString(Math.pow(2, 32) + 10); }),
                    thrownCtor(function () { new Int32Array(new ArrayBuffer(12), 2); }),
                    thrownCtor(function () { new Int32Array(new ArrayBuffer(12), 0, 4); }),
                    thrownCtor(function () { new Uint8Array().set([], -1); }),
                    thrownCtor(function () { new Int32Array(4).set([0], 2147483648); })
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError|RangeError", result.ToString());
    }

    [Fact]
    public void Intl_NumberFormat_RelativeTimeFormat_DurationFormat_And_RegExp_Split_TypeErrors_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var numberFormatNoInstanceof = (function () {
                    var original = Object.getOwnPropertyDescriptor(Intl.NumberFormat, Symbol.hasInstance);
                    Object.defineProperty(Intl.NumberFormat, Symbol.hasInstance, {
                        get: function () {
                            throw new Test262Error();
                        },
                        configurable: true
                    });

                    try {
                        return thrownCtor(function () {
                            return Object.create(Intl.NumberFormat.prototype).format;
                        });
                    } finally {
                        if (original) {
                            Object.defineProperty(Intl.NumberFormat, Symbol.hasInstance, original);
                        } else {
                            delete Intl.NumberFormat[Symbol.hasInstance];
                        }
                    }
                })();

                var splitSpecies = (function () {
                    var re = /./;
                    re.constructor = function () {};
                    re[Symbol.split]();

                    re.constructor[Symbol.species] = {};
                    return thrownCtor(function () {
                        re[Symbol.split]();
                    });
                })();

                return [
                    thrownCtor(function () { new Intl.NumberFormat([], { style: 'unit' }); }),
                    thrownCtor(function () { new Intl.NumberFormat([], { style: 'currency', unit: 'test' }); }),
                    numberFormatNoInstanceof,
                    thrownCtor(function () { Intl.RelativeTimeFormat(); }),
                    thrownCtor(function () { new Intl.RelativeTimeFormat([], null); }),
                    thrownCtor(function () { new Intl.RelativeTimeFormat('en-US').format(Symbol(), 'second'); }),
                    thrownCtor(function () { new Intl.DurationFormat([undefined]); }),
                    splitSpecies
                ].join('|');
            })();
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void String_Intl_And_Iterator_Abrupt_Getters_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.search, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.search(obj);
                    }),
                    thrownCtor(function () {
                        var obj = {};
                        Object.defineProperty(obj, Symbol.split, {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        ''.split(obj);
                    }),
                    thrownCtor(function () {
                        var iterable = {
                            [Symbol.iterator]: function () {
                                var iterator = {
                                    next: function () {
                                        return { done: false, value: 1 };
                                    },
                                    return: function () {
                                        throw new Test262Error();
                                    }
                                };

                                return iterator;
                            }
                        };

                        var value;
                        [value] = iterable;
                    }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            type: 'language',
                            get fallback() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            type: 'language',
                            get languageDisplay() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            type: 'language',
                            get localeMatcher() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            type: 'language',
                            get style() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.DisplayNames('en', {
                            get type() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.NumberFormat('en', {
                            get roundingIncrement() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.NumberFormat('en', {
                            get roundingMode() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.NumberFormat('en', {
                            get roundingPriority() {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        new Intl.NumberFormat('en', {
                            get trailingZeroDisplay() {
                                throw new Test262Error();
                            }
                        });
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error|Test262Error",
            result.ToString());
    }

    [Fact]
    public void RegExp_Species_And_Destructuring_Assignment_Abrupt_Completions_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var unicodeResult = thrownCtor(function () {
                    var re = /./;
                    Object.defineProperty(re, 'unicode', {
                        get: function () {
                            throw new Test262Error();
                        }
                    });

                    re[Symbol.match]('');
                });

                var matchAllSpecies = thrownCtor(function () {
                    var regexp = /./;
                    regexp.constructor = {
                        [Symbol.species]: function () {
                            throw new Test262Error();
                        }
                    };

                    regexp[Symbol.matchAll]('');
                });

                var splitSpecies = thrownCtor(function () {
                    var re = /x/;
                    re.constructor = function () {};
                    re.constructor[Symbol.species] = function () {
                        throw new Test262Error();
                    };

                    re[Symbol.split]();
                });

                var destructuringAssignment = thrownCtor(function () {
                    var target = {
                        set y(val) {
                            throw new Test262Error();
                        }
                    };

                    0, { a: target.y } = { a: 23 };
                });

                return [
                    unicodeResult,
                    matchAllSpecies,
                    splitSpecies,
                    destructuringAssignment
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|Test262Error|Test262Error|Test262Error", result.ToString());
    }

    [Fact]
    public void Reflect_And_RegExp_Abrupt_Completions_From_Test262_Are_Preserved()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () {
                        var key = {
                            toString: function () {
                                throw new Test262Error();
                            }
                        };

                        Reflect.defineProperty({}, key);
                    }),
                    thrownCtor(function () {
                        var pattern = Object.defineProperty({}, 'constructor', {
                            get: function () {
                                throw new Test262Error();
                            }
                        });

                        pattern[Symbol.match] = true;
                        RegExp(pattern);
                    }),
                    thrownCtor(function () {
                        var obj = {
                            get [Symbol.match]() {
                                throw new Test262Error();
                            }
                        };

                        RegExp.prototype[Symbol.matchAll].call(obj, '');
                    }),
                    thrownCtor(function () {
                        var obj = {
                            toString: function () {
                                throw new Test262Error();
                            }
                        };

                        RegExp.prototype[Symbol.matchAll].call(obj, '');
                    })
                ].join('|');
            })();
            """);

        Assert.Equal("Test262Error|Test262Error|Test262Error|Test262Error", result.ToString());
    }

    [Fact]
    public void RegExp_Search_And_Split_Generic_Observable_Steps_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var searchGetLastIndex = thrownCtor(function () {
                    RegExp.prototype[Symbol.search].call({
                        get lastIndex() {
                            throw new Test262Error();
                        }
                    });
                });

                var searchExec = {
                    lastIndex: 86,
                    exec: function () {
                        throw new Test262Error();
                    }
                };
                var searchMatchErr = thrownCtor(function () {
                    RegExp.prototype[Symbol.search].call(searchExec);
                });

                var searchRestore = (function () {
                    var latestValue = 86;
                    var callCount = 0;
                    var fakeRe = {
                        get lastIndex() {
                            return latestValue;
                        },
                        set lastIndex(_) {
                            latestValue = _;
                        },
                        exec: function () {
                            callCount++;
                            latestValue = null;
                            return null;
                        }
                    };

                    return [
                        String(RegExp.prototype[Symbol.search].call(fakeRe)),
                        String(latestValue),
                        String(callCount)
                    ].join(',');
                })();

                var splitSpeciesErr = thrownCtor(function () {
                    var poisonedSpecies = function () {};
                    Object.defineProperty(poisonedSpecies, Symbol.species, {
                        get: function () {
                            throw new Test262Error();
                        }
                    });

                    RegExp.prototype[Symbol.split].call({ constructor: poisonedSpecies }, 'a');
                });

                var splitFlagsErr = thrownCtor(function () {
                    RegExp.prototype[Symbol.split].call({
                        constructor: function () {},
                        flags: {
                            toString: function () {
                                throw new Test262Error();
                            }
                        }
                    });
                });

                var splitSymbolFlagsErr = thrownCtor(function () {
                    RegExp.prototype[Symbol.split].call({
                        constructor: function () {},
                        flags: Symbol.split
                    });
                });

                var splitMatchErr = thrownCtor(function () {
                    var obj = {
                        constructor: function () {}
                    };
                    obj.constructor[Symbol.species] = function () {
                        return {
                            exec: function () {
                                throw new Test262Error();
                            }
                        };
                    };

                    RegExp.prototype[Symbol.split].call(obj, 'a');
                });

                var splitGetLastIndexErr = (function () {
                    var obj = {
                        constructor: function () {}
                    };
                    var callCount = 0;
                    obj.constructor[Symbol.species] = function () {
                        return {
                            set lastIndex(_) {},
                            get lastIndex() {
                                throw new Test262Error();
                            },
                            exec: function () {
                                callCount++;
                                return [];
                            }
                        };
                    };

                    return [
                        thrownCtor(function () {
                            RegExp.prototype[Symbol.split].call(obj, 'abcd');
                        }),
                        String(callCount)
                    ].join(',');
                })();

                var splitLastIndexSequence = (function () {
                    var obj = {
                        constructor: function () {}
                    };
                    var lastIndex = 0;
                    var indices = '';
                    obj.constructor[Symbol.species] = function () {
                        return {
                            set lastIndex(val) {
                                lastIndex = val;
                                indices += val + ',';
                            },
                            get lastIndex() {
                                return lastIndex;
                            },
                            exec: function () {
                                lastIndex += 1;
                                return ['a'];
                            }
                        };
                    };

                    RegExp.prototype[Symbol.split].call(obj, 'abcd');
                    return indices;
                })();

                return [
                    searchGetLastIndex,
                    searchMatchErr,
                    String(searchExec.lastIndex),
                    searchRestore,
                    splitSpeciesErr,
                    splitFlagsErr,
                    splitSymbolFlagsErr,
                    splitMatchErr,
                    splitGetLastIndexErr,
                    splitLastIndexSequence
                ].join('|');
            })();
            """);

        Assert.Equal(
            "Test262Error|Test262Error|0|-1,86,1|Test262Error|Test262Error|TypeError|Test262Error|Test262Error,1|0,1,2,3,",
            result.ToString());
    }

    [Fact]
    public void Script_Host_Test262_Style_BuiltIns_Preserve_Abrupt_Completions_And_Are_Exposed()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function Test262Error(message) {
                    this.message = message || '';
                }

                Test262Error.prototype.toString = function () {
                    return 'Test262Error: ' + this.message;
                };

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e && e.constructor && e.constructor.name;
                    }
                }

                var copyWithinPropType = 'unreached';
                var proxy = new Proxy({ 42: true, length: 43 }, {
                    deleteProperty: function (_target, prop) {
                        copyWithinPropType = typeof prop + ':' + prop;
                        throw new Test262Error();
                    }
                });

                var genericReplace = {
                    flags: 'g',
                    global: true
                };
                Object.defineProperty(genericReplace, 'exec', {
                    get: function () {
                        throw new Test262Error();
                    }
                });

                var genericMatch = {
                    flags: 'g',
                    global: true
                };
                Object.defineProperty(genericMatch, 'exec', {
                    get: function () {
                        throw new Test262Error();
                    }
                });

                return [
                    typeof Reflect.getPrototypeOf,
                    thrownCtor(function () {
                        Reflect.getPrototypeOf(new Proxy({}, {
                            getPrototypeOf: function () {
                                throw new Test262Error();
                            }
                        }));
                    }),
                    typeof Promise.race,
                    thrownCtor(function () {
                        Promise.race.call(function () {
                            throw new Test262Error();
                        });
                    }),
                    thrownCtor(function () {
                        Array.prototype.copyWithin.call(proxy, 42, 0);
                    }),
                    copyWithinPropType,
                    typeof RegExp.prototype[Symbol.replace],
                    thrownCtor(function () {
                        /./[Symbol.replace]({
                            toString: function () {
                                throw new Test262Error();
                            }
                        });
                    }),
                    thrownCtor(function () {
                        RegExp.prototype[Symbol.match].call(genericMatch, '');
                    }),
                    thrownCtor(function () {
                        RegExp.prototype[Symbol.replace].call(genericReplace, '', '');
                    }),
                    thrownCtor(function () {
                        var re = /./;
                        re.exec = function () {
                            return {
                                length: 2,
                                1: {
                                    toString: function () {
                                        throw new Test262Error();
                                    }
                                }
                            };
                        };

                        re[Symbol.replace]('a', 'b');
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "function|Test262Error|function|Test262Error|Test262Error|string:42|function|Test262Error|Test262Error|Test262Error|Test262Error",
            result.ToString());
    }

    [Fact]
    public void MatchAll_Set_Delete_RegExp_And_Proxy_TypeErrors_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () {
                        var toString = RegExp.prototype.toString;
                        toString();
                    }),
                    thrownCtor(function () {
                        String.raw({ raw: ['a', 'b', 'c'] }, '', Symbol(''));
                    }),
                    thrownCtor(function () {
                        var regex = /a/g;
                        Object.defineProperty(regex, 'flags', { value: undefined });
                        ''.matchAll(regex);
                    }),
                    (function () {
                        var regex = /./g;
                        regex[Symbol.matchAll] = 1;
                        return thrownCtor(function () {
                            ''.matchAll(regex);
                        });
                    })(),
                    (function () {
                        var original = Object.getOwnPropertyDescriptor(RegExp.prototype, Symbol.matchAll);
                        try {
                            delete RegExp.prototype[Symbol.matchAll];
                            return thrownCtor(function () {
                                ''.matchAll(/./g);
                            });
                        } finally {
                            Object.defineProperty(RegExp.prototype, Symbol.matchAll, original);
                        }
                    })(),
                    (function () {
                        var sym = Symbol();
                        var obj = {};
                        Object.defineProperty(obj, sym, { value: 1 });
                        return thrownCtor(function () {
                            'use strict';
                            delete obj[sym];
                        });
                    })(),
                    (function () {
                        var original = Object.getOwnPropertyDescriptor(Set.prototype, 'add');
                        try {
                            Object.defineProperty(Set.prototype, 'add', { value: null, configurable: true });
                            return thrownCtor(function () {
                                new Set([1, 2]);
                            });
                        } finally {
                            Object.defineProperty(Set.prototype, 'add', original);
                        }
                    })(),
                    thrownCtor(function () {
                        var string = new String('str');
                        var stringTarget = new Proxy(string, {});
                        var stringProxy = new Proxy(stringTarget, {});
                        Object.defineProperty(stringProxy, '0', { value: 'x' });
                    }),
                    thrownCtor(function () {
                        var trapCalls = 0;
                        var proxy = new Proxy({}, {
                            getOwnPropertyDescriptor: function (target, prop) {
                                Object.defineProperty(target, prop, {
                                    configurable: false,
                                    writable: true
                                });

                                trapCalls++;
                                return {
                                    configurable: false,
                                    writable: false
                                };
                            }
                        });

                        Object.getOwnPropertyDescriptor(proxy, 'prop');
                    })
                ].join('|');
            })();
            """);

        Assert.Equal(
            "TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError|TypeError",
            result.ToString());
    }

    [Fact]
    public void RegExp_MatchAll_Iterator_Next_Preserves_Test262_Abrupt_Completions()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var originalExec = RegExp.prototype.exec;
                var iteratorNextType = typeof /./g[Symbol.matchAll]('').next;

                RegExp.prototype.exec = function () {
                    throw new Test262Error();
                };
                var callThrows = (function () {
                    var iter = /./[Symbol.matchAll]('');
                    return thrownCtor(function () {
                        iter.next();
                    });
                })();

                RegExp.prototype.exec = originalExec;

                return [iteratorNextType, callThrows].join('|');
            })();
            """);

        Assert.Equal("function|Test262Error", result.ToString());
    }


    [Fact]
    public void AnnexB_String_Prototype_Metadata_Matches_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function dataDescriptorSummary(object, key) {
                    var descriptor = Object.getOwnPropertyDescriptor(object, key);
                    return [
                        descriptor && descriptor.writable,
                        descriptor && descriptor.enumerable,
                        descriptor && descriptor.configurable
                    ].join(',');
                }

                function functionMetadata(fn) {
                    var lengthDescriptor = Object.getOwnPropertyDescriptor(fn, 'length');
                    var nameDescriptor = Object.getOwnPropertyDescriptor(fn, 'name');
                    return [
                        fn.length,
                        lengthDescriptor && lengthDescriptor.writable,
                        lengthDescriptor && lengthDescriptor.enumerable,
                        lengthDescriptor && lengthDescriptor.configurable,
                        fn.name,
                        nameDescriptor && nameDescriptor.writable,
                        nameDescriptor && nameDescriptor.enumerable,
                        nameDescriptor && nameDescriptor.configurable
                    ].join(',');
                }

                return [
                    typeof String.prototype.fixed,
                    functionMetadata(String.prototype.fixed),
                    functionMetadata(String.prototype.substr),
                    String.prototype.trimRight === String.prototype.trimEnd,
                    dataDescriptorSummary(String.prototype, 'trimRight'),
                    functionMetadata(String.prototype.trimRight)
                ].join('|');
            })();
            """);

        Assert.Equal(
            "function|0,false,false,true,fixed,false,false,true|2,false,false,true,substr,false,false,true|true|true,false,true|0,false,false,true,trimEnd,false,false,true",
            result.ToString());
    }

    [Fact]
    public void BuiltIn_Function_Lengths_Bind_Call_Apply_Keys()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var parts = [];
            var d1 = Object.getOwnPropertyDescriptor(Function.prototype.bind, 'length');
            parts.push('bind.own=' + (d1 ? d1.value : 'none'));
            parts.push('bind.get=' + Function.prototype.bind.length);

            var d2 = Object.getOwnPropertyDescriptor(Function.prototype.call, 'length');
            parts.push('call.own=' + (d2 ? d2.value : 'none'));
            parts.push('call.get=' + Function.prototype.call.length);

            var d3 = Object.getOwnPropertyDescriptor(Function.prototype.apply, 'length');
            parts.push('apply.own=' + (d3 ? d3.value : 'none'));
            parts.push('apply.get=' + Function.prototype.apply.length);

            var d4 = Object.getOwnPropertyDescriptor(Object.keys, 'length');
            parts.push('keys.own=' + (d4 ? d4.value : 'none'));
            parts.push('keys.get=' + Object.keys.length);

            parts.join(';');
        ").ToString();
        // Expected: all should have own length matching spec values
        Assert.Equal(
            "bind.own=1;bind.get=1;call.own=1;call.get=1;apply.own=2;apply.get=2;keys.own=1;keys.get=1",
            result);
    }

    [Fact]
    public void Array_DefineProperty_Invalid_Length_Throws_RangeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try { fn(); return 'no-throw'; }
                    catch (e) { return e.constructor.name; }
                }
                return [
                    thrownCtor(function () { Object.defineProperty([], 'length', { value: -1 }); }),
                    thrownCtor(function () { Object.defineProperty([], 'length', { value: 4294967296 }); }),
                    thrownCtor(function () { Object.defineProperty([], 'length', { value: 1.5 }); }),
                    thrownCtor(function () { Object.defineProperty([], 'length', { value: NaN }); })
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|RangeError|RangeError|RangeError", result.ToString());
    }

    [Fact]
    public void BuiltIn_Length_Metadata_Matches_Optional_Argument_Specs()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                function lengthOf(fn) {
                    var descriptor = Object.getOwnPropertyDescriptor(fn, 'length');
                    return String(descriptor && descriptor.value) + '/' + String(fn.length);
                }

                function nextLengthOf(iteratorFactory) {
                    var proto = Object.getPrototypeOf(iteratorFactory());
                    var next = Object.getOwnPropertyDescriptor(proto, 'next').value;
                    var descriptor = Object.getOwnPropertyDescriptor(next, 'length');
                    return String(descriptor && descriptor.value) + '/' + String(next.length);
                }

                return [
                    lengthOf(Array.prototype.values),
                    lengthOf(Uint8Array.prototype.values),
                    lengthOf(ArrayBuffer.prototype.transfer),
                    lengthOf(ArrayBuffer.prototype.transferToFixedLength),
                    lengthOf(String.prototype.normalize),
                    lengthOf(Number.prototype.toLocaleString),
                    lengthOf(Uint8Array.prototype.toBase64),
                    nextLengthOf(function () { return [][Symbol.iterator](); }),
                    nextLengthOf(function () { return new Map([[1, 2]]).values(); }),
                    nextLengthOf(function () { return new Set([1]).values(); })
                ].join('|');
            })();
            """);

        Assert.Equal("0/0|0/0|0/0|0/0|0/0|0/0|0/0|0/0|0/0|0/0", result.ToString());
    }

    [Fact]
    public void DateTimeFormat_FormatToParts_NaN_Throws_RangeError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try { fn(); return 'no-throw'; }
                    catch (e) { return e.constructor.name; }
                }
                var dtf = new Intl.DateTimeFormat();
                return [
                    thrownCtor(function () { dtf.formatToParts(NaN); }),
                    typeof dtf.formatToParts
                ].join('|');
            })();
            """);

        Assert.Equal("RangeError|function", result.ToString());
    }

    [Fact]
    public void Iterator_Prototype_Constructor_Is_Function()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Iterator.prototype.constructor,
                    Iterator.prototype.constructor === Iterator
                ].join('|');
            })();
            """);

        Assert.Equal("function|true", result.ToString());
    }

    [Fact]
    public void Iterator_Prototype_Next_And_Return_Work_For_BuiltIn_Iterators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function* g() {
                    yield 1;
                    yield 2;
                }

                var arrayIterator = [1, 2][Symbol.iterator]();
                var generator = g();
                var nextResult = Iterator.prototype.next.call(arrayIterator);
                var returnResult = Iterator.prototype.return.call(generator, 99);

                return [
                    nextResult.value,
                    nextResult.done,
                    returnResult.value,
                    returnResult.done
                ].join('|');
            })();
            """);

        Assert.Equal("1|false|99|true", result.ToString());
    }

    [Fact]
    public void Intl_ListFormat_Prototype_Methods_Exist()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.ListFormat.prototype.format,
                    typeof Intl.ListFormat.prototype.formatToParts,
                    typeof Intl.ListFormat.prototype.resolvedOptions
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|function", result.ToString());
    }

    [Fact]
    public void Intl_Locale_Prototype_Methods_Exist()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.Locale.prototype.maximize,
                    typeof Intl.Locale.prototype.minimize,
                    typeof Intl.Locale.prototype.getCalendars,
                    typeof Intl.Locale.prototype.getCollations,
                    typeof Intl.Locale.prototype.getHourCycles,
                    typeof Intl.Locale.prototype.getNumberingSystems,
                    typeof Intl.Locale.prototype.getTextInfo,
                    typeof Intl.Locale.prototype.getTimeZones,
                    typeof Intl.Locale.prototype.getWeekInfo
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|function|function|function|function|function|function|function", result.ToString());
    }

    [Fact]
    public void Intl_RelativeTimeFormat_SupportedLocalesOf_And_FormatToParts_Exist()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.RelativeTimeFormat.supportedLocalesOf,
                    typeof Intl.RelativeTimeFormat.prototype.formatToParts,
                    typeof Intl.RelativeTimeFormat.prototype.resolvedOptions
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|function", result.ToString());
    }

    [Fact]
    public void Intl_Segmenter_Prototype_Methods_And_SupportedLocalesOf_Exist()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.Segmenter.supportedLocalesOf,
                    typeof Intl.Segmenter.prototype.resolvedOptions,
                    typeof Intl.Segmenter.prototype.segment
                ].join('|');
            })();
            """);

        Assert.Equal("function|function|function", result.ToString());
    }

    [Fact]
    public void Intl_ResolvedOptions_Methods_Are_Exposed_And_Extensible()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"[
            typeof Intl.DateTimeFormat.prototype.resolvedOptions,
            typeof Intl.NumberFormat.prototype.resolvedOptions,
            typeof Intl.PluralRules.prototype.resolvedOptions,
            Object.isExtensible(Intl.DateTimeFormat.prototype.resolvedOptions),
            Object.isExtensible(Intl.NumberFormat.prototype.resolvedOptions),
            Object.isExtensible(Intl.PluralRules.prototype.resolvedOptions)
        ].join('|');");

        Assert.Equal("function|function|function|true|true|true", result.ToString());
    }

    [Fact]
    public void Intl_NumberFormat_FormatRange_Methods_Exist()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.NumberFormat.prototype.formatRange,
                    typeof Intl.NumberFormat.prototype.formatRangeToParts
                ].join('|');
            })();
            """);

        Assert.Equal("function|function", result.ToString());
    }

    [Fact]
    public void Intl_PluralRules_SelectRange_Exists()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return [
                    typeof Intl.PluralRules.prototype.selectRange,
                    typeof Intl.PluralRules.supportedLocalesOf
                ].join('|');
            })();
            """);

        Assert.Equal("function|function", result.ToString());
    }

    [Fact]
    public void Strict_Eval_Var_Does_Not_Leak()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              eval("'use strict'; var strictEvalVar = 42;");
              return typeof strictEvalVar;
            })()
            """);
        Assert.Equal("undefined", r.ToString());
    }

    [Fact]
    public void Direct_Eval_Function_Local_Declarations_Are_Hoisted_And_Update_Existing_Bindings()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            [
              (function() {
                var after;
                eval('{ function f() { return "inner declaration"; } }after = f; function f() { return "outer declaration"; }');
                return typeof after + "|" + after();
              }()),
              (function() {
                var f = 88;
                var initial;
                eval('initial = f; function f() { return 33; }');
                return typeof initial + "|" + initial();
              }())
            ].join("||")
            """);

        Assert.Equal("function|inner declaration||function|33", result.ToString());
    }

    [Fact]
    public void Strict_Mode_Delete_NonConfigurable_Throws()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              var o2 = Object.freeze({noconfig: 'ow'});
              try { eval("'use strict'; delete o2.noconfig"); return 'no-throw'; }
              catch(e) { return e instanceof TypeError ? 'TypeError' : e.constructor.name; }
            })()
            """);
        Assert.Equal("TypeError", r.ToString());
    }

    [Fact]
    public void Strict_Function_In_If_Is_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              try { eval("'use strict'; if (true) function f() {}"); return 'no-throw'; }
              catch(e) { return e instanceof SyntaxError ? 'SyntaxError' : e.constructor.name; }
            })()
            """);
        Assert.Equal("SyntaxError", r.ToString());
    }

    [Fact]
    public void Strict_Assign_String_Length_Throws()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              try { eval("'use strict'; var s = new String('foo'); s.length = 1;"); return 'no-throw'; }
              catch(e) { return e instanceof TypeError ? 'TypeError' : e.constructor.name; }
            })()
            """);
        Assert.Equal("TypeError", r.ToString());
    }

    [Fact]
    public void NonStrict_Assign_String_Length_Silently_Fails()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              var s = new String('foo');
              s.length = 1;
              return s.length;
            })()
            """);
        Assert.Equal(3d, r.DoubleValue);
    }

    [Fact]
    public void IsRegExpLike_Accesses_Symbol_Match()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              var accessed = false;
              var rx = /a/;
              Object.defineProperty(rx, Symbol.match, {
                get: function() { accessed = true; return undefined; }
              });
              rx[Symbol.split]("abba");
              return accessed;
            })()
            """);
        Assert.True(r.BooleanValue);
    }

    [Fact]
    public void String_Keyed_Shorthand_Destructuring_Is_SyntaxError()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() {
              try { new Function("({''})"); return 'no-error'; }
              catch(e) { return e instanceof SyntaxError ? 'SyntaxError' : e.constructor.name; }
            })()
            """);
        Assert.Equal("SyntaxError", r.ToString());
    }

    [Fact]
    public void RegExp_Unicode_Word_Boundary_Matches_Long_S()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() { return /\b/iu.test('\u017F'); })()
            """);
        Assert.True(r.BooleanValue);
    }

    [Fact]
    public void RegExp_IgnoreCase_Unicode_CaseFolding_Mu_Micro()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() { return /\u039C/i.test('\xB5'); })()
            """);
        Assert.True(r.BooleanValue);
    }

    [Fact]
    public void RegExp_IgnoreCase_Unicode_CaseFolding_Long_S()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var r = ctx.Eval("""
            (function() { return /\u017F/iu.test('s'); })()
            """);
        Assert.True(r.BooleanValue);
    }

    // ----------------------------------------------------------------
    // Seeded property-based parameterized tests (recommendation #5 from
    // docs/compliance/testsuite-optimization.md).  Each fixture generates
    // inputs from a fixed seed so failures are reproducible and surface
    // under the existing `dotnet test` evidence command.
    // ----------------------------------------------------------------

    #region Property-based: JSON.parse error mapping

    public static TheoryData<int, string> JsonParseErrorInputs()
    {
        var data = new TheoryData<int, string>();
        var rng = new Random(20260525);
        // Deterministic corpus of malformed JSON strings.
        string[] templates = [
            "{0}",          // bare token
            "{{\"a\": {0}}}", // value position
            "[{0}]",        // array element
            "{{\"a\": [{0}]}}", // nested array element
        ];
        for (int seed = 0; seed < 20; seed++)
        {
            // Generate a random bad token: control chars, truncated
            // strings, unmatched braces, bare identifiers, etc.
            string bad = (rng.Next(6)) switch
            {
                0 => new string((char)rng.Next(0, 32), rng.Next(1, 4)),
                1 => "\"unterminated",
                2 => "{\"a\":",
                3 => "[,]",
                4 => "undefined",
                _ => "NaN",
            };
            string template = templates[rng.Next(templates.Length)];
            data.Add(seed, string.Format(template, bad));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(JsonParseErrorInputs))]
    public void JsonParse_Malformed_Throws_SyntaxError_Seeded(int seed, string badJson)
    {
        _ = seed; // recorded in test name for reproducibility
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var escaped = badJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
        var result = ctx.Eval($$"""
            (function() {
                try { JSON.parse('{{escaped}}'); return 'no-error'; }
                catch (e) { return e.constructor.name; }
            })()
            """);
        Assert.Equal("SyntaxError", result.ToString());
    }

    #endregion

    #region Property-based: property-key interning

    public static TheoryData<int, string, string> PropertyKeyInputs()
    {
        var data = new TheoryData<int, string, string>();
        var rng = new Random(20260525);
        for (int seed = 0; seed < 15; seed++)
        {
            // Generate varied property key names: numeric-like, unicode,
            // short, long, with special chars.
            string key = (rng.Next(5)) switch
            {
                0 => rng.Next(0, 1000).ToString(), // numeric string key
                1 => $"prop_{seed}_{rng.Next(100)}", // normal identifier
                2 => $"\u00e9_{seed}", // unicode key
                3 => $"__proto{seed}__", // proto-like
                _ => new string('x', rng.Next(1, 30)), // variable length
            };
            string value = rng.Next(1000).ToString();
            data.Add(seed, key, value);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(PropertyKeyInputs))]
    public void PropertyKey_Roundtrip_Preserves_Value_Seeded(int seed, string key, string value)
    {
        _ = seed;
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var escapedKey = key.Replace("\\", "\\\\").Replace("'", "\\'");
        var result = ctx.Eval($$"""
            (function() {
                var obj = {};
                obj['{{escapedKey}}'] = {{value}};
                return String(obj['{{escapedKey}}']);
            })()
            """);
        Assert.Equal(value, result.ToString());
    }

    #endregion

    #region Property-based: array mutator observable steps

    public static TheoryData<int, int> ArrayReverseInputs()
    {
        var data = new TheoryData<int, int>();
        var rng = new Random(20260525);
        for (int seed = 0; seed < 10; seed++)
        {
            data.Add(seed, rng.Next(0, 20));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ArrayReverseInputs))]
    public void Array_Reverse_Preserves_Elements_Seeded(int seed, int length)
    {
        _ = seed;
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var result = ctx.Eval($$"""
            (function() {
                var arr = [];
                for (var i = 0; i < {{length}}; i++) arr.push(i);
                var original = arr.slice();
                arr.reverse();
                if (arr.length !== original.length) return 'length mismatch';
                for (var j = 0; j < arr.length; j++) {
                    if (arr[j] !== original[original.length - 1 - j]) return 'mismatch at ' + j;
                }
                return 'ok';
            })()
            """);
        Assert.Equal("ok", result.ToString());
    }

    #endregion

    #region Property-based: Object.keys/values roundtrip

    public static TheoryData<int, int> ObjectKeysInputs()
    {
        var data = new TheoryData<int, int>();
        var rng = new Random(20260525);
        for (int seed = 0; seed < 10; seed++)
        {
            data.Add(seed, rng.Next(0, 15));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ObjectKeysInputs))]
    public void Object_Keys_Returns_All_Own_Enumerable_Properties_Seeded(int seed, int propCount)
    {
        _ = seed;
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var assignments = string.Join(" ", Enumerable.Range(0, propCount).Select(i => $"obj['k{i}'] = {i};"));
        var script = $"(function() {{ var obj = {{}}; {assignments} var keys = Object.keys(obj); return keys.length === {propCount} ? 'ok' : 'expected {propCount} got ' + keys.length; }})()";
        var result = ctx.Eval(script);
        Assert.Equal("ok", result.ToString());
    }

    #endregion

    #region Property-based: RegExp matchAll iterator

    public static TheoryData<int, string, string, int> MatchAllInputs()
    {
        var data = new TheoryData<int, string, string, int>();
        var rng = new Random(20260525);
        string[] patterns = ["a", "\\\\d+", "[bc]", "x"];
        for (int seed = 0; seed < 10; seed++)
        {
            var pattern = patterns[rng.Next(patterns.Length)];
            // Build a random haystack mixing matching and non-matching chars.
            var chars = new char[rng.Next(5, 25)];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = "abcd0123xyz"[rng.Next(11)];
            var haystack = new string(chars);
            // Count expected matches by running a simple scan — the test
            // verifies that matchAll returns an iterable with .next().
            data.Add(seed, pattern, haystack, -1); // -1 = don't assert count, just assert iterable
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(MatchAllInputs))]
    public void RegExp_MatchAll_Returns_Iterable_Iterator_Seeded(int seed, string pattern, string haystack, int _expectedHint)
    {
        _ = seed;
        _ = _expectedHint;
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext();
        var escapedHaystack = haystack.Replace("\\", "\\\\").Replace("'", "\\'");
        var result = ctx.Eval($$"""
            (function() {
                var re = new RegExp('{{pattern}}', 'g');
                var iter = '{{escapedHaystack}}'.matchAll(re);
                if (typeof iter.next !== 'function') return 'next is not a function';
                var count = 0;
                var r = iter.next();
                while (!r.done) { count++; r = iter.next(); }
                return 'ok:' + count;
            })()
            """);
        Assert.StartsWith("ok:", result.ToString());
    }

    #endregion

    [Fact]
    public void BuiltIn_Prototypes_Have_Symbol_ToStringTag_As_Own_Property()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(@"(function() {
            function hasTag(obj, expected) {
                var desc = Object.getOwnPropertyDescriptor(obj, Symbol.toStringTag);
                if (!desc) return 'missing:' + expected;
                if (desc.value !== expected) return 'wrong:' + expected + ':' + desc.value;
                if (desc.writable !== false) return 'writable:' + expected;
                if (desc.enumerable !== false) return 'enumerable:' + expected;
                if (desc.configurable !== true) return 'not-configurable:' + expected;
                return 'ok';
            }
            var results = [];
            // Core built-ins
            results.push(hasTag(BigInt.prototype, 'BigInt'));
            results.push(hasTag(Reflect, 'Reflect'));
            results.push(hasTag(WeakRef.prototype, 'WeakRef'));
            results.push(hasTag(FinalizationRegistry.prototype, 'FinalizationRegistry'));
            results.push(hasTag(Symbol.prototype, 'Symbol'));
            results.push(hasTag(Map.prototype, 'Map'));
            results.push(hasTag(Set.prototype, 'Set'));
            results.push(hasTag(Promise.prototype, 'Promise'));
            // Generator / AsyncGenerator
            var GeneratorFunction = Object.getPrototypeOf(function*(){}).constructor;
            results.push(hasTag(GeneratorFunction.prototype, 'GeneratorFunction'));
            var genProto = Object.getPrototypeOf((function*(){})());
            results.push(hasTag(genProto, 'Generator'));
            var AsyncGeneratorFunction = Object.getPrototypeOf(async function*(){}).constructor;
            results.push(hasTag(AsyncGeneratorFunction.prototype, 'AsyncGeneratorFunction'));
            // Intl
            results.push(hasTag(Intl, 'Intl'));
            results.push(hasTag(Intl.DateTimeFormat.prototype, 'Intl.DateTimeFormat'));
            results.push(hasTag(Intl.NumberFormat.prototype, 'Intl.NumberFormat'));
            results.push(hasTag(Intl.PluralRules.prototype, 'Intl.PluralRules'));
            results.push(hasTag(Intl.RelativeTimeFormat.prototype, 'Intl.RelativeTimeFormat'));
            results.push(hasTag(Intl.Locale.prototype, 'Intl.Locale'));
            results.push(hasTag(Intl.ListFormat.prototype, 'Intl.ListFormat'));
            results.push(hasTag(Intl.DurationFormat.prototype, 'Intl.DurationFormat'));
            results.push(hasTag(Intl.DisplayNames.prototype, 'Intl.DisplayNames'));
            results.push(hasTag(Intl.Segmenter.prototype, 'Intl.Segmenter'));
            return results.join('|');
        })();");

        var parts = result.ToString().Split('|');
        foreach (var part in parts)
        {
            Assert.Equal("ok", part);
        }
    }

    private static void EnsureBuiltInsLoaded()
    {
        // Load CLR assembly so JSEngine.ClrInterop is properly configured
        // (required for JSConsole marshalling via ClrProxy).
        RuntimeHelpers.RunClassConstructor(
            typeof(Clr.DefaultClrInterop).TypeHandle);
        RuntimeHelpers.RunClassConstructor(
            typeof(Weak.JSWeakRef).TypeHandle);
    }

    #region RegExp_Unicode_Surrogate_Pairs

    [Theory]
    [InlineData("/^.$/u.test('\\uD83D\\uDCA9')", true, "dot with u flag matches surrogate pair")]
    [InlineData("/^..$/u.test('\\uD83D\\uDCA9\\uD83D\\uDE00')", true, "two dots with u match two surrogate pairs")]
    [InlineData("/\\uD834\\uDF06/u.test('\\uD834\\uDF06')", true, "surrogate pair escape matches with u")]
    [InlineData("/[\\uD834\\uDF06]/u.test('\\uD834\\uDF06')", true, "surrogate pair in char class matches with u")]
    public void RegExp_Unicode_SurrogatePairs(string expression, bool expected, string description)
    {
        _ = description;
        using var ctx = new JSContext();
        var result = ctx.Eval(expression);
        Assert.Equal(expected, result.BooleanValue);
    }

    #endregion

    #region Not-A-Constructor Built-ins Exist As Functions

    [Theory]
    [InlineData("typeof Array.prototype.toSpliced", "function")]
    [InlineData("typeof Array.prototype.with", "function")]
    [InlineData("typeof ArrayBuffer.prototype.sliceToImmutable", "function")]
    [InlineData("typeof DataView.prototype.getBigUint64", "function")]
    [InlineData("typeof DataView.prototype.setBigUint64", "function")]
    [InlineData("typeof JSON.rawJSON", "function")]
    [InlineData("typeof JSON.isRawJSON", "function")]
    [InlineData("typeof Promise.allKeyed", "function")]
    [InlineData("typeof WeakSet.prototype.has", "function")]
    [InlineData("typeof Map.prototype[Symbol.iterator]", "function")]
    public void BuiltIn_Methods_Are_Functions(string expression, string expected)
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval(expression);
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void JsonRawJson_Rejects_Empty_And_Whitespace_Bounded_Input()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            [
              '', '\n123', '123\n', '\t123', '123\t', '\r123', '123\r', ' 123', '123 '
            ].map(value => {
              try {
                JSON.rawJSON(value);
                return 'no error';
              } catch (e) {
                return e.name;
              }
            }).join('|');
            """);
        Assert.Equal("SyntaxError|SyntaxError|SyntaxError|SyntaxError|SyntaxError|SyntaxError|SyntaxError|SyntaxError|SyntaxError", result.ToString());
    }

    [Fact]
    public void Array_ToSpliced_Returns_New_Array()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var a = [1, 2, 3, 4, 5];
            var b = a.toSpliced(1, 2, 'a', 'b');
            [a.join(','), b.join(',')].join('|');
            """);
        Assert.Equal("1,2,3,4,5|1,a,b,4,5", result.ToString());
    }

    [Fact]
    public void Array_With_Returns_New_Array()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var a = [1, 2, 3];
            var b = a.with(1, 'x');
            [a.join(','), b.join(',')].join('|');
            """);
        Assert.Equal("1,2,3|1,x,3", result.ToString());
    }

    [Fact]
    public void Generator_Prototype_Has_Next_Return_Throw()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function() {
                function* g() { yield 1; }
                var gen = g();
                return [
                    typeof gen.next,
                    typeof gen.return,
                    typeof gen.throw
                ].join('|');
            })();
            """);
        Assert.Equal("function|function|function", result.ToString());
    }

    [Fact]
    public void Date_BigInt_And_IteratorConcat_Regressions_Match_Test262()
    {
        EnsureBuiltInsLoaded();
        using var ctx = CreateContext(JavaScriptFeatureFlags.IteratorConcat);

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var dateCallIgnoresArgument = thrownCtor(function () {
                    var poisonedObject = Object.defineProperty({}, Symbol.toPrimitive, {
                        get: function () {
                            throw new Test262Error();
                        }
                    });

                    Date(poisonedObject);
                });

                var copiedDateSkipsToPrimitive = (function () {
                    var poisonedDate = new Date(1234);
                    Object.defineProperty(poisonedDate, Symbol.toPrimitive, {
                        get: function () {
                            throw new Test262Error();
                        }
                    });

                    return String(new Date(poisonedDate).valueOf() === 1234);
                })();

                var bigintNullToPrimitiveFallsBack = (function () {
                    return String(({
                        [Symbol.toPrimitive]: null,
                        valueOf: function () {
                            return 2n;
                        }
                    } + 1n) === 3n);
                })();

                var concatReturnBeforeStart = thrownCtor(function () {
                    var iterator = Iterator.concat({
                        [Symbol.iterator]() {
                            return {
                                next() {
                                    throw new Test262Error();
                                },
                                return() {
                                    throw new Test262Error();
                                }
                            };
                        }
                    });

                    iterator.return();
                    iterator.next();
                    iterator.return();
                });

                return [
                    dateCallIgnoresArgument,
                    copiedDateSkipsToPrimitive,
                    bigintNullToPrimitiveFallsBack,
                    concatReturnBeforeStart
                ].join('|');
            })();
            """);

        Assert.Equal("no-throw|true|true|no-throw", result.ToString());
    }

    [Fact]
    public void YieldStar_Abrupt_Completions_Are_Catchable_In_Sync_Generators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                class Test262Error extends Error {}

                function getIteratorAbrupt() {
                    var thrown = new Test262Error();
                    var poisonedIter = Object.defineProperty({}, Symbol.iterator, {
                        get: function() {
                            throw thrown;
                        }
                    });
                    function* g() {
                        try {
                            yield * poisonedIter;
                        } catch (err) {
                            caught = err;
                        }
                    }
                    var iter = g();
                    var caught;

                    var result = iter.next();
                    return String(result.done && result.value === undefined && caught === thrown);
                }

                function nextMethodAbrupt() {
                    var thrown = new Test262Error();
                    var badIter = {};
                    var poisonedNext = Object.defineProperty({}, 'next', {
                        get: function() {
                            throw thrown;
                        }
                    });
                    badIter[Symbol.iterator] = function() {
                        return poisonedNext;
                    };
                    function* g() {
                        try {
                            yield * badIter;
                        } catch (err) {
                            caught = err;
                        }
                    }
                    var iter = g();
                    var caught;

                    var result = iter.next();
                    return String(result.done && result.value === undefined && caught === thrown);
                }

                function returnMethodAbrupt() {
                    var thrown = new Test262Error();
                    var badIter = {};
                    var poisonedReturn = {
                        next: function() {
                            return { done: false };
                        }
                    };
                    Object.defineProperty(poisonedReturn, 'return', {
                        get: function() {
                            throw thrown;
                        }
                    });
                    badIter[Symbol.iterator] = function() {
                        return poisonedReturn;
                    };
                    function* g() {
                        try {
                            yield * badIter;
                        } catch (err) {
                            caught = err;
                        }
                    }
                    var iter = g();
                    var caught;

                    iter.next();
                    var result = iter.return();
                    return String(result.done && result.value === undefined && caught === thrown);
                }

                function missingThrowCloseAbrupt() {
                    var thrown = new Test262Error();
                    var badIter = {};
                    var callCount = 0;
                    var poisonedReturn = {
                        next: function() {
                            return { done: false };
                        }
                    };
                    Object.defineProperty(poisonedReturn, 'throw', {
                        get: function() {
                            callCount += 1;
                        }
                    });
                    Object.defineProperty(poisonedReturn, 'return', {
                        get: function() {
                            throw thrown;
                        }
                    });
                    badIter[Symbol.iterator] = function() {
                        return poisonedReturn;
                    };
                    function* g() {
                        try {
                            yield * badIter;
                        } catch (err) {
                            caught = err;
                        }
                    }
                    var iter = g();
                    var caught;

                    iter.next();
                    var result = iter.throw();
                    return String(result.done && result.value === undefined && caught === thrown && callCount === 1);
                }

                return [
                    getIteratorAbrupt(),
                    nextMethodAbrupt(),
                    returnMethodAbrupt(),
                    missingThrowCloseAbrupt()
                ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true", result.ToString());
    }

    #endregion

    #region Error_Constructor_Names

    [Theory]
    [InlineData("try { eval('???'); } catch(e) { e.constructor.name }", "SyntaxError")]
    [InlineData("try { null.x } catch(e) { e.constructor.name }", "TypeError")]
    [InlineData("try { decodeURIComponent('%'); } catch(e) { e.constructor.name }", "URIError")]
    [InlineData("try { new Array(-1); } catch(e) { e.constructor.name }", "RangeError")]
    [InlineData("try { undeclaredVar123; } catch(e) { e.constructor.name }", "ReferenceError")]
    public void Error_Factory_Produces_Correct_Constructor_Name(string code, string expectedName)
    {
        using var ctx = CreateContext();
        var result = ctx.Eval(code);
        Assert.Equal(expectedName, result.ToString());
    }

    [Theory]
    [InlineData("try { eval('???'); } catch(e) { e instanceof SyntaxError }", "true")]
    [InlineData("try { null.x } catch(e) { e instanceof TypeError }", "true")]
    [InlineData("try { decodeURIComponent('%'); } catch(e) { e instanceof URIError }", "true")]
    [InlineData("try { new Array(-1); } catch(e) { e instanceof RangeError }", "true")]
    [InlineData("try { undeclaredVar123; } catch(e) { e instanceof ReferenceError }", "true")]
    public void Error_Factory_Produces_Correct_InstanceOf(string code, string expected)
    {
        using var ctx = CreateContext();
        var result = ctx.Eval(code);
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void SetFromHex_Invalid_Char_Throws_SyntaxError_With_Partial_Write()
    {
        using var ctx = CreateContext();

        // Invalid hex character should produce SyntaxError
        var result = ctx.Eval("""
            var arr = new Uint8Array(4);
            var error = null;
            try { arr.setFromHex("deadXX00"); } catch(e) { error = e; }
            [error instanceof SyntaxError, arr[0], arr[1]].join(",");
            """);
        Assert.Equal("true,222,173", result.ToString());
    }

    [Fact]
    public void FromHex_Invalid_Char_Throws_SyntaxError()
    {
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            var error = null;
            try { Uint8Array.fromHex("deadXX"); } catch(e) { error = e; }
            error instanceof SyntaxError;
            """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void Indirect_Eval_Parse_Failure_Throws_SyntaxError()
    {
        using var ctx = CreateContext();

        var result = ctx.Eval("""
            var error = null;
            try { (0, eval)('var \\n'); } catch(e) { error = e; }
            error instanceof SyntaxError;
            """);
        Assert.Equal("true", result.ToString());
    }

    #endregion

    #region Proxy HasProperty Prototype Chain

    [Fact]
    public void Proxy_Has_Trap_Invoked_Via_Prototype_Chain()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var proxy = new Proxy({}, {
                has: function(t, p) { return true; }
            });
            var obj = Object.create(proxy);
            ("foo" in obj).toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    #endregion

    #region RegExp DotAll and Unicode

    [Fact]
    public void RegExp_Dot_Excludes_All_LineTerminators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^.$/;
            [r.test("\r"), r.test("\u2028"), r.test("\u2029"), r.test("\n")]
                .every(function(v) { return v === false; }).toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void RegExp_DotAll_Matches_All_LineTerminators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^.$/s;
            [r.test("\r"), r.test("\u2028"), r.test("\u2029"), r.test("\n")]
                .every(function(v) { return v === true; }).toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void RegExp_Unicode_DotAll_Matches_All()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^.$/su;
            [r.test("\r"), r.test("\u2028"), r.test("\u2029"), r.test("\n")]
                .every(function(v) { return v === true; }).toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void RegExp_Unicode_Dot_Excludes_LineTerminators()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^.$/u;
            [r.test("\r"), r.test("\u2028"), r.test("\u2029"), r.test("\n")]
                .every(function(v) { return v === false; }).toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void RegExp_Unicode_NonWhitespace_Matches_Surrogate_Pairs()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^\S$/u;
            r.test("\ud800\udc00").toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    [Fact]
    public void RegExp_Unicode_CharClass_Surrogate_Pairs()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var r = /^[\ud834\udf06]$/u;
            r.test("\ud834\udf06").toString();
        """);
        Assert.Equal("true", result.ToString());
    }

    #endregion

    #region Indirect Eval Global Scope

    [Fact]
    public void Indirect_Eval_Sees_Global_Scope()
    {
        EnsureBuiltInsLoaded();
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            var __x = "str";
            function testcase() {
                var __x = "str1";
                var r = eval("var _eval = eval; _eval('__x')");
                return r;
            }
            testcase();
        """);
        Assert.Equal("str", result.ToString());
    }

    #endregion

    #region DefineProperty TypeError on non-extensible objects

    [Fact]
    public void DefineProperty_Throws_TypeError_On_NonExtensible_PlainObject()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = {};
            Object.preventExtensions(obj);
            Object.defineProperty(obj, 'newProp', { value: 42 });
        """));
    }

    [Fact]
    public void DefineProperties_Allows_Redefining_On_NonExtensible_Object()
    {
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            var obj = { x: 1 };
            Object.preventExtensions(obj);
            Object.defineProperties(obj, { x: { value: 2 } });
            obj.x;
        """);
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void DefineProperty_Throws_TypeError_On_NonExtensible_Symbol()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = {};
            Object.preventExtensions(obj);
            Object.defineProperty(obj, Symbol('test'), { value: 42 });
        """));
    }

    [Fact]
    public void DefineProperty_Throws_TypeError_On_NonExtensible_Index()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = {};
            Object.preventExtensions(obj);
            Object.defineProperty(obj, 0, { value: 42 });
        """));
    }

    #endregion

    #region String.prototype.matchAll flags check

    [Fact]
    public void String_Prototype_Custom_Matchers_Are_Ignored_For_Primitives()
    {
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                Object.defineProperty(Boolean.prototype, Symbol.match, {
                    get: function () { throw new Error('match getter should not be called'); }
                });
                Object.defineProperty(Boolean.prototype, Symbol.split, {
                    get: function () { throw new Error('split getter should not be called'); }
                });
                Object.defineProperty(Number.prototype, Symbol.replace, {
                    get: function () { throw new Error('replace getter should not be called'); }
                });
                Object.defineProperty(BigInt.prototype, Symbol.replace, {
                    get: function () { throw new Error('replaceAll getter should not be called'); }
                });
                Object.defineProperty(String.prototype, Symbol.search, {
                    get: function () { throw new Error('search getter should not be called'); }
                });
                Object.defineProperty(String.prototype, Symbol.matchAll, {
                    get: function () { throw new Error('matchAll getter should not be called'); }
                });

                return [
                    JSON.stringify('atruebtruec'.match(true)),
                    'a1b1c'.replace(1, 'X'),
                    'a1b1c'.replaceAll(1n, 'X'),
                    String('a1b1c'.search('1')),
                    JSON.stringify('atruebtruec'.split(true)),
                    Array.from('a1b1c'.matchAll('1')).map(function (m) { return m[0]; }).join(',')
                ].join('|');
            })();
        """);

        Assert.Equal("[\"true\"]|aXb1c|aXbXc|1|[\"a\",\"b\",\"c\"]|1,1", result.ToString());
    }

    [Fact]
    public void MatchAll_Throws_TypeError_On_NonGlobal_RegExp()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            'test'.matchAll(/t/);
        """));
    }

    [Fact]
    public void MatchAll_Throws_TypeError_On_RegExpLike_UndefinedFlags()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = { [Symbol.match]: true };
            'test'.matchAll(obj);
        """));
    }

    #endregion

    #region Array abrupt completion regressions

    [Fact]
    public void Array_At_Uses_ArrayLike_Length_And_Propagates_TypeError()
    {
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    Array.prototype.at.call({ 0: 'first', length: 1 }, 0),
                    thrownCtor(function () { Array.prototype.at.call({ length: Symbol() }, 0); })
                ].join('|');
            })();
        """);

        Assert.Equal("first|TypeError", result.ToString());
    }

    [Fact]
    public void Array_FlatMap_Throws_TypeError_For_NonCallable_Callback_Before_Species()
    {
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                var array = [];
                array.constructor = {
                    get [Symbol.species]() {
                        throw new RangeError('species should not be read first');
                    }
                };

                return thrownCtor(function () { array.flatMap(null); });
            })();
        """);

        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void Array_Fill_Propagates_TypeError_From_Start_And_End_Conversions()
    {
        using var ctx = CreateContext();
        var result = ctx.Eval("""
            (function () {
                function thrownCtor(fn) {
                    try {
                        fn();
                        return 'no-throw';
                    } catch (e) {
                        return e.constructor.name;
                    }
                }

                return [
                    thrownCtor(function () { Array.prototype.fill.call({ length: 1 }, 0, Symbol()); }),
                    thrownCtor(function () { Array.prototype.fill.call({ length: 1 }, 0, 0, Symbol()); })
                ].join('|');
            })();
        """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    #endregion

    #region RegExp Symbol.search lastIndex TypeError

    [Fact]
    public void RegExp_SymbolSearch_Throws_On_NonWritable_LastIndex_Init()
    {
        using var ctx = CreateContext();
        Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = {
                exec: function() { return null; },
                get flags() { return ''; },
                lastIndex: 1
            };
            Object.defineProperty(obj, 'lastIndex', { writable: false });
            RegExp.prototype[Symbol.search].call(obj, '');
        """));
    }

    #endregion
}
