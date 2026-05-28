using Broiler.JavaScript.Ast;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Compiler.Tests;

public class CompilerTests
{
    [Fact]
    public void Compile_SimpleExpression_ProducesResult()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("1 + 2 * 3");
        Assert.Equal(7.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_LetConst_ScopingWorks()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("let x = 5; const y = 10; x + y;");
        Assert.Equal(15.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ArrowFunction_Works()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("((x) => x * 2)(21)");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ArrowFunction_Is_Not_A_Constructor()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                try {
                    new (() => {});
                    return 'no-throw';
                } catch (e) {
                    return e.constructor.name;
                }
            })()
            """);

        Assert.Equal("TypeError", result.ToString());
    }

    [Fact]
    public void Compile_Assigning_Callable_Proxy_Does_Not_Infer_Function_Name()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var proxy = new Proxy(() => {}, {
                    get() {
                        throw new Error("proxy trap should not be touched");
                    }
                });

                return 1;
            })()
            """);

        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ClassExtends_Proxy_Of_ArrowFunction_Throws_TypeError_Before_Prototype_Lookup()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var proxy = new Proxy(() => {}, {
                    get() {
                        throw new Error("superclass.prototype should be unreachable");
                    }
                });

                try {
                    class C extends proxy {}
                } catch (e) {
                    return e instanceof TypeError;
                }

                return false;
            })()
            """);

        Assert.True(result.BooleanValue);
    }

    [Fact]
    public void Compile_CompoundAssignment_ComputedMembers_Preserve_Nullish_Base_Evaluation_Order()
    {
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
                        var base = null;
                        var prop = function () {
                            throw new RangeError();
                        };
                        var expr = function () {
                            throw new Test262Error("right-hand side expression evaluated");
                        };

                        base[prop()] *= expr();
                    }),
                    thrownCtor(function () {
                        var base = null;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };
                        var expr = function () {
                            throw new Test262Error("right-hand side expression evaluated");
                        };

                        base[prop] *= expr();
                    }),
                    thrownCtor(function () {
                        var base = undefined;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };
                        var expr = function () {
                            throw new Test262Error("right-hand side expression evaluated");
                        };

                        base[prop] |= expr();
                    })
                ].join('|');
            })()
            """);

        Assert.Equal("RangeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void Compile_ArrowFunction_ArrayDestructuringElisions_Work()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var first = 0;
                var second = 0;

                function* g() {
                    first += 1;
                    yield 0;
                    second += 1;
                }

                var parts = [];
                (([,]) => { parts.push(first + '|' + second); })(g());

                first = 0;
                second = 0;
                (([[,] = g()]) => { parts.push(first + '|' + second); })([]);

                first = 0;
                second = 0;
                (([...[,]]) => { parts.push(first + '|' + second); })(g());

                return parts.join(';');
            })()
            """);

        Assert.Equal("1|0;1|0;1|1", result.ToString());
    }

    [Fact]
    public void Compile_ArrowFunction_ArrayDestructuringElisions_Work_With_BareYield_Generator()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var first = 0;
                var second = 0;

                function* g() {
                    first += 1;
                    yield;
                    second += 1;
                }

                var callCount = 0;
                var f = ([,]) => {
                    callCount += 1;
                    return first + '|' + second;
                };

                var outcome = f(g());
                return outcome + ';' + callCount;
            })()
            """);

        Assert.Equal("1|0;1", result.ToString());
    }

    [Fact]
    public void Compile_ArrayDestructuring_DoesNotCloseExhaustedIterator()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var returnCount = 0;
                var iter = {
                    [Symbol.iterator]() {
                        var done = false;
                        return {
                            next() {
                                if (!done) {
                                    done = true;
                                    return { value: 1, done: false };
                                }
                                return { value: undefined, done: true };
                            },
                            return() {
                                returnCount += 1;
                                return { value: undefined, done: true };
                            }
                        };
                    }
                };

                var [a, b] = iter;
                return returnCount + '|' + a + '|' + b;
            })()
            """);

        Assert.Equal("0|1|undefined", result.ToString());
    }

    [Fact]
    public void Compile_ArrayDestructuring_Closes_Iterator_Without_Arguments_On_Abrupt_Assignment_Target()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var nextCount = 0;
                var returnCount = 0;
                var returnArgsLength = -1;
                var iterable = {};
                var iterator = {
                    next: function() {
                        nextCount += 1;
                        return { done: nextCount > 10 };
                    },
                    return: function() {
                        returnCount += 1;
                        returnArgsLength = arguments.length;
                        return {};
                    }
                };
                var thrower = function() {
                    throw new Error('boom');
                };
                iterable[Symbol.iterator] = function() {
                    return iterator;
                };

                try {
                    0, [...{}[thrower()]] = iterable;
                    return 'no-throw';
                } catch (e) {
                    return [e.message, nextCount, returnCount, returnArgsLength].join('|');
                }
            })();
            """);

        Assert.Equal("boom|0|1|0", result.ToString());
    }

    [Fact]
    public void Compile_ArgumentsObject_WorksWithoutExplicitModulesLoad()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("(function () { return arguments.length; })(1, 2, 3)");
        Assert.Equal(3.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_ArgumentsObject_TypeOf_Is_Object()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("(function () { return typeof arguments; })();");
        Assert.Equal("object", result.ToString());
    }

    [Fact]
    public void Compile_NonStrict_ArgumentsObject_Maps_NonConfigurable_Parameters()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function (a) {
                Object.defineProperty(arguments, "0", { configurable: false });
                a = 2;
                return [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
            })(1);
            """);

        Assert.Equal("2|2|2", result.ToString());
    }

    [Fact]
    public void Compile_NonStrict_ArgumentsObject_Preserves_Current_Value_When_Becoming_NonWritable()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function (a) {
                Object.defineProperty(arguments, "0", { configurable: false });
                a = 2;
                Object.defineProperty(arguments, "0", { writable: false });
                var before = [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
                a = 3;
                return before + "|" + [a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
            })(1);
            """);

        Assert.Equal("2|2|2|3|2|2", result.ToString());
    }

    [Fact]
    public void Compile_NonStrict_ArgumentsObject_Delete_False_Keeps_Mapping()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function (a) {
                Object.defineProperty(arguments, "0", { configurable: false });
                var deleted = delete arguments[0];
                a = 2;
                return [deleted, a, arguments[0], Object.getOwnPropertyDescriptor(arguments, "0").value].join("|");
            })(1);
            """);

        Assert.Equal("false|2|2|2", result.ToString());
    }

    [Fact]
    public void Compile_NonStrict_Delete_Arguments_Identifier_Returns_False()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                return delete arguments;
            })();
            """);

        Assert.Equal("false", result.ToString());
    }

    [Fact]
    public void JSContext_LoadsClrInteropWithoutExplicitClrReference()
    {
        using var ctx = new JSContext();

        Assert.Equal("Broiler.JavaScript.Clr.DefaultClrInterop", JSEngine.ClrInterop.GetType().FullName);
        Assert.NotNull(JSEngine.ClrModuleProvider);
    }

    [Fact]
    public void Compile_ObjectDestructuringDefault_OnlyFallsBackForUndefined()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var initCount = 0;
                var source = { a: null, b: undefined };
                var { a = (++initCount, 'fallback-a'), b = (++initCount, 'fallback-b') } = source;
                return String(a) + '|' + b + '|' + initCount;
            })()
            """);

        Assert.Equal("null|fallback-b|1", result.ToString());
    }

    [Fact]
    public void Compile_ObjectDestructuringParameterDefault_KeepsNullValues()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var initCount = 0;
                return (({ a = (++initCount, 'fallback') }) => String(a) + '|' + initCount)({ a: null });
            })()
            """);

        Assert.Equal("null|0", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_Infer_Anonymous_Arrow_Function_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var arrayName = (([arrow = () => {}] = []) => arrow.name)();
                var objectName = (({ arrow = () => {} } = {}) => arrow.name)();
                return arrayName + '|' + objectName;
            })()
            """);

        Assert.Equal("arrow|arrow", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_Infer_Anonymous_Class_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var arrayName = (([cls = class {}] = []) => cls.name)();
                var objectName = (({ cls = class {} } = {}) => cls.name)();
                return arrayName + '|' + objectName;
            })()
            """);

        Assert.Equal("cls|cls", result.ToString());
    }

    [Fact]
    public void Compile_Class_Accessor_Literal_Names_Are_Canonicalized_And_Inferred()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    get "string"() { return 1; }
                    set "string"(value) {}
                    get 0b10() { return 2; }
                    static get "\u0073tatic"() { return 3; }
                    static set "\u0073tatic"(value) {}
                    static get 1e2() { return 4; }
                }

                var stringDesc = Object.getOwnPropertyDescriptor(C.prototype, "string");
                var numericDesc = Object.getOwnPropertyDescriptor(C.prototype, 2);
                var staticStringDesc = Object.getOwnPropertyDescriptor(C, "static");
                var staticNumericDesc = Object.getOwnPropertyDescriptor(C, 100);

                return [
                    stringDesc.get.name,
                    stringDesc.set.name,
                    numericDesc === undefined ? "missing" : numericDesc.get.name,
                    staticStringDesc.get.name,
                    staticStringDesc.set.name,
                    staticNumericDesc === undefined ? "missing" : staticNumericDesc.get.name
                ].join('|');
            })()
            """);

        Assert.Equal("get string|set string|get 2|get static|set static|get 100", result.ToString());
    }

    [Fact]
    public void Compile_Object_Accessor_Literal_Names_Are_Canonicalized_And_Inferred()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var obj = {
                    get "string"() { return 1; },
                    set "string"(value) {},
                    get 0b10() { return 2; },
                    get "\u0073tatic"() { return 3; },
                    set "\u0073tatic"(value) {},
                    get 1e2() { return 4; }
                };

                var stringDesc = Object.getOwnPropertyDescriptor(obj, "string");
                var numericDesc = Object.getOwnPropertyDescriptor(obj, 2);
                var escapedDesc = Object.getOwnPropertyDescriptor(obj, "static");
                var staticNumericDesc = Object.getOwnPropertyDescriptor(obj, 100);

                return [
                    stringDesc.get.name,
                    stringDesc.set.name,
                    numericDesc === undefined ? "missing" : numericDesc.get.name,
                    escapedDesc.get.name,
                    escapedDesc.set.name,
                    staticNumericDesc === undefined ? "missing" : staticNumericDesc.get.name
                ].join('|');
            })()
            """);

        Assert.Equal("get string|set string|get 2|get static|set static|get 100", result.ToString());
    }

    [Fact]
    public void Compile_Object_Accessor_Computed_And_NonCanonical_Literal_Keys_Work()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var defaultSet = '';
                var unicodeSet = '';
                var numericSet = '';
                var obj = {
                    get ['def\u{61}ult']() { return 'get default'; },
                    set ['def\u{61}ult'](value) { defaultSet = value; },
                    get ['unicod\u{000065}Escape']() { return 'get unicode'; },
                    set ['unicod\u{000065}Escape'](value) { unicodeSet = value; },
                    get 0.0000001() { return 'get numeric'; },
                    set 0.0000001(value) { numericSet = value; }
                };

                obj['default'] = 'set default';
                obj['unicodeEscape'] = 'set unicode';
                obj['1e-7'] = 'set numeric';

                return [
                    obj['default'],
                    defaultSet,
                    obj['unicodeEscape'],
                    unicodeSet,
                    obj['1e-7'],
                    numericSet
                ].join('|');
            })()
            """);

        Assert.Equal("get default|set default|get unicode|set unicode|get numeric|set numeric", result.ToString());
    }

    [Fact]
    public void Compile_Class_Accessor_CodePointEscapes_And_NonCanonical_Literal_Keys_Work()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var instanceDefaultSet = '';
                var instanceNumericSet = '';
                var staticUnicodeSet = '';
                var staticNumericSet = '';

                class C {
                    get 'def\u{61}ult'() { return 'get instance default'; }
                    set 'def\u{61}ult'(value) { instanceDefaultSet = value; }
                    get 0.0000001() { return 'get instance numeric'; }
                    set 0.0000001(value) { instanceNumericSet = value; }
                    static get 'unicod\u{000065}Escape'() { return 'get static unicode'; }
                    static set 'unicod\u{000065}Escape'(value) { staticUnicodeSet = value; }
                    static get 0.0000001() { return 'get static numeric'; }
                    static set 0.0000001(value) { staticNumericSet = value; }
                }

                C.prototype['default'] = 'set instance default';
                C.prototype['1e-7'] = 'set instance numeric';
                C['unicodeEscape'] = 'set static unicode';
                C['1e-7'] = 'set static numeric';

                return [
                    C.prototype['default'],
                    instanceDefaultSet,
                    C.prototype['1e-7'],
                    instanceNumericSet,
                    C['unicodeEscape'],
                    staticUnicodeSet,
                    C['1e-7'],
                    staticNumericSet
                ].join('|');
            })()
            """);

        Assert.Equal("get instance default|set instance default|get instance numeric|set instance numeric|get static unicode|set static unicode|get static numeric|set static numeric", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringDefaults_DoNotInfer_Function_Names_Through_Cover_Grammar()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var parameterName = (({ xCover = (0, function () {}) } = {}) => xCover.name)();
                var arrayName = (([xCover = (0, function () {})] = []) => xCover.name)();
                return (parameterName === '') + '|' + (arrayName === '');
            })()
            """);

        Assert.Equal("true|true", result.ToString());
    }

    [Fact]
    public void Compile_FunctionLength_Ignores_Trailing_Commas_And_Stops_At_First_Initializer_Or_Rest()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    method(a,) {}
                    methodDefault(a = 1,) {}
                    *gen(a,) {}
                    *genDefault(a = 1,) {}
                    async asyncMethod(a,) {}
                    async asyncMethodDefault(a = 1,) {}
                    async *asyncGen(a,) {}
                    async *asyncGenDefault(a = 1,) {}
                }

                return [
                    (function (a,) {}).length,
                    (function (a = 1,) {}).length,
                    (function (...rest) {}).length,
                    (async function (a,) {}).length,
                    (async function (a = 1,) {}).length,
                    (function* (a,) {}).length,
                    (function* (a = 1,) {}).length,
                    (async function* (a,) {}).length,
                    (async function* (a = 1,) {}).length,
                    ((a,) => {}).length,
                    ((a = 1,) => {}).length,
                    C.prototype.method.length,
                    C.prototype.methodDefault.length,
                    C.prototype.gen.length,
                    C.prototype.genDefault.length,
                    C.prototype.asyncMethod.length,
                    C.prototype.asyncMethodDefault.length,
                    C.prototype.asyncGen.length,
                    C.prototype.asyncGenDefault.length
                ].join('|');
            })()
            """);

        Assert.Equal("1|0|0|1|0|1|0|1|0|1|0|1|0|1|0|1|0|1|0", result.ToString());
    }

    [Fact]
    public void Compile_TaggedTemplate_NoSubstitution_Invokes_Tag_Function()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var calls = 0;
                var cooked = '';
                var raw = '';

                (function(parts) {
                    calls += 1;
                    cooked = parts[0];
                    raw = parts.raw[0];
                })`\x41`;

                return [calls, cooked, raw].join('|');
            })()
            """);

        Assert.Equal(@"1|A|\x41", result.ToString());
    }

    [Fact]
    public void Compile_TemplateLiteral_WithSubstitution_Returns_Interpolated_String()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                function printCodePoint(codePoint) {
                    var hex = codePoint
                        .toString(16)
                        .toUpperCase()
                        .padStart(6, "0");
                    return `U+${hex}`;
                }

                return printCodePoint(255);
            })()
            """);

        Assert.Equal("U+0000FF", result.ToString());
    }

    [Fact]
    public void Compile_DestructuringAssignmentProperties_OnlyInfer_Direct_Anonymous_Function_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var xCover;
                var cover;
                ({ x: xCover = (0, function () {}), y: cover = (function () {}) } = {});
                return (xCover.name === '') + '|' + cover.name;
            })()
            """);

        Assert.Equal("true|cover", result.ToString());
    }

    [Fact]
    public void Compile_StaticPrivateAsyncGeneratorMethod_DoesNotAppear_In_Property_Introspection()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    static async *#gen() { yield 1; }
                    static get gen() { return this.#gen; }
                }

                var iter = C.gen();
                return [
                    Object.prototype.hasOwnProperty.call(C, '#gen'),
                    Object.prototype.hasOwnProperty.call(C.prototype, '#gen'),
                    Object.getOwnPropertyNames(C).includes('#gen'),
                    Object.getOwnPropertyDescriptor(C, '#gen') === undefined,
                    typeof iter.next
                ].join('|');
            })()
            """);

        Assert.Equal("false|false|false|true|function", result.ToString());
    }

    [Fact]
    public void Compile_Class_Private_Methods_Infer_Function_Names()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                class C {
                    #method() {}
                    *#gen() { yield 1; }
                    async #asyncMethod() {}
                    async *#asyncGen() { yield 1; }
                    static #staticMethod() {}
                    static *#staticGen() { yield 1; }
                    static async #staticAsyncMethod() {}
                    static async *#staticAsyncGen() { yield 1; }

                    get method() { return this.#method; }
                    get gen() { return this.#gen; }
                    get asyncMethod() { return this.#asyncMethod; }
                    get asyncGen() { return this.#asyncGen; }
                    static get staticMethod() { return this.#staticMethod; }
                    static get staticGen() { return this.#staticGen; }
                    static get staticAsyncMethod() { return this.#staticAsyncMethod; }
                    static get staticAsyncGen() { return this.#staticAsyncGen; }
                }

                var c = new C();
                return [
                    c.method.name,
                    c.gen.name,
                    c.asyncMethod.name,
                    c.asyncGen.name,
                    C.staticMethod.name,
                    C.staticGen.name,
                    C.staticAsyncMethod.name,
                    C.staticAsyncGen.name
                ].join('|');
            })()
            """);

        Assert.Equal("#method|#gen|#asyncMethod|#asyncGen|#staticMethod|#staticGen|#staticAsyncMethod|#staticAsyncGen", result.ToString());
    }

    [Fact]
    public void Compile_Strict_Function_Unresolved_Assignment_Throws_ReferenceError()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""
            (function () {
                "use strict";
                missing = 1;
            })();
            """));

        Assert.Equal("ReferenceError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", ex.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Compound_And_Destructuring_Assignment_Targets_Preserve_Reference_Errors()
    {
        using var ctx = new JSContext();

        var compound = Assert.Throws<JSException>(() => ctx.Eval("""missing += 1;"""));
        Assert.Equal("ReferenceError", compound.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", compound.Error[KeyStrings.message].ToString());

        var destructuring = Assert.Throws<JSException>(() => ctx.Eval("""
            "use strict";
            ({ missing } = { missing: 1 });
            """));
        Assert.Equal("ReferenceError", destructuring.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", destructuring.Error[KeyStrings.message].ToString());

        var arrayDestructuring = Assert.Throws<JSException>(() => ctx.Eval("""
            "use strict";
            0, [ missing ] = [];
            """));
        Assert.Equal("ReferenceError", arrayDestructuring.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", arrayDestructuring.Error[KeyStrings.message].ToString());

        var tdz = Assert.Throws<JSException>(() => ctx.Eval("""
            (function () {
                0, { x } = {};
            })();
            let x;
            """));
        Assert.Equal("ReferenceError", tdz.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", tdz.Error[KeyStrings.message].ToString());

        var arrayTdz = Assert.Throws<JSException>(() => ctx.Eval("""
            0, [ x ] = [];
            let x;
            """));
        Assert.Equal("ReferenceError", arrayTdz.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", arrayTdz.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Object_Destructuring_Assignment_Expression_Returns_Rhs_Object()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                var source1 = { default: 42 };
                var source2 = { break: 7 };
                var y1 = ({ default: x } = source1);
                var y2 = ({ bre\u0061k: z } = source2);

                return [
                    y1 === source1,
                    x,
                    y1['default'],
                    y2 === source2,
                    z,
                    y2['break']
                ].join('|');
            })()
            """);

        Assert.Equal("true|42|42|true|7|7", result.ToString());
    }

    [Fact]
    public void Compile_Strict_Accessor_Bodies_Invoke_With_Strict_Mode()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""
            var obj = {};
            Object.defineProperty(obj, "accProperty", {
                set: function () {
                    "use strict";
                    test262unresolvable = null;
                    return 11;
                }
            });
            obj.accProperty = "overrideData";
            """));

        Assert.Equal("ReferenceError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("test262unresolvable is not defined", ex.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Eval_Strict_Directive_And_Top_Level_Lexical_Tdz_Throw_ReferenceError()
    {
        using var ctx = new JSContext();

        var strictEval = Assert.Throws<JSException>(() => ctx.Eval("""eval('"use strict"; missing = 1;');"""));
        Assert.Equal("ReferenceError", strictEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("missing is not defined", strictEval.Error[KeyStrings.message].ToString());

        var lexicalEval = Assert.Throws<JSException>(() => ctx.Eval("""eval('typeof x; let x;');"""));
        Assert.Equal("ReferenceError", lexicalEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", lexicalEval.Error[KeyStrings.message].ToString());

        var indirectLexicalEval = Assert.Throws<JSException>(() => ctx.Eval("""(0, eval)('typeof y; class y {}');"""));
        Assert.Equal("ReferenceError", indirectLexicalEval.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'y' before initialization", indirectLexicalEval.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void Compile_Switch_Case_Declarations_Respect_Switch_Lexical_Scope()
    {
        using var ctx = new JSContext();

        var strictResult = ctx.Eval("""
            "use strict";
            var before;
            var after;
            try { f; before = "value"; } catch (e) { before = e.name; }
            switch (1) {
              case 1:
                function f() {}
            }
            try { f; after = "value"; } catch (e) { after = e.name; }
            [before, after].join("|");
            """);
        Assert.Equal("ReferenceError|ReferenceError", strictResult.ToString());

        var tdz = Assert.Throws<JSException>(() => ctx.Eval("""
            switch (1) {
              case 0:
                let x;
              case 1:
                (function() { x; })();
            }
            """));
        Assert.Equal("ReferenceError", tdz.Error[KeyStrings.constructor][KeyStrings.name].ToString());
    }

    [Fact]
    public void Compile_ForIn_Lexical_Head_Creates_Tdz_For_Target_Expression()
    {
        using var ctx = new JSContext();

        var boundName = Assert.Throws<JSException>(() => ctx.Eval("""
            let x = 1;
            for (const x in { x }) {}
            """));
        Assert.Equal("ReferenceError", boundName.Error[KeyStrings.constructor][KeyStrings.name].ToString());

        var scopeOpen = ctx.Eval("""
            let x = 'outside';
            var probeBefore = function() { return x; };
            var probeExpr;

            for (let x in { i: probeExpr = function() { typeof x; }}) ;

            [
              probeBefore(),
              (function() {
                try {
                  probeExpr();
                  return 'no-throw';
                } catch (e) {
                  return e.name;
                }
              })()
            ].join('|');
            """);
        Assert.Equal("outside|ReferenceError", scopeOpen.ToString());
    }

    [Fact]
    public void Compile_ForOf_Lexical_Head_Creates_Tdz_For_Target_Expression()
    {
        using var ctx = new JSContext();

        var boundName = Assert.Throws<JSException>(() => ctx.Eval("""
            let x = 1;
            for (let x of [x]) {}
            """));
        Assert.Equal("ReferenceError", boundName.Error[KeyStrings.constructor][KeyStrings.name].ToString());

        var scopeOpen = ctx.Eval("""
            let x = 'outside';
            var probeBefore = function() { return x; };
            var probeExpr;

            for (let x of (probeExpr = function() { typeof x; }, [])) ;

            [
              probeBefore(),
              (function() {
                try {
                  probeExpr();
                  return 'no-throw';
                } catch (e) {
                  return e.name;
                }
              })()
            ].join('|');
            """);
        Assert.Equal("outside|ReferenceError", scopeOpen.ToString());
    }

    [Fact]
    public void Compile_Syntax_Errors_Are_Reported_For_Strict_And_Direct_Eval_Cases()
    {
        using var ctx = new JSContext();

        void AssertSyntaxError(string source)
        {
            var ex = Assert.Throws<JSException>(() => ctx.Eval(source));
            Assert.Equal("SyntaxError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        }

        AssertSyntaxError("""
            (function () {
                let x;
                eval('var x;');
            })();
            """);

        AssertSyntaxError("""eval('"use strict"; ({ x: function eval() {} });');""");
        AssertSyntaxError("""
            "use strict";
            eval('(function () { var eval; })');
            """);
        AssertSyntaxError("""(0, eval)('"use strict"; var arguments;');""");
        AssertSyntaxError("""
            "use strict";
            eval("try {} catch (arguments) { }");
            """);
        AssertSyntaxError("""Function('function eval(){"use strict";}');""");
        AssertSyntaxError("""Function('function arguments(){"use strict";}');""");
        AssertSyntaxError("""eval('"use strict"; var _f = function (param, param) { };');""");
        AssertSyntaxError("""eval("/\\\rn/;");""");
        AssertSyntaxError("eval(\"'foo\\\\\r\")");
        AssertSyntaxError("""
            "use strict";
            eval("a = 0x1; a = 01;");
            """);
        AssertSyntaxError("""eval("await 10");""");
    }

    [Fact]
    public void Compile_Direct_Eval_In_Parameter_Defaults_Rejects_Var_Conflicts()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            var functionCallCount = 0;
            var methodCallCount = 0;
            var functionError = 'none';
            var methodError = 'none';
            var fn = function (a = eval("var a = 42")) {
              functionCallCount++;
            };
            var obj = {
              method(a = eval("var a = 42")) {
                methodCallCount++;
              }
            };

            try {
              fn();
            } catch (e) {
              functionError = e.name;
            }

            try {
              obj.method();
            } catch (e) {
              methodError = e.name;
            }

            [functionError, functionCallCount, methodError, methodCallCount].join("|");
            """);

        Assert.Equal("SyntaxError|0|SyntaxError|0", result.ToString());
    }

    [Fact]
    public void Compile_Shadowed_Eval_Calls_Are_Not_Treated_As_Direct_Eval()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var functionCalls = 0;
                function run() {
                    var eval = function (value) {
                        functionCalls++;
                        return value + 1;
                    };

                    return eval(1);
                }

                var withCalls = 0;
                var scope = {
                    eval: function (value) {
                        withCalls++;
                        return value + 2;
                    }
                };
                var withResult;
                with (scope) {
                    withResult = eval(1);
                }

                var globalCalls = 0;
                function globalReplacement(value) {
                    globalCalls++;
                    return value + 3;
                }

                var originalEval = eval;
                eval = globalReplacement;
                var globalResult = eval(1);
                eval = originalEval;

                return [run(), functionCalls, withResult, withCalls, globalResult, globalCalls].join('|');
            })();
            """);

        Assert.Equal("2|1|3|1|4|1", result.ToString());
    }

    [Fact]
    public void Compile_Functions_Created_Inside_With_Capture_With_Scope()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var obj = { x: 1 };
                var getter;
                with (obj) {
                    getter = function () { return [x, eval(1)].join("|"); };
                    obj.eval = function (value) { return value + 2; };
                }

                return getter();
            })();
            """);

        Assert.Equal("1|3", result.ToString());
    }

    [Fact]
    public void Parse_Dot_Followed_By_Number_Is_Syntax_Error()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""Function("a.1");"""));
        Assert.Equal("SyntaxError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
    }

    [Fact]
    public void Compile_Update_Expressions_Modify_Captured_Variables()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var postfix = 0;
                var prefix = 0;
                var viaMethod = 0;

                (function () { postfix++; })();
                (function () { ++prefix; })();
                ({ update: function () { viaMethod++; } }).update();

                return [postfix, prefix, viaMethod].join('|');
            })();
            """);

        Assert.Equal("1|1|1", result.ToString());
    }

    [Fact]
    public void Compile_Strict_Update_Delete_With_And_Direct_Eval_Delete_Match_Test262()
    {
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

                function completesNormally(code) {
                    try {
                        eval(code);
                        return true;
                    } catch (_error) {
                        return false;
                    }
                }

                function raisesException(exception) {
                    return function (code) {
                        try {
                            eval(code);
                            return false;
                        } catch (actual) {
                            return actual instanceof exception;
                        }
                    };
                }

                function testLenientAndStrict(code, lenient_pred, strict_pred) {
                    return strict_pred('"use strict"; ' + code) && lenient_pred(code);
                }

                var evalDelete = (function () {
                    var outerVar = eval("var x; (function() { return delete x; })");
                    var outerFunction = eval("function x() {} (function() { return delete x; })");
                    var argumentShadow = eval("var x; (function(x) { return delete x; })");
                    var functionLocal = eval("(function() { var x; return delete x; })");
                    return [outerVar(), outerVar(), outerFunction(), outerFunction(), argumentShadow(), functionLocal()].join('|');
                })();

                return [
                    testLenientAndStrict('arguments++', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('arguments--', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('++arguments', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('--arguments', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('delete x;', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('delete (x);', parsesSuccessfully, parseRaisesException(SyntaxError)),
                    testLenientAndStrict('with (1) {}', completesNormally, raisesException(SyntaxError)),
                    parsesSuccessfully('function f() { "use strict"; }; with (1) {}'),
                    evalDelete
                ].join('|');
            })();
            """);

        Assert.Equal("true|true|true|true|true|true|true|true|true|true|true|true|false|false", result.ToString());
    }

    [Fact]
    public void Compile_Direct_Eval_Delete_Preserves_Lexical_Nondeletable_And_Removes_Deletable_Vars()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var outerLet = eval("let x; (function() { return delete x; })");
                var outerVarInForLet = eval("for (let q = 0; q < 1; q++) { var x; } (function() { return delete x; })");
                return [outerLet(), outerVarInForLet(), outerVarInForLet()].join('|');
            })();
            """);

        Assert.Equal("false|true|true", result.ToString());
    }

    [Fact]
    public void Compile_Indirect_Eval_Global_Vars_Remain_Deletable_And_Frozen_Delete_Uses_Strict_Mode_Rules()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                var ev = eval;
                var deletable = ev("var y = 5; (function () { return delete y; })")();
                var strictDelete;
                try {
                    eval('"use strict"; var o = Object.freeze({ noconfig: 1 }); delete o.noconfig;');
                    strictDelete = 'no-throw';
                } catch (e) {
                    strictDelete = e.name;
                }
                var sloppyDelete = eval('var o = Object.freeze({ noconfig: 1 }); delete o.noconfig;');
                return [deletable, sloppyDelete, strictDelete].join('|');
            })();
            """);

        Assert.Equal("true|false|TypeError", result.ToString());
    }

    [Fact]
    public void Compile_Map_And_Set_Constructors_Close_Iterators_When_Subclass_Mutators_Throw()
    {
        using var ctx = new JSContext();

        var result = ctx.Eval("""
            (function () {
                function run(Ctor, valueFactory) {
                    var iterable = {
                        closed: false,
                        [Symbol.iterator]: function () {
                            var first = true;
                            return {
                                next: function () {
                                    if (first) {
                                        first = false;
                                        return { value: valueFactory(), done: false };
                                    }

                                    return { value: undefined, done: true };
                                },
                                return: function () {
                                    iterable.closed = true;
                                    return {};
                                }
                            };
                        }
                    };

                    try {
                        new Ctor(iterable);
                        return 'no-throw';
                    } catch (e) {
                        return String(iterable.closed) + '|' + e;
                    }
                }

                class MyMap extends Map { set(_k, _v) { throw 'setter throws'; } }
                class MyWeakMap extends WeakMap { set(_k, _v) { throw 'setter throws'; } }
                class MySet extends Set { add(_v) { throw 'adder throws'; } }
                class MyWeakSet extends WeakSet { add(_v) { throw 'adder throws'; } }

                return [
                    run(MyMap, function () { return [{}, {}]; }),
                    run(MyWeakMap, function () { return [{}, {}]; }),
                    run(MySet, function () { return {}; }),
                    run(MyWeakSet, function () { return {}; })
                ].join('|');
            })();
            """);

        Assert.Equal("true|setter throws|true|setter throws|true|adder throws|true|adder throws", result.ToString());
    }

    [Fact]
    public void Compile_Default_Parameter_Self_Reference_Throws_ReferenceError()
    {
        using var ctx = new JSContext();

        var ex = Assert.Throws<JSException>(() => ctx.Eval("""
            var f;
            f = (x = x) => x;
            f();
            """));

        Assert.Equal("ReferenceError", ex.Error[KeyStrings.constructor][KeyStrings.name].ToString());
        Assert.Equal("Cannot access 'x' before initialization", ex.Error[KeyStrings.message].ToString());
    }

    [Fact]
    public void ForOf_Iterator_Close_On_Break_And_Throw()
    {
        using var ctx = new JSContext();

        // Test 1: break triggers iterator.return()
        var result1 = ctx.Eval("""
            (function () {
                var closed = false;
                var it = {
                    [Symbol.iterator]() {
                        var i = 0;
                        return {
                            next() { return { value: i++, done: false }; },
                            return() { closed = true; return { done: true }; }
                        };
                    }
                };
                for (var x of it) { break; }
                return String(closed);
            })();
            """).ToString();
        Assert.Equal("true", result1);

        // Test 2: throw triggers iterator.return()
        var result2 = ctx.Eval("""
            (function () {
                var closed = false;
                var it = {
                    [Symbol.iterator]() {
                        var i = 0;
                        return {
                            next() { return { value: i++, done: false }; },
                            return() { closed = true; return { done: true }; }
                        };
                    }
                };
                try { for (var y of it) { throw "err"; } } catch (e) {}
                return String(closed);
            })();
            """).ToString();
        Assert.Equal("true", result2);

        // Test 3: normal completion does NOT call return()
        var result3 = ctx.Eval("""
            (function () {
                var closed = false;
                var it = {
                    [Symbol.iterator]() {
                        var done = false;
                        return {
                            next() {
                                if (!done) { done = true; return { value: 1, done: false }; }
                                return { value: undefined, done: true };
                            },
                            return() { closed = true; return { done: true }; }
                        };
                    }
                };
                for (var z of it) {}
                return String(closed);
            })();
            """).ToString();
        Assert.Equal("false", result3);
    }

    [Fact]
    public void Compile_Const_ForOf_Throws_TypeError_On_Assignment()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                try {
                    for (const x of [1]) { x = 2; }
                    return 'no-throw';
                } catch (e) {
                    return e instanceof TypeError;
                }
            })()
            """);
        Assert.Equal(true, result.BooleanValue);
    }

    [Fact]
    public void Compile_Const_ForStatement_Update_Throws_TypeError()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                try {
                    for (const x = 0; x < 1; x++) {}
                    return 'no-throw';
                } catch (e) {
                    return e instanceof TypeError;
                }
            })()
            """);
        Assert.Equal(true, result.BooleanValue);
    }

    [Fact]
    public void Compile_Delete_String_Wrapper_Length_Throws_In_Strict_Mode()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""
            (function () {
                try {
                    (function() { "use strict"; var s = new String("abc"); delete s.length; })();
                    return 'no-throw';
                } catch (e) {
                    return e instanceof TypeError;
                }
            })()
            """);
        Assert.Equal(true, result.BooleanValue);
    }

    [Fact]
    public void Compile_Labeled_Block_Break_Preserves_Completion_Value()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""eval("L: { 'a'; break L; }")""");
        Assert.Equal("a", result.ToString());
    }

    [Fact]
    public void Compile_Labeled_Block_Without_Break_Returns_Last_Expression()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("""eval("L: { 42; }")""");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Compile_Statement_Completion_Preserves_Loop_Values()
    {
        using var ctx = new JSContext();

        Assert.Equal(3.0, ctx.Eval("""eval("2; do { 3; } while (false)")""").DoubleValue);
        Assert.Equal(3.0, ctx.Eval("""eval("2; while (true) { 3; break; }")""").DoubleValue);
        Assert.Equal(6.0, ctx.Eval("""eval("5; outer: do { while (true) { 6; continue outer; } } while (false)")""").DoubleValue);
        Assert.True(ctx.Eval("""eval("2; for (; false; ) { 3; }")""").IsUndefined);
        Assert.Equal(3.0, ctx.Eval("""eval("2; for (; ; ) { 3; break; }")""").DoubleValue);
        Assert.Equal(3.0, ctx.Eval("""eval("2; for (var k in { a: 1 }) { 3; }")""").DoubleValue);
        Assert.Equal(3.0, ctx.Eval("""eval("2; for (var v of [1]) { 3; }")""").DoubleValue);
    }

    [Fact]
    public void Compile_Statement_Completion_Preserves_Nested_Statement_Values()
    {
        using var ctx = new JSContext();

        Assert.Equal(3.0, ctx.Eval("""eval("1; do { 2; if (true) { 3; break; } 4; } while (false)")""").DoubleValue);
        Assert.True(ctx.Eval("""eval("5; do { 6; if (true) { break; } 7; } while (false)")""").IsUndefined);

        Assert.Equal(3.0, ctx.Eval("""eval("2; switch ('a') { case 'a': { 3; break; } default: }")""").DoubleValue);
        Assert.Equal(6.0, ctx.Eval("""eval("5; do { switch ('a') { case 'a': { 6; continue; } default: } } while (false)")""").DoubleValue);

        Assert.Equal(3.0, ctx.Eval("""eval("1; do { 2; with({}) { 3; break; } 4; } while (false)")""").DoubleValue);
        Assert.True(ctx.Eval("""eval("5; do { 6; with({}) { break; } 7; } while (false)")""").IsUndefined);
    }

    [Fact]
    public void Duplicate_Params_Allowed_In_Non_Strict_Mode()
    {
        using var ctx = new JSContext();
        // Non-strict: duplicate params should parse successfully
        var result = ctx.Eval("(function(x,x) { return x; })(1,2)");
        Assert.Equal(2.0, result.DoubleValue);

        // Function constructor: non-strict duplicate params
        var result2 = ctx.Eval("new Function('x','x','return x')(1,2)");
        Assert.Equal(2.0, result2.DoubleValue);
    }

    [Fact]
    public void Duplicate_Params_Rejected_In_Strict_Mode()
    {
        using var ctx = new JSContext();
        // Strict mode function body: duplicate params should throw SyntaxError
        Assert.ThrowsAny<Exception>(() => ctx.Eval("'use strict'; function f(x,x) {}"));

        // Function with 'use strict' directive: duplicate params should throw
        Assert.ThrowsAny<Exception>(() => ctx.Eval("(function(x,x) { 'use strict'; })"));
    }

    [Fact]
    public void RegExp_LastIndex_Is_Own_Data_Property()
    {
        using var ctx = new JSContext();
        // lastIndex should be an own data property
        var result = ctx.Eval("Object.getOwnPropertyDescriptor(/foo/, 'lastIndex') !== undefined");
        Assert.True(result.BooleanValue);

        var writable = ctx.Eval("Object.getOwnPropertyDescriptor(/foo/, 'lastIndex').writable");
        Assert.True(writable.BooleanValue);

        var configurable = ctx.Eval("Object.getOwnPropertyDescriptor(/foo/, 'lastIndex').configurable");
        Assert.False(configurable.BooleanValue);

        var enumerable = ctx.Eval("Object.getOwnPropertyDescriptor(/foo/, 'lastIndex').enumerable");
        Assert.False(enumerable.BooleanValue);

        var value = ctx.Eval("Object.getOwnPropertyDescriptor(/foo/, 'lastIndex').value");
        Assert.Equal(0.0, value.DoubleValue);
    }

    [Fact]
    public void RegExp_Whitespace_Matches_Unicode_Zs_Characters()
    {
        using var ctx = new JSContext();
        // \s should match Ogham Space Mark (U+1680)
        var result = ctx.Eval(@"/^\s$/.test('\u1680')");
        Assert.True(result.BooleanValue);

        // \s should match Ideographic Space (U+3000)
        var result2 = ctx.Eval(@"/^\s$/.test('\u3000')");
        Assert.True(result2.BooleanValue);

        // \S should NOT match Unicode whitespace
        var result3 = ctx.Eval(@"/^\S$/.test('\u2000')");
        Assert.False(result3.BooleanValue);
    }

    [Fact]
    public void Duplicate_Params_Rejected_In_Arrow_Functions()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("(x, x) => x"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("(a, b, a) => a"));
    }

    [Fact]
    public void Duplicate_Params_Rejected_In_Generators()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("function* g(a, a) {}"));
    }

    [Fact]
    public void Duplicate_Params_Rejected_In_Async_Functions()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("async function f(a, a) {}"));
    }

    [Fact]
    public void Duplicate_Params_Rejected_In_Methods()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("({ m(a, a) {} })"));
    }

    [Fact]
    public void Duplicate_Params_Rejected_With_Rest()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("function f(a, ...a) {}"));
    }

    [Fact]
    public void Duplicate_Params_Rejected_With_Defaults()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("function f(a, a = 1) {}"));
    }

    [Fact]
    public void Line_Terminator_Before_Arrow_Rejected()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("(x)\n=> x"));
    }

    [Fact]
    public void Numeric_Literal_0x_Without_Digits_Rejected()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("0x"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("0b"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("0o"));
    }

    [Fact]
    public void Numeric_Separator_Invalid_Positions_Rejected()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("100_"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("10__0"));
    }

    [Fact]
    public void Identifier_Start_After_Numeric_Literal_Rejected()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("0xfz"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("1a"));
    }

    [Fact]
    public void Unicode_Escaped_Reserved_Word_Rejected_As_Identifier()
    {
        using var ctx = new JSContext();
        // Escaped reserved word in expression position
        Assert.ThrowsAny<Exception>(() => ctx.Eval("var x = \\u0076ar;"));
    }

    [Theory]
    [InlineData("Function(\"for (var foo o\\\\u0066 [1]) ;\")")]
    [InlineData("Function(\"for (var foo i\\\\u006e [1]) ;\")")]
    [InlineData("Function(\"\\\\u0064o { } while (0)\")")]
    [InlineData("Function(\"class { st\\\\u0061tic m() { return 0; } }\")")]
    [InlineData("Function(\"({ g\\\\u0065t foo() { return 0; } })\")")]
    [InlineData("Function(\"({ s\\\\u0065t foo(v) {} })\")")]
    [InlineData("Function(\"function f() { return new.\\\\u0074arget }\")")]
    public void Unicode_Escaped_Grammar_Keywords_Are_Rejected(string source)
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval(source));
    }

    [Theory]
    [InlineData("(function(){ 'use strict'; l\\u0065t: 42; })()")]
    [InlineData("(function(){ 'use strict'; st\\u0061tic: 42; })()")]
    public void Strict_Mode_Rejects_Escaped_Let_And_Static_Labels(string source)
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval(source));
    }

    [Fact]
    public void Function_Constructor_Rejects_Block_Method_Definition_Syntax()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("Function(\"{a(){}}\")"));
    }

    [Theory]
    [InlineData("Function(\"var a, b; ([a, b]) = [1, 2];\")")]
    [InlineData("Function(\"var a, b; ({a, b}) = { a: 1, b: 2 };\")")]
    [InlineData("Function(\"var a, b; ({ a: ([b]) } = { a: [42] });\")")]
    [InlineData("Function(\"var a, b; ({ a: (b = 7)} = { b: 1 });\")")]
    public void Function_Constructor_Rejects_Parenthesized_Destructuring_Patterns(string source)
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval(source));
    }

    [Fact]
    public void Unicode_Escaped_Keyword_Allowed_As_Property_Name()
    {
        using var ctx = new JSContext();
        // Escaped reserved word as property key is valid
        var result = ctx.Eval("({ bre\\u0061k: 7 }).break");
        Assert.Equal(7.0, result.DoubleValue);
    }

    [Fact]
    public void Getter_Must_Have_Zero_Params()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("({ get x(a) { return a; } })"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("class C { get x(a) { return a; } }"));
    }

    [Fact]
    public void Setter_Must_Have_Exactly_One_Param()
    {
        using var ctx = new JSContext();
        Assert.ThrowsAny<Exception>(() => ctx.Eval("({ set x() {} })"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("({ set x(a, b) {} })"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("({ set x(...a) {} })"));
    }

    [Fact]
    public void Delete_String_Index_Returns_False()
    {
        using var ctx = new JSContext();
        // delete on string index properties should return false in non-strict mode
        var result = ctx.Eval("delete 'foo'[0]");
        Assert.False(result.BooleanValue);

        var result2 = ctx.Eval("delete 'hello'[4]");
        Assert.False(result2.BooleanValue);

        // delete on out-of-range index should return true
        var result3 = ctx.Eval("delete 'foo'[10]");
        Assert.True(result3.BooleanValue);

        // delete on string 'length' should return false
        var result4 = ctx.Eval("var s = 'foo'; delete s['length']");
        Assert.False(result4.BooleanValue);
    }

    [Fact]
    public void Reserved_Word_Shorthand_Destructuring_Throws()
    {
        using var ctx = new JSContext();
        // Using reserved words as shorthand in destructuring should throw SyntaxError
        Assert.ThrowsAny<Exception>(() => ctx.Eval("var {if} = {'if': 1}"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("var {for} = {'for': 1}"));
        Assert.ThrowsAny<Exception>(() => ctx.Eval("var {while} = {'while': 1}"));

        // But renamed destructuring with reserved words as source should work
        var result = ctx.Eval("var {if: x} = {'if': 1}; x");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void Catch_With_Array_Destructuring()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var r; try { throw [1, 2, 3]; } catch([a, b, c]) { r = a + b + c; } r");
        Assert.Equal(6.0, result.DoubleValue);
    }

    [Fact]
    public void Catch_With_Object_Destructuring()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var r; try { throw {x: 10, y: 20}; } catch({x, y}) { r = x + y; } r");
        Assert.Equal(30.0, result.DoubleValue);
    }

    [Fact]
    public void Catch_Without_Binding()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var r = 0; try { throw 1; } catch { r = 42; } r");
        Assert.Equal(42.0, result.DoubleValue);
    }

    [Fact]
    public void Catch_Destructured_Eval_Binding_Rejected_In_Strict()
    {
        using var ctx = new JSContext();
        // catch([eval]) should be rejected in strict mode
        Assert.ThrowsAny<Exception>(() => ctx.Eval("'use strict'; try {} catch([eval]) {}"));
        // catch({x: eval}) should be rejected in strict mode
        Assert.ThrowsAny<Exception>(() => ctx.Eval("'use strict'; try {} catch({x: eval}) {}"));
    }

    [Fact]
    public void AnnexB_Eval_Block_Function_Skip_Early_Err_Try_Destructured_Catch()
    {
        using var ctx = new JSContext();

        // Per B.3.3.3 and B.3.5: when a block-scoped function declaration
        // appears inside a catch with a destructured CatchParameter that
        // binds the same name, Annex B var-hoisting must be skipped.
        // Accessing the name should throw ReferenceError.

        // Function scope, direct eval
        var ex1 = Assert.Throws<JSException>(() => ctx.Eval("""
            (function() {
              return eval('try { throw {}; } catch ({ f }) { { function f() {} } } f;');
            }())
            """));
        Assert.Equal("ReferenceError", ex1.Error[KeyStrings.constructor][KeyStrings.name].ToString());

        // typeof should return "undefined" (no binding exists)
        var typeof1 = ctx.Eval("""
            (function() {
              return eval('try { throw {}; } catch ({ f }) { { function f() {} } } typeof f;');
            }())
            """);
        Assert.Equal("undefined", typeof1.ToString());

        // Global scope, direct eval
        var ex2 = Assert.Throws<JSException>(() => ctx.Eval("""
            eval('try { throw {}; } catch ({ f }) { { function f() {} } } f;')
            """));
        Assert.Equal("ReferenceError", ex2.Error[KeyStrings.constructor][KeyStrings.name].ToString());

        // Global scope, indirect eval
        var ex3 = Assert.Throws<JSException>(() => ctx.Eval("""
            (0, eval)('try { throw {}; } catch ({ f }) { { function f() {} } } f;')
            """));
        Assert.Equal("ReferenceError", ex3.Error[KeyStrings.constructor][KeyStrings.name].ToString());
    }

    // ----------------------------------------------------------------
    // Seeded property-based parameterized tests (recommendation #5 from
    // docs/compliance/testsuite-optimization.md).  Each fixture generates
    // inputs from a fixed seed so failures are reproducible and surface
    // under the existing `dotnet test` evidence command.
    // ----------------------------------------------------------------

    #region Property-based: destructuring assignment return value

    public static TheoryData<int, int> DestructuringInputs()
    {
        var data = new TheoryData<int, int>();
        var rng = new Random(20260525);
        for (int seed = 0; seed < 10; seed++)
        {
            data.Add(seed, rng.Next(2, 8));
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(DestructuringInputs))]
    public void Destructuring_Assignment_Returns_RHS_Seeded(int seed, int propCount)
    {
        _ = seed;
        using var ctx = new JSContext();
        // Build an object with propCount properties, destructure it, and
        // verify the overall expression returns the original RHS object.
        var props = string.Join(", ", Enumerable.Range(0, propCount).Select(i => $"p{i}: {i}"));
        var targets = string.Join(", ", Enumerable.Range(0, propCount).Select(i => $"p{i}"));
        var script = $"(function() {{\nvar {targets};\nvar rhs = {{ {props} }};\nvar result = ({{ {targets} }} = rhs);\nreturn result === rhs ? 'ok' : 'returned different object';\n}})()";
        var result = ctx.Eval(script);
        Assert.Equal("ok", result.ToString());
    }

    #endregion

    #region Property-based: binary expression type coercion

    public static TheoryData<int, string, string, string> BinaryCoercionInputs()
    {
        var data = new TheoryData<int, string, string, string>();
        var rng = new Random(20260525);
        // Pairs of (jsExpr, expectedType) for addition coercion checks.
        (string expr, string type)[] atoms =
        [
            ("42", "number"),
            ("'hello'", "string"),
            ("true", "boolean"),
            ("null", "number"),
            ("0", "number"),
        ];
        for (int seed = 0; seed < 15; seed++)
        {
            var left = atoms[rng.Next(atoms.Length)];
            var right = atoms[rng.Next(atoms.Length)];
            // When either operand is a string, result is string;
            // otherwise result is number (booleans/null/undefined coerce to number).
            string expectedType = (left.type == "string" || right.type == "string")
                ? "string" : "number";
            data.Add(seed, left.expr, right.expr, expectedType);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(BinaryCoercionInputs))]
    public void Addition_Coercion_Returns_Expected_Type_Seeded(int seed, string left, string right, string expectedType)
    {
        _ = seed;
        using var ctx = new JSContext();
        var result = ctx.Eval($"typeof ({left} + {right})");
        Assert.Equal(expectedType, result.ToString());
    }

    #endregion

    #region Property-based: function length with defaults/rest

    public static TheoryData<int, int, int, int> FunctionLengthInputs()
    {
        var data = new TheoryData<int, int, int, int>();
        var rng = new Random(20260525);
        for (int seed = 0; seed < 10; seed++)
        {
            int requiredCount = rng.Next(0, 6);
            int defaultCount = rng.Next(0, 4);
            int hasRest = rng.Next(2); // 0 or 1
            // ECMAScript: length = number of params before the first
            // default/rest parameter.
            data.Add(seed, requiredCount, defaultCount, hasRest);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(FunctionLengthInputs))]
    public void Function_Length_Stops_At_First_Default_Or_Rest_Seeded(int seed, int requiredCount, int defaultCount, int hasRest)
    {
        _ = seed;
        using var ctx = new JSContext();
        var required = string.Join(", ", Enumerable.Range(0, requiredCount).Select(i => $"a{i}"));
        var defaults = string.Join(", ", Enumerable.Range(0, defaultCount).Select(i => $"d{i} = 0"));
        var rest = hasRest == 1 ? "...r" : "";
        var parts = new[] { required, defaults, rest }.Where(s => s.Length > 0);
        var paramList = string.Join(", ", parts);
        var result = ctx.Eval($"(function({paramList}) {{}}).length");
        Assert.Equal((double)requiredCount, result.DoubleValue);
    }

    #endregion

    #region SyntaxError_EarlyErrors

    [Theory]
    [InlineData("for (let x = 1 in {}) {}", "for-in let init")]
    [InlineData("for (var x, y in {}) {}", "for-in multi-decl")]
    [InlineData("label: let x = 1;", "labeled let")]
    [InlineData("label: const x = 1;", "labeled const")]
    [InlineData("label: class C {}", "labeled class")]
    [InlineData("label: function* g() {}", "labeled function*")]
    [InlineData("'use strict'; var l\\u0065t = 1;", "escaped let strict")]
    [InlineData("(function(x) { let x = 1; })", "param redecl let")]
    [InlineData("(function(x) { const x = 1; })", "param redecl const")]
    [InlineData("var r = /\\A/u;", "regex identity escape /u")]
    [InlineData("var r = /\\-/u;", "regex dash escape /u")]
    [InlineData("if (0) label: function f() {}", "labeled fn in if")]
    [InlineData("for (var x = 1 of []) {}", "for-of init")]
    public void Syntax_ShouldThrowSyntaxError(string source, string description)
    {
        _ = description;
        using var ctx = new JSContext();
        var ex = Assert.ThrowsAny<Exception>(() => ctx.Eval(source));
        // Accept JSException (SyntaxError) or FastParseException
        Assert.True(
            ex is JSException || ex is Broiler.JavaScript.Ast.Misc.FastParseException,
            $"Expected SyntaxError but got {ex.GetType().Name}: {ex.Message}");
    }

    #endregion

    #region FnNameCover

    [Fact]
    public void FnNameCover_VarDeclaration_CommaExpression_DoesNotInferName()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var x = (0, function(){}); x.name");
        Assert.Equal("", result.ToString());
    }

    [Fact]
    public void FnNameCover_LetDeclaration_CommaExpression_DoesNotInferName()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("let x = (0, function(){}); x.name");
        Assert.Equal("", result.ToString());
    }

    [Fact]
    public void FnNameCover_Assignment_CommaExpression_DoesNotInferName()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var x; x = (0, function(){}); x.name");
        Assert.Equal("", result.ToString());
    }

    [Fact]
    public void FnNameCover_DirectFunctionExpression_InfersName()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval("var x = function(){}; x.name");
        Assert.Equal("x", result.ToString());
    }

    #endregion

    #region TaggedTemplateCaching

    [Fact]
    public void TaggedTemplate_SameSourcePosition_ReturnsSameObject()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            function tag(t) { return t; }
            var results = [];
            for (var i = 0; i < 2; i++) results.push(tag`hello`);
            results[0] === results[1];
        ");
        Assert.Equal(true, result.BooleanValue);
    }

    [Fact]
    public void TaggedTemplate_Object_IsFrozen()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            function tag(t) { return t; }
            Object.isFrozen(tag`hello`);
        ");
        Assert.Equal(true, result.BooleanValue);
    }

    #endregion

    #region Prefix Update Expression

    [Fact]
    public void PrefixIncrement_NonWritableProperty_ReturnsNewValue()
    {
        using var ctx = new JSContext();
        // Per spec, ++x returns the new computed value even when the
        // write silently fails on a non-writable property in sloppy mode.
        var result = ctx.Eval(@"
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false });
            ++o.x;
        ");
        Assert.Equal(2.0, result.DoubleValue);
    }

    [Fact]
    public void PrefixDecrement_NonWritableProperty_ReturnsNewValue()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false });
            --o.x;
        ");
        Assert.Equal(0.0, result.DoubleValue);
    }

    [Fact]
    public void PrefixIncrement_NonWritableProperty_StrictMode_ThrowsTypeError()
    {
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval(@"
            'use strict';
            var o = {};
            Object.defineProperty(o, 'x', { value: 1, writable: false });
            ++o.x;
        "));
    }

    [Fact]
    public void UpdateExpression_ComputedMembers_Null_Base_Throws_TypeError_Before_ToPropertyKey()
    {
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
                        var base = null;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        base[prop]++;
                    }),
                    thrownCtor(function () {
                        var base = null;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        base[prop]--;
                    }),
                    thrownCtor(function () {
                        var base = undefined;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        ++base[prop];
                    }),
                    thrownCtor(function () {
                        var base = undefined;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        --base[prop];
                    })
                ].join('|');
            })()
            """);

        Assert.Equal("TypeError|TypeError|TypeError|TypeError", result.ToString());
    }

    [Fact]
    public void ComputedMemberExpression_Null_Undefined_Throws_TypeError_Before_ToPropertyKey()
    {
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
                        var base = null;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        base[prop];
                    }),
                    thrownCtor(function () {
                        var base = undefined;
                        var prop = {
                            toString() {
                                throw new Test262Error("property key evaluated");
                            }
                        };

                        base[prop];
                    })
                ].join('|');
            })()
            """);

        Assert.Equal("TypeError|TypeError", result.ToString());
    }

    #endregion

    #region Octal Escapes in Strict Mode

    [Fact]
    public void OctalEscape_08_StrictMode_ThrowsSyntaxError()
    {
        using var ctx = new JSContext();
        // \08 is a NonOctalDecimalEscape, forbidden in strict mode
        Assert.Throws<JSException>(() => ctx.Eval(
            "'use strict'; \"\\08\";"
        ));
    }

    [Fact]
    public void OctalEscape_8_StrictMode_ThrowsSyntaxError()
    {
        using var ctx = new JSContext();
        // \8 is a NonOctalDecimalEscape, forbidden in strict mode
        Assert.Throws<JSException>(() => ctx.Eval(
            "'use strict'; \"\\8\";"
        ));
    }

    [Fact]
    public void OctalEscape_08_SloppyMode_Parses()
    {
        using var ctx = new JSContext();
        // \08 in sloppy mode should parse without error
        var result = ctx.Eval("\"\\08\".length;");
        Assert.Equal(2.0, result.DoubleValue);
    }

    #endregion

    #region Function Declarations in Statement Bodies

    [Fact]
    public void FunctionDeclaration_InIf_SloppyMode_Parses()
    {
        using var ctx = new JSContext();
        var result = ctx.Eval(@"
            var r = 0;
            if (true) function f() { r = 1; }
            f();
            r;
        ");
        Assert.Equal(1.0, result.DoubleValue);
    }

    [Fact]
    public void FunctionDeclaration_InWhile_SloppyMode_Parses()
    {
        using var ctx = new JSContext();
        // Should parse without error in sloppy mode (Annex B)
        ctx.Eval("(function() { while (false) function f() {} })()");
    }

    [Fact]
    public void FunctionDeclaration_InWhile_StrictMode_ThrowsSyntaxError()
    {
        using var ctx = new JSContext();
        Assert.Throws<JSException>(() => ctx.Eval(@"
            'use strict';
            while (false) function f() {}
        "));
    }

    #endregion
}
