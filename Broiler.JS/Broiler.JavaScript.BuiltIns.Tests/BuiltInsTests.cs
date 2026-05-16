using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.Engine;
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

              return [
                thrownCtor(function () {
                  Object.defineProperties(arr1, { length: { value: 1 } });
                }) + ':' + arr1.length,
                thrownCtor(function () {
                  Object.defineProperties(arr2, { length: { configurable: true } });
                }),
                thrownCtor(function () {
                  Object.defineProperties(arr3, { '3': { value: 'abc' } });
                }) + ':' + arr3.hasOwnProperty('3')
              ].join('|');
            })();
            """);

        Assert.Equal("TypeError:2|TypeError|TypeError:false", result.ToString());
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
                    Object.setPrototypeOf(array, Array.prototype);
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

        Assert.Equal("TypeError|x,y|TypeError|TypeError|TypeError|TypeError|0,1,2|TypeError|TypeError|TypeError|TypeError", result.ToString());

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

        Assert.Equal("TypeError|TypeError|TypeError|0|TypeError|1|1", result.ToString());
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

    private static void EnsureBuiltInsLoaded()
    {
        // Load CLR assembly so JSEngine.ClrInterop is properly configured
        // (required for JSConsole marshalling via ClrProxy).
        RuntimeHelpers.RunClassConstructor(
            typeof(Clr.DefaultClrInterop).TypeHandle);
        RuntimeHelpers.RunClassConstructor(
            typeof(Weak.JSWeakRef).TypeHandle);
    }
}
