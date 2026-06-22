using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Broiler.JavaScript.BuiltIns.Array;
using Broiler.JavaScript.BuiltIns.Array.Typed;
using Broiler.JavaScript.BuiltIns.Date;
using Broiler.JavaScript.BuiltIns.Debug;
using Broiler.JavaScript.BuiltIns.Decimal;
using Broiler.JavaScript.BuiltIns.Disposable;
using Broiler.JavaScript.BuiltIns.Error;
using Broiler.JavaScript.BuiltIns.Intl;
using Broiler.JavaScript.BuiltIns.Iterator;
using Broiler.JavaScript.BuiltIns.Map;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.BuiltIns.Promise;
using Broiler.JavaScript.BuiltIns.Set;
using Broiler.JavaScript.BuiltIns.Symbol;
using Broiler.JavaScript.BuiltIns.BigInt;
using Broiler.JavaScript.BuiltIns.Boolean;
using Broiler.JavaScript.BuiltIns.Generator;
using Broiler.JavaScript.BuiltIns.Null;
using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.LinqExpressions;
using Broiler.JavaScript.BuiltIns.Class;
using Broiler.JavaScript.BuiltIns.RegExp;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Extensions;
using Broiler.JavaScript.LinqExpressions.LinqExpressions.GeneratorsV2;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.BuiltIns;

internal static class BuiltInsAssemblyInitializer
{

    [ModuleInitializer]
    internal static void Initialize()
    {
        // Set the default built-in registry on JSContext.
        // DefaultBuiltInRegistry now lives in this assembly (BuiltIns).
        JSEngine.BuiltInRegistry ??= DefaultBuiltInRegistry.Instance;

        // Register BuiltIns assembly types into the built-in registration pipeline.
        // This appends to any existing additional registrations so that multiple
        // satellite assemblies can contribute built-in types.
        var existing = DefaultBuiltInRegistry.AdditionalRegistrations;
        DefaultBuiltInRegistry.AdditionalRegistrations = existing == null
            ? static context =>
            {
                context.RegisterBuiltInClasses();
                PatchErrorConstructors(context);
                PatchLegacyDatePrototype(context);
                PatchCompatibilityBuiltIns(context);
                PatchBuiltInConstructorPrototypeDescriptors(context);
            }
            : context =>
            {
                existing(context);
                context.RegisterBuiltInClasses();
                PatchErrorConstructors(context);
                PatchLegacyDatePrototype(context);
                PatchCompatibilityBuiltIns(context);
                PatchBuiltInConstructorPrototypeDescriptors(context);
            };

        // Wire factory delegate for JSDisposableStack so the Compiler can create
        // instances via the IJSDisposableStack interface without referencing BuiltIns.
        IJSDisposableStack.CreateNew = static () => new JSDisposableStack();

        // Wire factory delegate for the Intl global object so the Globals assembly
        // does not directly reference JSIntl.
        DefaultBuiltInRegistry.IntlFactory = static () => JSIntl.GetIntlObject();

        // Wire factory delegate for JSDate so Core/Clr can create
        // Date values without referencing the concrete type directly.
        JSValue.CreateDateFactory = static v => new JSDate(v);

        // Wire factory delegates for JSArray so Core can create
        // array values without referencing the concrete type directly.
        JSValue.CreateArrayFactory = static () => new JSArray();
        JSValue.CreateArrayWithLengthFactory = static count => new JSArray(count);

        JSObject.CreatePrimitiveObject = static value => value switch
        {
            // JSSymbol is a JSPrimitive, so it must be matched before the general
            // JSPrimitive arm to box into a JSSymbolObject rather than a plain
            // JSPrimitiveObject.
            JSSymbol symbol => new JSSymbolObject(symbol),
            JSPrimitive primitive => new JSPrimitiveObject(primitive),
            _ => throw JSEngine.NewTypeError($"Cannot convert {value} to object")
        };

        // Initialize JSArrayBuilder with the concrete JSArray type so the
        // Compiler can build array expression trees without a direct reference.
        JSArrayBuilder.Initialize(typeof(JSArray));

        // Wire factory delegates for JSDecimal so Core/Compiler can create
        // and inspect decimal values without referencing the concrete type.
        JSValue.CreateDecimalFactory = static v => new JSDecimal(v);
        JSValue.CreateDecimalFromStringFactory = static s => new JSDecimal(s);

        // Wire factory delegates for JSBigInt so Core/Compiler can create
        // BigInt values without referencing the concrete type directly.
        JSValue.CreateBigIntFromStringFactory = static s => new JSBigInt(s);
        JSValue.CreateBigIntFactory = static v => new JSBigInt(v);

        // Wire JSNumber singletons and factory delegates so Core/Compiler can
        // create and inspect number values without referencing the concrete type directly.
        JSValue.NumberOne = JSNumber.One;
        JSValue.NumberNaN = JSNumber.NaN;
        JSValue.NumberZero = JSNumber.Zero;
        JSValue.NumberMinusOne = JSNumber.MinusOne;
        JSValue.NumberTwo = JSNumber.Two;
        JSValue.NumberNegativeZero = JSNumber.NegativeZero;
        JSValue.NumberPositiveInfinity = JSNumber.PositiveInfinity;
        JSValue.NumberNegativeInfinity = JSNumber.NegativeInfinity;
        JSValue.CreateNumber = static v => new JSNumber(v);
        JSValue.NumberToECMAString = JSNumber.ToECMAString;
        JSValue.IsPositiveZeroCheck = JSNumber.IsPositiveZero;
        JSValue.IsNegativeZeroCheck = JSNumber.IsNegativeZero;

        // Initialize JSNumberBuilder with the concrete JSNumber type so the
        // Compiler can build number expression trees without a direct reference.
        JSNumberBuilder.Initialize(typeof(JSNumber));

        // Wire JSBoolean singletons so Core/Runtime can access boolean
        // values without referencing the concrete type directly.
        JSValue.BooleanTrue = JSBoolean.True;
        JSValue.BooleanFalse = JSBoolean.False;

        // Wire JSNull singleton so Core/Runtime can access the null
        // value without referencing the concrete type directly.
        JSValue.NullValue = JSNull.Value;
        JSNullBuilder.Initialize(typeof(JSNull));

        // Wire JSString factory delegates and cached empty-string value
        // so Core/Runtime can create string values without referencing
        // the concrete type directly.
        JSValue.CreateString = static v => new JSString(v);
        JSValue.EmptyString = JSString.Empty;
        JSValue.CreateStringWithKey = static (s, k) => new JSString(s, k);

        // Initialize JSStringBuilder with the concrete JSString type so the
        // Compiler can build string expression trees without a direct reference.
        JSStringBuilder.Initialize(typeof(JSString));

        // Wire JSSymbol well-known singletons and factory delegates so Core
        // and other assemblies can work with symbols without referencing the
        // concrete JSSymbol type directly.
        JSValue.SymbolIterator = JSSymbol.iterator;
        JSValue.SymbolAsyncIterator = JSSymbol.asyncIterator;
        JSValue.SymbolDispose = JSSymbol.dispose;
        JSValue.SymbolAsyncDispose = JSSymbol.asyncDispose;
        JSValue.CreateSymbolFactory = static name => new JSSymbol(name);
        JSValue.CreateSymbolClassFactory = static (ctx, register) =>
            JSSymbol.CreateClass((JSContext)ctx, register);
        JSValue.GetGlobalSymbolFactory = static name => JSSymbol.GlobalSymbol(name);
        JSValue.GetSymbolByKeyFactory = static key => JSSymbol.FromKey(key);
        JSValue.GetBuiltinToStringTag = static value => ResolveBuiltinToStringTag(value);

        // Initialize JSSymbolBuilder with the concrete JSSymbol type so the
        // ClassGenerator can emit symbol lookups without a direct reference.
        JSSymbolBuilder.Initialize(typeof(JSSymbol));

        // Initialize JSClassBuilder with the concrete JSClass type so the
        // Compiler can build class expression trees without a direct reference.
        JSClassBuilder.Initialize(typeof(JSClass), typeof(JSFunction), typeof(JSFunctionDelegate));

        // Initialize JSFunctionBuilder with the concrete JSFunction type so the
        // Compiler can build function expression trees without a direct reference.
        JSFunctionBuilder.Initialize(typeof(JSFunction));

        // Initialize JSRegExpBuilder with the concrete JSRegExp type so the
        // Compiler can build regex expression trees without a direct reference.
        JSRegExpBuilder.Initialize(typeof(JSRegExp));

        // Wire factory delegates for JSFunction so Core can create
        // function instances without referencing the concrete type directly.
        JSValue.CreateFunctionFactory = static d => new JSFunction(d);
        JSValue.CreateFunctionFullFactory = static (d, name, source, length, createProto) =>
            new JSFunction(d, name, source, length, createProto);

        // Wire factory delegate for JSFunction.CreateClass so JSContext can
        // build the Function constructor without referencing JSFunction directly.
        JSEngine.CreateFunctionClass = static (ctx, register) => JSFunction.CreateClass(ctx, register);

        // Wire factory delegates for JSGenerator so Core and Clr can create
        // generator instances without a direct type reference.
        JSGeneratorBuilder.CreateFromEnumerator = static (en, name) => new JSGenerator(en, name);
        JSGeneratorBuilder.CreateFromClrV2 = static g => new JSGenerator((ClrGeneratorV2)g);

        // Wire factory delegate for JSPrototype so Core can create prototype
        // instances without referencing the concrete type directly.
        JSObject.CreatePrototype = static obj => new JSPrototype(obj);

        // Wire factory delegates for JSError types so Core can create
        // error instances without referencing the concrete types directly.
        JSEngine.CreateTypeError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.TypeError] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSEngine.CreateSyntaxError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.SyntaxError] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSEngine.CreateURIError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.URIError] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSEngine.CreateRangeError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.RangeError] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSEngine.CreateReferenceError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.ReferenceError] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSEngine.CreateError = static (message, function, filePath, line) =>
            new JSException(message, ((JSEngine.CurrentContext as JSObject)?[KeyStrings.Error] as IJSFunction)?.Prototype as JSObject,
                function: function, filePath: filePath, line: line);
        JSException.CreateJSError = static (ex, msg) => new JSError(ex, msg);
        JSException.CreateJSErrorWithPrototype = static (ex, prototype) => new JSError(ex, prototype);
        JSException.JSErrorFrom = static (ex) => JSError.From(ex);

        // Wire JSConstants with concrete JSString instances.
        JSConstants.Decimal = new JSString("decimal");
        JSConstants.Arguments = new JSString("arguments");
        JSConstants.BigInt = new JSString("bigint");
        JSConstants.Undefined = new JSString("undefined");
        JSConstants.Boolean = new JSString("boolean");
        JSConstants.String = new JSString("string");
        JSConstants.Object = new JSString("object");
        JSConstants.Number = new JSString("number");
        JSConstants.Function = new JSString("function");
        JSConstants.Symbol = new JSString("symbol");
        JSConstants.Infinity = new JSString("Infinity");
        JSConstants.NegativeInfinity = new JSString("-Infinity");

        // Wire factory delegate for JSConsole so DefaultBuiltInRegistry
        // does not directly reference the concrete type.
        DefaultBuiltInRegistry.ConsoleFactory = static ctx => new JSConsole(ctx);

        // Wire structured clone extension for Date, Map, Set, and ArrayBuffer types so that
        // JSGlobal.StructuredClone works without Core referencing BuiltIns.
        DefaultBuiltInRegistry.StructuredCloneExtension = static (value, seen, recurse) =>
        {
            if (value is JSDate date)
            {
                var clone = new JSDate(date.value);
                seen[value] = clone;
                return clone;
            }

            if (value is JSMap map)
            {
                var clone = new JSMap(Arguments.Empty);
                seen[value] = clone;
                foreach (var entry in map.EnumerateEntries())
                {
                    var clonedKey = recurse(entry[0], seen);
                    var clonedVal = recurse(entry[1], seen);
                    clone.Set(clonedKey, clonedVal);
                }
                return clone;
            }

            if (value is JSSet set)
            {
                var clone = new JSSet(Arguments.Empty);
                seen[value] = clone;
                foreach (var item in set.EnumerateValues())
                    clone.Add(recurse(item, seen));
                return clone;
            }

            if (value is JSArrayBuffer arrayBuffer)
            {
                if (arrayBuffer.isDetached)
                    throw JSEngine.NewTypeError("structuredClone: cannot clone a detached ArrayBuffer");

                var newBuf = new byte[arrayBuffer.buffer.Length];
                System.Array.Copy(arrayBuffer.buffer, newBuf, arrayBuffer.buffer.Length);

                var clone = new JSArrayBuffer(newBuf);
                seen[value] = clone;
                return clone;
            }

            if (value is JSTypedArray typedArray)
            {
                var clonedBuffer = recurse(typedArray.buffer, seen) as JSArrayBuffer
                    ?? throw JSEngine.NewTypeError("structuredClone: typed array buffer must be an ArrayBuffer");

                var clone = CloneTypedArray(typedArray, clonedBuffer);
                seen[value] = clone;
                return clone;
            }

            if (value is DataView.DataView dataView)
            {
                var clonedBuffer = recurse(dataView.Buffer, seen) as JSArrayBuffer
                    ?? throw JSEngine.NewTypeError("structuredClone: DataView buffer must be an ArrayBuffer");

                var clone = new DataView.DataView(clonedBuffer, dataView.ByteOffset, dataView.ByteLength);
                seen[value] = clone;
                return clone;
            }

            return null;
        };

        // Wire Iterator.prototype helper methods so DefaultBuiltInRegistry
        // does not directly reference JSIteratorObject.
        DefaultBuiltInRegistry.IteratorPrototypeSetup = static proto =>
        {
            // Per spec %Iterator.prototype% itself carries neither "next" nor "return"; those live on
            // %IteratorHelperPrototype% / %WrapForValidIteratorPrototype% (created below). Only the
            // iterator-helper methods, @@dispose, @@iterator and @@toStringTag sit on the base.
            DefaultBuiltInRegistry.AddProto(proto, "map", JSIteratorObject.StaticMap, 1);
            DefaultBuiltInRegistry.AddProto(proto, "filter", JSIteratorObject.StaticFilter, 1);
            DefaultBuiltInRegistry.AddProto(proto, "take", JSIteratorObject.StaticTake, 1);
            DefaultBuiltInRegistry.AddProto(proto, "drop", JSIteratorObject.StaticDrop, 1);
            DefaultBuiltInRegistry.AddProto(proto, "flatMap", JSIteratorObject.StaticFlatMap, 1);
            DefaultBuiltInRegistry.AddProto(proto, "reduce", JSIteratorObject.StaticReduce, 1);
            DefaultBuiltInRegistry.AddProto(proto, "toArray", JSIteratorObject.StaticToArray, 0);
            DefaultBuiltInRegistry.AddProto(proto, "forEach", JSIteratorObject.StaticForEach, 1);
            DefaultBuiltInRegistry.AddProto(proto, "some", JSIteratorObject.StaticSome, 1);
            DefaultBuiltInRegistry.AddProto(proto, "every", JSIteratorObject.StaticEvery, 1);
            DefaultBuiltInRegistry.AddProto(proto, "find", JSIteratorObject.StaticFind, 1);

            // %IteratorPrototype% [ @@dispose ] (): close the iterator by calling its return method.
            proto.FastAddValue((IJSSymbol)JSSymbol.dispose, CreateNativeFunction(static (in Arguments a) =>
            {
                var iterator = a.This;
                var ret = iterator[KeyStrings.GetOrCreate("return")]; // GetMethod(O, "return")
                if (!ret.IsNullOrUndefined)
                {
                    if (ret is not IJSFunction returnFn)
                        throw JSEngine.NewTypeError("Iterator.prototype[Symbol.dispose]: return is not a function");
                    returnFn.InvokeFunction(new Arguments(iterator));
                }
                return JSUndefined.Value;
            }, "[Symbol.dispose]", 0), JSPropertyAttributes.ConfigurableValue);

            // %IteratorPrototype% [ @@toStringTag ] is an accessor: the getter reports "Iterator"; the
            // setter is SetterThatIgnoresPrototypeProperties (a non-object receiver, or %Iterator.prototype%
            // itself, is a TypeError — the latter emulates assigning to a non-writable data property — and
            // any other receiver gets an own data property instead, bypassing this inherited accessor).
            proto.FastAddProperty(
                (IJSSymbol)JSSymbol.toStringTag,
                // CreateNativeGetter prepends the "get " accessor prefix itself, so pass the
                // bare property name; passing "get [Symbol.toStringTag]" doubled it, yielding a
                // name / toString of "get get [Symbol.toStringTag]" (not valid NativeFunction syntax).
                CreateNativeGetter(static (in Arguments a) => JSValue.CreateString("Iterator"), "[Symbol.toStringTag]"),
                CreateNativeFunction((in Arguments a) =>
                {
                    if (a.This is not JSObject receiver)
                        throw JSEngine.NewTypeError("Iterator.prototype[Symbol.toStringTag] setter requires an object receiver");
                    if (ReferenceEquals(receiver, proto))
                        throw JSEngine.NewTypeError("Cannot assign to Symbol.toStringTag of Iterator.prototype");
                    receiver.FastAddValue((IJSSymbol)JSSymbol.toStringTag, a.Get1(), JSPropertyAttributes.EnumerableConfigurableValue);
                    return JSUndefined.Value;
                }, "set [Symbol.toStringTag]", 1),
                JSPropertyAttributes.ConfigurableProperty);

            // %IteratorHelperPrototype% (the prototype of map/filter/take/drop/flatMap results):
            // next / return plus a "Iterator Helper" @@toStringTag, inheriting from %Iterator.prototype%.
            var helperProto = new JSObject { BasePrototypeObject = proto };
            DefaultBuiltInRegistry.AddProto(helperProto, "next", JSIteratorObject.StaticNext, 0);
            DefaultBuiltInRegistry.AddProto(helperProto, "return", JSIteratorObject.StaticReturn, 1);
            helperProto.FastAddValue((IJSSymbol)JSSymbol.toStringTag,
                JSValue.CreateString("Iterator Helper"), JSPropertyAttributes.ConfigurableReadonlyValue);

            // %WrapForValidIteratorPrototype% (the prototype of Iterator.from wrappers): next / return,
            // and no own @@toStringTag (so it inherits "Iterator" from %Iterator.prototype%).
            var wrapProto = new JSObject { BasePrototypeObject = proto };
            DefaultBuiltInRegistry.AddProto(wrapProto, "next", JSIteratorObject.StaticNext, 0);
            DefaultBuiltInRegistry.AddProto(wrapProto, "return", JSIteratorObject.StaticReturn, 1);

            JSIteratorObject.RegisterHelperPrototypes(proto, helperProto, wrapProto);
        };

        // Wire factory delegates for JSPromise so Core can create
        // promise instances without referencing the concrete type directly.
        JSEngine.CreateResolvedOrRejectedPromise = static (value, isResolved) =>
            new JSPromise(value, isResolved ? JSPromise.PromiseState.Resolved : JSPromise.PromiseState.Rejected);
        JSEngine.CreatePromiseFromDelegate = static (d) => new JSPromise(d);
        JSValue.CreatePromiseFromTask = static (task) => new JSPromise(task);
        JSValue.CreatePromiseFromUntypedTask = static (task) => task.ToPromise();
        JSValue.CreatePromiseFromGenericTask = static (task) => task.ToPromise();

        // Wire JSFunction.CreateClrDelegateFactory (moved from LinqExpressionsAssemblyInitializer)
        JSFunction.CreateClrDelegateFactory = LinqExpressionsAssemblyInitializer.CreateClrDelegate;

        // Initialize builders for generator/async function types
        JSGeneratorFunctionBuilderV2.Initialize(typeof(JSGeneratorFunctionV2));
        JSAsyncFunctionBuilder.Initialize(typeof(JSAsyncFunction), typeof(JSValue));
    }

    // Object.prototype.toString (§20.1.3.6) builtin tags for values whose
    // primitive internal slot lives in the BuiltIns layer: boxed primitives
    // (new Number/Boolean/String, Object(symbol/bigint)) and the
    // Number/Boolean/String prototype objects, which per spec carry a
    // [[NumberData]]/[[BooleanData]]/[[StringData]] slot. Date and RegExp are
    // intentionally excluded: in modern ECMAScript their prototypes are ordinary
    // objects, so a type-based check would mis-tag e.g. RegExp.prototype as
    // "[object RegExp]" instead of "[object Object]".
    private static string ResolveBuiltinToStringTag(JSValue value)
    {
        // §20.1.3.6 step 14: an object with a [[RegExpMatcher]] internal slot (a real RegExp instance,
        // whose compiled matcher is set — unlike %RegExp.prototype%, an ordinary object) tags as "RegExp".
        if (value is JSRegExp { value: not null })
            return "RegExp";

        // §20.1.3.6 step 13: an object with a [[DateValue]] internal slot (a real Date
        // instance — %Date.prototype% is an ordinary object and so falls through to "Object")
        // tags as "Date". The tag must come from the internal slot, not a @@toStringTag on the
        // prototype, so a user-supplied own @@toStringTag can still override it.
        if (value is JSDate)
            return "Date";

        if (value is JSPrimitiveObject boxed)
        {
            var primitive = boxed.value;
            if (primitive.IsString) return "String";
            if (primitive.IsNumber) return "Number";
            if (primitive.IsBoolean) return "Boolean";
            // BigInt and Symbol are intentionally absent: §20.1.3.6 has no builtin
            // tag for them, so their wrappers tag as "Object". The "[object BigInt]"/
            // "[object Symbol]" display comes from the string-valued @@toStringTag on
            // BigInt.prototype / Symbol.prototype instead (see comment below).
        }

        // Object.prototype.toString performs ToObject(this) before computing the
        // builtin tag, so a RAW primitive receiver (e.g. toString.call(true)) carries
        // the same [[BooleanData]]/[[NumberData]]/[[StringData]] slot its wrapper would.
        // (Symbol/BigInt primitives box to wrappers whose builtin tag is "Object"; their
        // "Symbol"/"BigInt" tag comes from the @@toStringTag override instead.)
        if (value is not JSObject)
        {
            if (value.IsString) return "String";
            if (value.IsNumber) return "Number";
            if (value.IsBoolean) return "Boolean";
        }

        if (value is JSObject @object && JSEngine.Current is JSObject global)
        {
            if (global[Names.Number] is JSFunction number && ReferenceEquals(@object, number.prototype))
                return "Number";
            if (global[Names.Boolean] is JSFunction boolean && ReferenceEquals(@object, boolean.prototype))
                return "Boolean";
            if (global[Names.String] is JSFunction @string && ReferenceEquals(@object, @string.prototype))
                return "String";
        }

        return null;
    }

    private static JSTypedArray CloneTypedArray(JSTypedArray typedArray, JSArrayBuffer clonedBuffer)
    {
        // The (buffer, byteOffset, length) TypedArray constructor takes length in
        // ELEMENTS, not bytes (the two coincide only for 1-byte element types).
        var args = new Arguments(
            JSUndefined.Value,
            clonedBuffer,
            new JSNumber(typedArray.byteOffset),
            new JSNumber(typedArray.Length));

        // The TypedArray (in Arguments) constructor enforces the "requires new" check
        // via new.target. This internal reconstruction is morally a `new`, so publish a
        // new.target (the source's constructor) for the duration: the check then passes
        // and the per-type prototype resolves correctly.
        var executionContext = JSEngine.Current as IJSExecutionContext;
        var savedNewTarget = executionContext?.CurrentNewTarget;
        if (executionContext != null)
            executionContext.CurrentNewTarget = typedArray[KeyStrings.constructor];
        try
        {
            return typedArray switch
            {
                JSInt8Array => new JSInt8Array(args),
                JSUInt8Array => new JSUInt8Array(args),
                JSUint8ClampedArray => new JSUint8ClampedArray(args),
                JSInt16Array => new JSInt16Array(args),
                JSUInt16Array => new JSUInt16Array(args),
                JSInt32Array => new JSInt32Array(args),
                JSUInt32Array => new JSUInt32Array(args),
                JSBigInt64Array => new JSBigInt64Array(args),
                JSBigUint64Array => new JSBigUint64Array(args),
                JSFloat16Array => new JSFloat16Array(args),
                JSFloat32Array => new JSFloat32Array(args),
                JSFloat64Array => new JSFloat64Array(args),
                _ => throw JSEngine.NewTypeError($"structuredClone: unsupported typed array type {typedArray.GetType().Name}")
            };
        }
        finally
        {
            if (executionContext != null)
                executionContext.CurrentNewTarget = savedNewTarget;
        }
    }

    private static void PatchErrorConstructors(JSContext context)
    {
        PatchErrorConstructor(context, KeyStrings.Error, static (in Arguments a) => new JSError(in a));

        // Per spec §20.5.6.1, the [[Prototype]] of each NativeError constructor is Error.
        var errorCtor = context[KeyStrings.Error] as JSFunction;
        PatchErrorConstructor(context, KeyStrings.TypeError, static (in Arguments a) => new JSTypeError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.SyntaxError, static (in Arguments a) => new JSSyntaxError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.URIError, static (in Arguments a) => new JSURIError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.RangeError, static (in Arguments a) => new JSRangeError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.ReferenceError, static (in Arguments a) => new JSReferenceError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.EvalError, static (in Arguments a) => new JSEvalError(in a), errorCtor);
        PatchErrorConstructor(context, KeyStrings.GetOrCreate("AggregateError"), static (in Arguments a) => new JSAggregateError(in a), errorCtor, 2);
        // SuppressedError is an ordinary error constructor (its [[Prototype]] is Error) and, like the
        // other error constructors, must be callable without `new` rather than throwing.
        PatchErrorConstructor(context, KeyStrings.GetOrCreate("SuppressedError"), static (in Arguments a) => new JSSuppressedError(in a), errorCtor, 3);
    }

    private static void PatchLegacyDatePrototype(JSContext context)
    {
        static JSValue OrdinaryToPrimitive(JSObject @object, bool preferString)
        {
            var first = preferString ? KeyStrings.toString : KeyStrings.valueOf;
            var second = preferString ? KeyStrings.valueOf : KeyStrings.toString;

            foreach (var key in new[] { first, second })
            {
                if (@object[key] is not IJSFunction method)
                    continue;

                var primitive = method.InvokeFunction(new Arguments(@object));
                if (!primitive.IsObject)
                    return primitive;
            }

            throw JSEngine.NewTypeError("Cannot convert object to primitive value");
        }

        static JSValue ToNumberPrimitive(JSValue value)
        {
            if (value is not JSObject @object)
                return value;

            var toPrimitive = @object[(IJSSymbol)JSSymbol.toPrimitive];
            if (!toPrimitive.IsUndefined && !toPrimitive.IsNull)
            {
                var primitive = toPrimitive.InvokeFunction(new Arguments(@object, JSConstants.Number));
                if (primitive.IsObject)
                    throw JSEngine.NewTypeError("Cannot convert object to primitive value");

                return primitive;
            }

            if (@object[KeyStrings.valueOf] is IJSFunction valueOf)
            {
                var primitive = valueOf.InvokeFunction(new Arguments(@object));
                if (!primitive.IsObject)
                    return primitive;
            }

            if (@object[KeyStrings.toString] is IJSFunction toString)
            {
                var primitive = toString.InvokeFunction(new Arguments(@object));
                if (!primitive.IsObject)
                    return primitive;
            }

            throw JSEngine.NewTypeError("Cannot convert object to primitive value");
        }

        if (context[KeyStrings.Date] is not JSFunction dateCtor)
            return;

        var prototype = dateCtor.prototype;
        var toGMTStringKey = KeyStrings.GetOrCreate("toGMTString");
        var toUTCStringKey = KeyStrings.GetOrCreate("toUTCString");
        var setYearKey = KeyStrings.GetOrCreate("setYear");
        var getYearKey = KeyStrings.GetOrCreate("getYear");

        var setYear = new JSFunction(JSDate.SetYearLegacy, "setYear", "function setYear() { [native code] }", length: 1, createPrototype: false);
        prototype.FastAddValue(setYearKey, setYear, JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(getYearKey, new JSFunction(JSDate.GetYearLegacy, "getYear", "function getYear() { [native code] }", length: 0, createPrototype: false), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.GetOrCreate("toJSON"), CreateNativeFunction(static (in Arguments a) =>
        {
            var receiver = a.This;
            var @object = receiver as JSObject;
            if (@object == null)
            {
                if (receiver.IsNullOrUndefined)
                    throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

                @object = (JSObject)JSObject.CreatePrimitiveObject(receiver);
            }

            var primitive = ToNumberPrimitive(@object);
            if (primitive.IsNumber)
            {
                var number = primitive.DoubleValue;
                if (double.IsNaN(number) || double.IsInfinity(number))
                    return JSNull.Value;
            }

            var toISOString = @object[KeyStrings.GetOrCreate("toISOString")];
            return toISOString.InvokeFunction(new Arguments(@object));
        }, "toJSON", 1), JSPropertyAttributes.ConfigurableValue);

        if (prototype.GetOwnPropertyDescriptor(JSSymbol.toPrimitive).IsUndefined)
        {
            ref var symbols = ref prototype.GetSymbols();
            symbols.Put(JSSymbol.toPrimitive.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
            {
                if (a.This is not JSObject @object)
                    throw JSEngine.NewTypeError("Date.prototype[Symbol.toPrimitive] requires an object receiver");

                var hint = a.Get1();
                if (!hint.IsString)
                    throw JSEngine.NewTypeError("Date.prototype[Symbol.toPrimitive] requires a valid hint");

                return hint.StringValue switch
                {
                    "string" or "default" => OrdinaryToPrimitive(@object, preferString: true),
                    "number" => OrdinaryToPrimitive(@object, preferString: false),
                    _ => throw JSEngine.NewTypeError("Date.prototype[Symbol.toPrimitive] requires a valid hint")
                };
            }, "[Symbol.toPrimitive]", 1), JSPropertyAttributes.ConfigurableReadonlyValue);
        }

        var toUTCString = prototype[toUTCStringKey];
        if (!toUTCString.IsUndefined)
            prototype.FastAddValue(toGMTStringKey, toUTCString, JSPropertyAttributes.ConfigurableValue);
        prototype.Dirty();
    }

    private static void PatchCompatibilityBuiltIns(JSContext context)
    {
        PatchStringPrototype(context);
        PatchErrorPrototype(context);
        PatchObjectPrototype(context);
        PatchPromisePrototype(context);
        PatchFunctionPrototype(context);
        PatchProxyConstructor(context);
        PatchSpeciesConstructors(context);
        PatchSymbolPrototype(context);
        PatchDatePrototype(context);
        PatchRegExpPrototype(context);
        PatchArrayPrototype(context);
        PatchTypedArrayBuiltIns(context);
        PatchToStringTags(context);
        PatchDisposableStacks(context);
        PatchTemporal(context);
    }

    // Builds the Temporal namespace object and attaches the Temporal.Duration /
    // Temporal.Instant constructors (registered via Register = false so they are NOT
    // globals). Each constructor's prototype carries its "Temporal.X" @@toStringTag.
    private static void PatchTemporal(JSContext context)
    {
        var temporal = new JSObject();

        void Attach(string name, JSFunction ctor)
        {
            if (ctor.prototype is JSObject proto)
                SetToStringTag(proto, $"Temporal.{name}");
            temporal.FastAddValue(KeyStrings.GetOrCreate(name), ctor, JSPropertyAttributes.ConfigurableValue);
        }

        // Implemented types.
        Attach("Duration", Temporal.JSTemporalDuration.CreateClass(context, register: false));
        Attach("Instant", Temporal.JSTemporalInstant.CreateClass(context, register: false));

        // Stub types (constructors exist but throw until implemented — see JSTemporalStubs.cs).
        Attach("PlainDate", Temporal.JSTemporalPlainDate.CreateClass(context, register: false));
        Attach("PlainTime", Temporal.JSTemporalPlainTime.CreateClass(context, register: false));
        Attach("PlainDateTime", Temporal.JSTemporalPlainDateTime.CreateClass(context, register: false));
        Attach("PlainYearMonth", Temporal.JSTemporalPlainYearMonth.CreateClass(context, register: false));
        Attach("PlainMonthDay", Temporal.JSTemporalPlainMonthDay.CreateClass(context, register: false));
        Attach("ZonedDateTime", Temporal.JSTemporalZonedDateTime.CreateClass(context, register: false));

        // Temporal.Now (proposal §2): the current instant/date/time. Backed by the host clock
        // (DateTimeOffset.UtcNow) and the system local zone; see Temporal.JSTemporalNow.
        var now = new JSObject();
        void AddNow(string name, int length, JSFunctionDelegate fn)
            => now.FastAddValue(
                KeyStrings.GetOrCreate(name),
                new JSFunction(fn, name, $"function {name}() {{ [native code] }}", createPrototype: false, length: length),
                JSPropertyAttributes.ConfigurableValue);

        AddNow("timeZoneId", 0, Temporal.JSTemporalNow.TimeZoneId);
        AddNow("instant", 0, Temporal.JSTemporalNow.Instant);
        AddNow("plainDateTimeISO", 0, Temporal.JSTemporalNow.PlainDateTimeISO);
        AddNow("zonedDateTimeISO", 0, Temporal.JSTemporalNow.ZonedDateTimeISO);
        AddNow("plainDateISO", 0, Temporal.JSTemporalNow.PlainDateISO);
        AddNow("plainTimeISO", 0, Temporal.JSTemporalNow.PlainTimeISO);
        SetToStringTag(now, "Temporal.Now");
        temporal.FastAddValue(KeyStrings.GetOrCreate("Now"), now, JSPropertyAttributes.ConfigurableValue);

        SetToStringTag(temporal, "Temporal");
        context.FastAddValue(KeyStrings.GetOrCreate("Temporal"), temporal, JSPropertyAttributes.ConfigurableValue);
    }

    // Explicit Resource Management: wire the symbol-keyed disposal aliases and toStringTags
    // for the user-facing DisposableStack / AsyncDisposableStack built-ins. The source
    // generator cannot key a [JSExport] on a well-known symbol, so the spec-mandated
    // prototype[@@dispose] === prototype.dispose (same function object) is established here.
    private static void PatchDisposableStacks(JSContext context)
    {
        if (context[KeyStrings.GetOrCreate("DisposableStack")] is JSFunction disposableStackCtor
            && disposableStackCtor.prototype is JSObject disposableProto)
        {
            AliasSymbolMethod(disposableProto, JSSymbol.dispose, KeyStrings.GetOrCreate("dispose"));
            SetToStringTag(disposableProto, "DisposableStack");
        }

        if (context[KeyStrings.GetOrCreate("AsyncDisposableStack")] is JSFunction asyncDisposableStackCtor
            && asyncDisposableStackCtor.prototype is JSObject asyncDisposableProto)
        {
            AliasSymbolMethod(asyncDisposableProto, JSSymbol.asyncDispose, KeyStrings.GetOrCreate("disposeAsync"));
            SetToStringTag(asyncDisposableProto, "AsyncDisposableStack");
        }
    }

    // prototype[symbol] is the same function object as prototype[methodName].
    private static void AliasSymbolMethod(JSObject prototype, JSSymbol symbol, in KeyString methodName)
    {
        var method = prototype[methodName];
        if (method is JSFunction)
            prototype.FastAddValue((IJSSymbol)symbol, method, JSPropertyAttributes.ConfigurableValue);
    }

    // Aliases built-in functions that the spec requires to be the SAME object across
    // two homes but which the source generator emits as distinct copies. Must run after
    // the full registration chain (BuiltIns AND Globals) so both homes exist regardless
    // of module-initializer order — invoked from DefaultBuiltInRegistry.Register.
    internal static void PatchNumberConstructor(JSContext context)
    {
        if (context[KeyStrings.Number] is not JSFunction numberCtor)
            return;

        // Number.parseFloat and Number.parseInt are the SAME built-in function objects
        // as the global parseFloat / parseInt (spec 21.1.2.12-13), so alias the global
        // ones onto the constructor rather than keeping the distinct generated copies.
        var parseFloatKey = KeyStrings.GetOrCreate("parseFloat");
        var parseIntKey = KeyStrings.GetOrCreate("parseInt");

        if (context[parseFloatKey] is IJSFunction globalParseFloat)
            numberCtor.FastAddValue(parseFloatKey, (JSValue)globalParseFloat, JSPropertyAttributes.ConfigurableValue);

        if (context[parseIntKey] is IJSFunction globalParseInt)
            numberCtor.FastAddValue(parseIntKey, (JSValue)globalParseInt, JSPropertyAttributes.ConfigurableValue);
    }

    private static JSFunction CreateNativeFunction(JSFunctionDelegate fx, string name, int length = 0)
        => new(fx, name, $"function {NativeFunctionToStringName(name)}() {{ [native code] }}", length: length, createPrototype: false);

    private static JSFunction CreateNativeGetter(JSFunctionDelegate fx, string name)
        => new(fx, $"get {name}", $"function get {NativeFunctionToStringName(name)}() {{ [native code] }}", createPrototype: false, length: 0);

    private static JSFunction CreateNativeSetter(JSFunctionDelegate fx, string name)
        => new(fx, $"set {name}", $"function set {NativeFunctionToStringName(name)}() {{ [native code] }}", createPrototype: false, length: 1);

    // The optional IdentifierName in a NativeFunction toString must be a valid
    // IdentifierName or a computed `[ … ]` name (e.g. "[Symbol.replace]"). A property
    // name that is neither — notably the legacy RegExp statics "$&", "$+", "$`", "$'" —
    // must be omitted, since the IdentifierName is optional and emitting it verbatim
    // (`function get $&() { [native code] }`) is not valid NativeFunction syntax.
    private static string NativeFunctionToStringName(string name)
        => IsValidNativeFunctionToStringName(name) ? name : string.Empty;

    private static bool IsValidNativeFunctionToStringName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // A computed `[ … ]` name is accepted by the NativeFunction grammar.
        if (name[0] == '[')
            return true;

        if (name[0] != '$' && name[0] != '_' && !char.IsLetter(name[0]))
            return false;

        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (c != '$' && c != '_' && !char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }

    private static void EnsureAccessorProperty(JSObject target, JSValue key, string name, JSFunctionDelegate getter, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableProperty)
    {
        if (!target.GetOwnPropertyDescriptor(key).IsUndefined)
            return;

        target.FastAddProperty(key, CreateNativeGetter(getter, name), null, attributes);
    }

    private static void EnsureAccessorProperty(JSObject target, KeyString key, string name, JSFunctionDelegate getter, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableProperty)
        => EnsureAccessorProperty(target, key, name, getter, null, attributes);

    private static void EnsureAccessorProperty(JSObject target, KeyString key, string name, JSFunctionDelegate getter, JSFunctionDelegate setter, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableProperty)
    {
        if (!target.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(key.ToString(), key)).IsUndefined)
            return;

        target.FastAddProperty(key, CreateNativeGetter(getter, name), setter == null ? null : CreateNativeSetter(setter, name), attributes);
    }

    private static void PatchSpeciesConstructors(JSContext context)
    {
        PatchSpeciesConstructor(context, KeyStrings.Array);
        PatchSpeciesConstructor(context, KeyStrings.Promise);
        PatchSpeciesConstructor(context, KeyStrings.Map);
        PatchSpeciesConstructor(context, KeyStrings.Set);
        PatchSpeciesConstructor(context, KeyStrings.RegExp);
        PatchSpeciesConstructor(context, KeyStrings.GetOrCreate("ArrayBuffer"));
        PatchSpeciesConstructor(context, KeyStrings.GetOrCreate("TypedArray"));
    }

    private static void PatchSpeciesConstructor(JSContext context, KeyString constructorName)
    {
        if (context[constructorName] is not JSObject constructor)
            return;

        EnsureAccessorProperty(constructor, JSSymbol.species, "[Symbol.species]", static (in Arguments a) => a.This);
    }

    private static void PatchStringPrototype(JSContext context)
    {
        if (context[KeyStrings.String] is not JSFunction stringCtor)
            return;

        var prototype = stringCtor.prototype;
        var atKey = KeyStrings.GetOrCreate("at");
        var trimStart = prototype[KeyStrings.GetOrCreate("trimStart")];
        var trimEnd = prototype[KeyStrings.GetOrCreate("trimEnd")];
        if (prototype[atKey].IsUndefined)
        {
            prototype.FastAddValue(atKey, CreateNativeFunction(static (in Arguments a) =>
            {
                var text = a.This.AsString();
                var index = a.Get1().IntegerValue;
                if (index < 0)
                    index += text.Length;

                if (index < 0 || index >= text.Length)
                    return JSUndefined.Value;

                return JSValue.CreateString(text[index].ToString());
            }, "at", 1), JSPropertyAttributes.ConfigurableValue);
        }

        if (prototype.GetOwnPropertyDescriptor(JSSymbol.iterator).IsUndefined)
        {
            // Register the named String iterator (JSString.Iterator) rather than a
            // lambda so JSString.GetIterableEnumerator can recognise the built-in
            // default (and keep its fast code-point path) versus a user override.
            // JSString.Iterator uses the raw code-point enumerator, so the override
            // protocol can call it without re-entering GetIterableEnumerator.
            prototype.FastAddValue((IJSSymbol)JSSymbol.iterator, CreateNativeFunction(JSString.Iterator, "[Symbol.iterator]"), JSPropertyAttributes.ConfigurableValue);
        }

        if (!trimStart.IsUndefined)
            prototype.FastAddValue(KeyStrings.GetOrCreate("trimLeft"), trimStart, JSPropertyAttributes.ConfigurableValue);

        if (!trimEnd.IsUndefined)
            prototype.FastAddValue(KeyStrings.GetOrCreate("trimRight"), trimEnd, JSPropertyAttributes.ConfigurableValue);

        prototype.Dirty();
    }

    private static void PatchErrorPrototype(JSContext context)
    {
        if (context[KeyStrings.Error] is not JSFunction errorCtor)
            return;

        var prototype = errorCtor.prototype;
        if (prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(KeyStrings.message.ToString(), KeyStrings.message)).IsUndefined)
            prototype.FastAddValue(KeyStrings.message, JSValue.EmptyString, JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.toString, CreateNativeFunction(static (in Arguments a) =>
        {
            if (a.This is not JSObject @object)
                throw JSEngine.NewTypeError("Error.prototype.toString called on non-object");

            var name = @object[KeyStrings.name];
            var message = @object[KeyStrings.message];

            // Coerce `name` and `message` with the ToString abstract operation
            // (StringValue), not C#'s ToString: a Symbol must throw a TypeError
            // rather than being rendered as "Symbol(...)", and an object must run
            // its own toString. (test262: Error.prototype.toString tostring-message-throws-symbol)
            var nameString = name.IsUndefined ? "Error" : name.StringValue;
            var messageString = message.IsUndefined ? string.Empty : message.StringValue;

            if (nameString.Length == 0)
                return JSValue.CreateString(messageString);

            if (messageString.Length == 0)
                return JSValue.CreateString(nameString);

            return JSValue.CreateString($"{nameString}: {messageString}");
        }, "toString", 0), JSPropertyAttributes.ConfigurableValue);
        prototype.Dirty();
    }

    private static void PatchPromisePrototype(JSContext context)
    {
        if (context[KeyStrings.Promise] is not JSFunction promiseCtor)
            return;

        var prototype = promiseCtor.prototype;
        prototype.FastAddValue(KeyStrings.GetOrCreate("catch"), CreateNativeFunction(static (in Arguments a) =>
        {
            var then = a.This[KeyStrings.then];
            return then.InvokeFunction(new Arguments(a.This, JSUndefined.Value, a.Get1()));
        }, "catch", 1), JSPropertyAttributes.ConfigurableValue);
        prototype.Dirty();
    }

    private static void PatchObjectPrototype(JSContext context)
    {
        static JSObject CoerceObject(JSValue value)
        {
            if (value is JSObject @object)
                return @object;

            if (value.IsNullOrUndefined)
                throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

            return (JSObject)JSObject.CreatePrimitiveObject(value);
        }

        static JSValue ToPropertyKeyValue(JSValue value)
        {
            var key = value.ToKey(false);
            if (key.IsSymbol)
                return (JSSymbol)key.Symbol;

            if (key.IsUInt)
                return JSValue.CreateNumber(key.Index);

            return key.KeyString.ToJSValue();
        }

        static JSValue LookupAccessor(JSValue thisValue, JSValue propertyName, KeyString accessorKey)
        {
            var current = CoerceObject(thisValue);
            var key = ToPropertyKeyValue(propertyName);
            while (true)
            {
                var descriptor = current.GetOwnPropertyDescriptor(key);
                if (!descriptor.IsUndefined)
                {
                    var accessor = descriptor[accessorKey];
                    return accessor.IsUndefined ? JSUndefined.Value : accessor;
                }

                if (current.GetPrototypeOf() is not JSObject next)
                    return JSUndefined.Value;

                current = next;
            }
        }

        if (context[KeyStrings.Object] is not JSFunction objectCtor)
            return;

        var prototype = objectCtor.prototype;
        var hasOwnKey = KeyStrings.GetOrCreate("hasOwn");
        if (objectCtor[hasOwnKey].IsUndefined)
        {
            objectCtor.FastAddValue(hasOwnKey, CreateNativeFunction(static (in Arguments a) =>
            {
                var @object = CoerceObject(a.Get1());
                var key = ToPropertyKeyValue(a.GetAt(1));
                return @object.GetOwnPropertyDescriptor(key).IsUndefined ? JSValue.BooleanFalse : JSValue.BooleanTrue;
            }, "hasOwn", 2), JSPropertyAttributes.ConfigurableValue);
        }

        prototype.FastAddValue(KeyStrings.GetOrCreate("hasOwnProperty"), CreateNativeFunction(static (in Arguments a) =>
        {
            var key = ToPropertyKeyValue(a.Get1());
            var @object = CoerceObject(a.This);
            return @object.GetOwnPropertyDescriptor(key).IsUndefined ? JSValue.BooleanFalse : JSValue.BooleanTrue;
        }, "hasOwnProperty", 1), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddProperty(
            KeyStrings.__proto__,
            CreateNativeGetter(static (in Arguments a) =>
            {
                // Per spec the getter performs ToObject(this value): primitives
                // (string/number/boolean/symbol/bigint) are boxed so that e.g.
                // `'x'.__proto__` resolves to String.prototype; null/undefined throw.
                return CoerceObject(a.This).GetPrototypeOf();
            }, "__proto__"),
            CreateNativeSetter(static (in Arguments a) =>
            {
                if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @object))
                    return JSUndefined.Value;

                var value = a.Get1();
                if (!value.IsObject && !value.IsNull)
                    return JSUndefined.Value;

                @object.SetPrototypeOf(value);
                return JSUndefined.Value;
            }, "__proto__"),
            JSPropertyAttributes.ConfigurableProperty);

        var toLocaleStringKey = KeyStrings.GetOrCreate("toLocaleString");
        if (prototype[toLocaleStringKey].IsUndefined)
        {
            prototype.FastAddValue(toLocaleStringKey, CreateNativeFunction((in Arguments a) =>
            {
                if (a.This.IsNullOrUndefined)
                    throw JSEngine.NewTypeError(JSException.Cannot_convert_undefined_or_null_to_object);

                // §20.1.3.5 Object.prototype.toLocaleString: return ? Invoke(O,
                // "toString") — dispatch to the actual "toString" method (e.g.
                // String/Number.prototype.toString), not the Object.prototype
                // .toString "[object X]" tag. A plain object whose toString is the
                // inherited Object.prototype.toString still yields "[object Object]".
                return a.This.InvokeMethod(KeyStrings.toString, in Arguments.Empty);
            }, "toLocaleString"), JSPropertyAttributes.ConfigurableValue);
        }

        prototype.FastAddValue(KeyStrings.GetOrCreate("__defineGetter__"), CreateNativeFunction(static (in Arguments a) =>
        {
            var target = CoerceObject(a.This);
            var getter = a.GetAt(1);
            if (getter is not IJSFunction)
                throw JSEngine.NewTypeError("Getter must be a function");

            var key = ToPropertyKeyValue(a.Get1());
            var descriptor = new JSObject();
            descriptor[KeyStrings.get] = getter;
            descriptor[KeyStrings.enumerable] = JSValue.BooleanTrue;
            descriptor[KeyStrings.configurable] = JSValue.BooleanTrue;
            var result = target.DefineProperty(key, descriptor);
            if (result.IsBoolean && !result.BooleanValue)
                throw JSEngine.NewTypeError($"Cannot define property {key}");
            return JSUndefined.Value;
        }, "__defineGetter__", 2), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.GetOrCreate("__defineSetter__"), CreateNativeFunction(static (in Arguments a) =>
        {
            var target = CoerceObject(a.This);
            var setter = a.GetAt(1);
            if (setter is not IJSFunction)
                throw JSEngine.NewTypeError("Setter must be a function");

            var key = ToPropertyKeyValue(a.Get1());
            var descriptor = new JSObject();
            descriptor[KeyStrings.set] = setter;
            descriptor[KeyStrings.enumerable] = JSValue.BooleanTrue;
            descriptor[KeyStrings.configurable] = JSValue.BooleanTrue;
            var result = target.DefineProperty(key, descriptor);
            if (result.IsBoolean && !result.BooleanValue)
                throw JSEngine.NewTypeError($"Cannot define property {key}");
            return JSUndefined.Value;
        }, "__defineSetter__", 2), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.GetOrCreate("__lookupGetter__"), CreateNativeFunction(static (in Arguments a) =>
            LookupAccessor(a.This, a.Get1(), KeyStrings.get), "__lookupGetter__", 1), JSPropertyAttributes.ConfigurableValue);
        prototype.FastAddValue(KeyStrings.GetOrCreate("__lookupSetter__"), CreateNativeFunction(static (in Arguments a) =>
            LookupAccessor(a.This, a.Get1(), KeyStrings.set), "__lookupSetter__", 1), JSPropertyAttributes.ConfigurableValue);
        prototype.Dirty();
    }

    private static void PatchFunctionPrototype(JSContext context)
    {
        if (context[KeyStrings.Function] is not JSFunction functionCtor)
            return;

        // Per §10.2.4 / §20.2.3, Function.prototype's "caller" and "arguments" are
        // accessor properties whose [[Get]] AND [[Set]] are the single per-realm
        // %ThrowTypeError% intrinsic — the very same function object across both
        // accessors and both properties (test262 ThrowTypeError/unique-per-realm-*,
        // Function/prototype/{caller,arguments}/prop-desc). %ThrowTypeError% is
        // anonymous (name ""), length 0, and non-extensible/frozen.
        var throwTypeError = Broiler.JavaScript.BuiltIns.Function.JSFunction.GetOrCreateThrowTypeError();

        var callerKey = KeyStrings.GetOrCreate("caller");
        if (functionCtor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(callerKey.ToString(), callerKey)).IsUndefined)
        {
            functionCtor.prototype.FastAddProperty(
                callerKey,
                throwTypeError,
                throwTypeError,
                JSPropertyAttributes.ConfigurableProperty);
        }

        if (functionCtor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(KeyStrings.arguments.ToString(), KeyStrings.arguments)).IsUndefined)
        {
            functionCtor.prototype.FastAddProperty(
                KeyStrings.arguments,
                throwTypeError,
                throwTypeError,
                JSPropertyAttributes.ConfigurableProperty);
        }

        ref var symbols = ref functionCtor.prototype.GetSymbols();
        symbols.Put(JSSymbol.hasInstance.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var constructor = a.This;
            if (!constructor.IsFunction && !ReferenceEquals(constructor, functionCtor.prototype))
                return JSValue.BooleanFalse;

            if (constructor is JSFunction { BoundTargetFunction: JSValue boundTargetFunction } && boundTargetFunction != null && !boundTargetFunction.IsUndefined)
                constructor = boundTargetFunction;

            var value = a.Get1();
            if (!value.IsObject)
                return JSValue.BooleanFalse;

            var prototype = constructor[KeyStrings.prototype];
            if (!prototype.IsObject)
                throw JSEngine.NewTypeError("Function has non-object prototype in instanceof check");

            var current = value.GetPrototypeOf();
            while (current is JSObject currentObject)
            {
                if (ReferenceEquals(currentObject, prototype))
                    return JSValue.BooleanTrue;

                current = currentObject.GetPrototypeOf();
            }

            return JSValue.BooleanFalse;
        }, "[Symbol.hasInstance]", 1), JSPropertyAttributes.ReadonlyValue);
    }

    private static void PatchProxyConstructor(JSContext context)
    {
        var proxyKey = KeyStrings.GetOrCreate("Proxy");
        if (context[proxyKey] is not JSFunction proxyCtor)
            return;

        // §28.2 The Proxy constructor has no "prototype" property at all, because
        // proxy exotic objects have no [[Prototype]] slot to initialize. The export
        // machinery auto-creates one for constructor functions, so strip it here
        // (test262: built-ins/Proxy/proxy-no-prototype).
        ref var ownProperties = ref proxyCtor.GetOwnProperties();
        // The auto-created "prototype" is non-configurable, so make it configurable
        // first and then delete it outright.
        ownProperties.Put(KeyStrings.prototype.Key) = JSProperty.Property(KeyStrings.prototype, JSUndefined.Value, JSPropertyAttributes.ConfigurableValue);
        ownProperties.RemoveAt(KeyStrings.prototype.Key);
    }

    private static void PatchSymbolPrototype(JSContext context)
    {
        if (context[KeyStrings.Symbol] is not JSFunction symbolCtor)
            return;

        static void RewriteWellKnownSymbol(JSFunction ctor, KeyString key, JSSymbol symbol)
        {
            ref var ownProperties = ref ctor.GetOwnProperties();
            ownProperties.Put(key.Key) = JSProperty.Property(
                key,
                symbol,
                JSPropertyAttributes.ReadonlyValue);
        }

        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("asyncDispose"), JSSymbol.asyncDispose);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("dispose"), JSSymbol.dispose);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("asyncIterator"), JSSymbol.asyncIterator);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("hasInstance"), JSSymbol.hasInstance);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("isConcatSpreadable"), JSSymbol.isConcatSpreadable);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("iterator"), JSSymbol.iterator);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("match"), JSSymbol.match);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("matchAll"), JSSymbol.matchAll);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("replace"), JSSymbol.replace);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("search"), JSSymbol.search);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("species"), JSSymbol.species);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("split"), JSSymbol.split);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("toPrimitive"), JSSymbol.toPrimitive);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("toStringTag"), JSSymbol.toStringTag);
        RewriteWellKnownSymbol(symbolCtor, KeyStrings.GetOrCreate("unscopables"), JSSymbol.unscopables);

        ref var symbols = ref symbolCtor.prototype.GetSymbols();
        symbols.Put(JSSymbol.toPrimitive.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            if (a.This is JSSymbol symbol)
                return symbol;

            if (a.This is Broiler.JavaScript.BuiltIns.Symbol.JSSymbolObject symbolObject)
                return symbolObject.WrappedSymbol;

            throw JSEngine.NewTypeError("Symbol.prototype[Symbol.toPrimitive] requires a symbol receiver");
        }, "[Symbol.toPrimitive]", 1), JSPropertyAttributes.ConfigurableValue);

        EnsureAccessorProperty(symbolCtor.prototype, KeyStrings.GetOrCreate("description"), "description", static (in Arguments a) =>
        {
            if (a.This is JSSymbol symbol)
                return symbol.Description == null ? JSUndefined.Value : JSValue.CreateString(symbol.Description);

            if (a.This is Broiler.JavaScript.BuiltIns.Symbol.JSSymbolObject symbolObject)
                return symbolObject.WrappedSymbol.Description == null ? JSUndefined.Value : JSValue.CreateString(symbolObject.WrappedSymbol.Description);

            throw JSEngine.NewTypeError("Symbol.prototype.description requires a symbol receiver");
        });
    }

    private static void PatchDatePrototype(JSContext context)
    {
        if (context[KeyStrings.Date] is not JSFunction dateCtor)
            return;

        // §21.4.4: %Date.prototype% has NO @@toStringTag. Object.prototype.toString tags a
        // Date via its [[DateValue]] internal slot (see ResolveBuiltinToStringTag), which a
        // user-supplied own @@toStringTag is free to override.
        _ = dateCtor;
    }

    private static void PatchRegExpPrototype(JSContext context)
    {
        if (context[KeyStrings.RegExp] is not JSFunction regExpCtor)
            return;

        if (regExpCtor is JSClassFunction originalCtor)
        {
            JSFunction replacement = null;
            replacement = new JSFunction((in Arguments a) =>
            {
                var (pattern, flags) = a.Get2();
                // §22.2.4.1 step 4: the "return the existing RegExp unchanged"
                // optimization applies ONLY to the call form (NewTarget undefined).
                // `new RegExp(re)` (NewTarget defined) must always allocate a fresh
                // instance — otherwise two regexes alias one another's lastIndex.
                var isConstruct = (JSEngine.Current as IJSExecutionContext)?.CurrentNewTarget != null;
                if (!isConstruct && flags.IsUndefined && JSRegExp.IsRegExpLike(pattern))
                {
                    var constructor = pattern[KeyStrings.constructor];
                    if (ReferenceEquals(constructor, replacement) || ReferenceEquals(constructor, originalCtor))
                        return pattern;
                }

                return originalCtor.CreateInstance(a);
            }, "RegExp", "function RegExp() { [native code] }", length: 2, createPrototype: false)
            {
                prototype = originalCtor.prototype
            };
            var functionMetadata = new JSFunction(JSFunction.empty, "Function", "function Function() { [native code] }", length: 1, createPrototype: false);

            // §22.2.5.1: RegExp.prototype is { [[Writable]]: false, [[Enumerable]]: false,
            // [[Configurable]]: false }. The replacement constructor must carry the same
            // non-writable/non-configurable "prototype" data property as the original (a
            // ConfigurableValue here left it writable and configurable, so
            // Object.getOwnPropertyDescriptor(RegExp, "prototype").writable was true).
            replacement.FastAddValue(KeyStrings.prototype, originalCtor.prototype, JSPropertyAttributes.ReadonlyValue);
            replacement.FastAddValue(KeyStrings.constructor, functionMetadata, JSPropertyAttributes.ConfigurableValue);
            EnsureAccessorProperty(replacement, JSSymbol.species, "[Symbol.species]", static (in Arguments a) => a.This);
            originalCtor.prototype[KeyStrings.constructor] = replacement;
            context.FastAddValue(KeyStrings.RegExp, replacement, JSPropertyAttributes.ConfigurableValue);
            regExpCtor = replacement;
        }

        EnsureRegExpEscape(regExpCtor);

        static JSValue GetSpeciesConstructor(JSValue constructor)
        {
            if (constructor.IsUndefined)
                return JSUndefined.Value;

            if (constructor is not JSObject constructorObject)
                throw JSEngine.NewTypeError("RegExp constructor must be an object");

            var species = constructorObject[(IJSSymbol)JSSymbol.species];
            if (!species.IsNullOrUndefined && species is not IJSFunction)
                throw JSEngine.NewTypeError("RegExp species constructor is not a constructor");

            return species;
        }

        static JSValue GetObservableFlags(JSValue regExpValue)
        {
            var sb = new StringBuilder(8);
            if (regExpValue[KeyStrings.GetOrCreate("hasIndices")].BooleanValue)
                sb.Append('d');
            if (regExpValue[KeyStrings.GetOrCreate("global")].BooleanValue)
                sb.Append('g');
            if (regExpValue[KeyStrings.GetOrCreate("ignoreCase")].BooleanValue)
                sb.Append('i');
            if (regExpValue[KeyStrings.GetOrCreate("multiline")].BooleanValue)
                sb.Append('m');
            if (regExpValue[KeyStrings.GetOrCreate("dotAll")].BooleanValue)
                sb.Append('s');
            if (regExpValue[KeyStrings.GetOrCreate("unicode")].BooleanValue)
                sb.Append('u');
            if (regExpValue[KeyStrings.GetOrCreate("unicodeSets")].BooleanValue)
                sb.Append('v');
            if (regExpValue[KeyStrings.GetOrCreate("sticky")].BooleanValue)
                sb.Append('y');

            return JSValue.CreateString(sb.ToString());
        }

        static JSValue RegExpExec(JSValue rx, JSValue input)
        {
            // §22.2.7.1 RegExpExec: a callable "exec" property is used; otherwise (it is absent or any
            // non-callable value such as null or a number) fall back to the builtin RegExpBuiltinExec,
            // which requires a real RegExp receiver. A non-callable "exec" is NOT an error.
            var exec = rx[KeyStrings.GetOrCreate("exec")];
            if (exec.IsFunction)
            {
                var result = exec.InvokeFunction(new Arguments(rx, input));
                if (!result.IsObject && !result.IsNull)
                    throw JSEngine.NewTypeError("RegExp exec result must be an object or null");

                return result;
            }

            if (rx is not JSRegExp regExp)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.replace] called on incompatible receiver");

            return regExp.Exec(new Arguments(rx, input));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SameValue(JSValue left, JSValue right)
        {
            if (left.IsNumber && right.IsNumber)
            {
                var leftNumber = left.DoubleValue;
                var rightNumber = right.DoubleValue;
                if (double.IsNaN(leftNumber) && double.IsNaN(rightNumber))
                    return true;

                if (leftNumber != rightNumber)
                    return false;

                if (leftNumber == 0d)
                    return BitConverter.DoubleToInt64Bits(leftNumber) == BitConverter.DoubleToInt64Bits(rightNumber);

                return true;
            }

            return left.StrictEquals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsPositiveZero(JSValue value) =>
            value.IsNumber
            && value.DoubleValue == 0d
            && BitConverter.DoubleToInt64Bits(value.DoubleValue) == BitConverter.DoubleToInt64Bits(0d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ToLength(JSValue value)
        {
            var length = value.DoubleValue;
            if (double.IsNaN(length) || length <= 0)
                return 0;

            if (length >= int.MaxValue)
                return int.MaxValue;

            return (int)length;
        }

        // ToLength as a double (clamped to the integer index range [0, 2^53-1]) for the
        // lastIndex advance, where the value can legitimately exceed int.MaxValue.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double ToLengthDouble(JSValue value)
        {
            var length = Math.Truncate(value.DoubleValue);
            if (double.IsNaN(length) || length <= 0)
                return 0;

            const double MaxSafeLength = 9007199254740991d; // 2^53 - 1
            return length > MaxSafeLength ? MaxSafeLength : length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AdvanceStringIndex(string input, int index, bool unicode)
        {
            if (!unicode || index + 1 >= input.Length || !char.IsHighSurrogate(input[index]) || !char.IsLowSurrogate(input[index + 1]))
                return index + 1;

            return index + 2;
        }

        static string GetSubstitution(string matched, string input, int position, IReadOnlyList<JSValue> captures, JSValue namedCaptures, string replacement)
        {
            if (replacement.IndexOf('$') < 0)
                return replacement;

            var replacementBuilder = new StringBuilder();
            for (int i = 0; i < replacement.Length; i++)
            {
                var c = replacement[i];
                if (c != '$' || i >= replacement.Length - 1)
                {
                    replacementBuilder.Append(c);
                    continue;
                }

                c = replacement[++i];
                switch (c)
                {
                    case '$':
                        replacementBuilder.Append('$');
                        break;
                    case '&':
                        replacementBuilder.Append(matched);
                        break;
                    case '`':
                        replacementBuilder.Append(input.AsSpan(0, Math.Max(position, 0)));
                        break;
                    case '\'':
                        replacementBuilder.Append(input.AsSpan(Math.Min(position + matched.Length, input.Length)));
                        break;
                    case '<':
                    {
                        // §22.1.3.18.1: $< introduces a named-capture reference ONLY when the
                        // pattern contains named groups (namedCaptures is defined). When it is
                        // undefined, the literal characters "$<" are emitted and parsing resumes
                        // immediately after them, so `"abcd".replace(/(.)(.)/, "$<x>")` yields
                        // "$<x>cd" rather than swallowing the "<x>".
                        if (namedCaptures.IsUndefined)
                        {
                            replacementBuilder.Append("$<");
                            break;
                        }

                        var end = replacement.IndexOf('>', i + 1);
                        if (end < 0)
                        {
                            replacementBuilder.Append("$<");
                            break;
                        }

                        if (namedCaptures is not JSObject namedCapturesObject)
                            throw JSEngine.NewTypeError("RegExp replacement named captures must be an object");

                        var groupName = replacement.Substring(i + 1, end - i - 1);
                        var capture = namedCapturesObject[KeyStrings.GetOrCreate(groupName)];
                        if (!capture.IsUndefined)
                            replacementBuilder.Append(capture.ToString());

                        i = end;
                        break;
                    }
                    default:
                        if (c is >= '0' and <= '9')
                        {
                            var captureIndex = c - '0';
                            if (i < replacement.Length - 1 && replacement[i + 1] is >= '0' and <= '9')
                            {
                                var twoDigitIndex = (captureIndex * 10) + (replacement[i + 1] - '0');
                                if (twoDigitIndex > 0 && twoDigitIndex <= captures.Count)
                                {
                                    captureIndex = twoDigitIndex;
                                    i++;
                                }
                            }

                            if (captureIndex > 0 && captureIndex <= captures.Count)
                            {
                                var capture = captures[captureIndex - 1];
                                if (!capture.IsUndefined)
                                    replacementBuilder.Append(capture.ToString());
                                break;
                            }
                        }

                        replacementBuilder.Append('$');
                        replacementBuilder.Append(c);
                        break;
                }
            }

            return replacementBuilder.ToString();
        }

        ref var symbols = ref regExpCtor.prototype.GetSymbols();
        symbols.Put(JSSymbol.match.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var rx = a.This;
            if (rx is not JSObject rxObj)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.match] called on incompatible receiver");

            var input = a.Get1();
            // §22.2.6.8 step 3: S = ToString(string). Coerce the argument before reading
            // "flags" so its toString/valueOf side effects occur in spec order, and so the
            // string S — not the raw argument — is what RegExpExec (and any user exec) sees.
            var s = input.StringValue;
            var sValue = JSValue.CreateString(s);
            // §22.2.6.8 step 4: flags = ToString(Get(rx, "flags")). Read the
            // observable "flags" property (so a user getter / toString runs and
            // its errors propagate) rather than the individual flag accessors.
            var flags = rx[KeyStrings.GetOrCreate("flags")].StringValue;
            if (!flags.Contains('g'))
            {
                // §22.2.6.8 step 6: when not global, return RegExpExec(rx, S). This
                // must go through exec — which reads `lastIndex` (firing its valueOf)
                // — rather than a fast path that skips that observable read.
                return RegExpExec(rx, sValue);
            }

            // §22.2.6.9 step 7: fullUnicode is derived from the flags STRING (a `u` or
            // `v` flag), so an empty match advances by a whole code point.
            var matchFullUnicode = flags.Contains('u') || flags.Contains('v');
            rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), JSValue.NumberZero);
            var matches = JSValue.CreateArray() as JSObject
                ?? throw new InvalidOperationException("Expected JS array object");
            uint matchCount = 0;
            while (true)
            {
                var result = RegExpExec(rx, sValue);
                if (result.IsNull)
                    return matchCount == 0 ? JSValue.NullValue : matches;

                var matchString = result[0].ToString();
                var descriptor = new JSObject();
                descriptor.FastAddValue(KeyStrings.value, JSValue.CreateString(matchString), JSPropertyAttributes.EnumerableConfigurableValue);
                descriptor.FastAddValue(KeyStrings.writable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
                descriptor.FastAddValue(KeyStrings.enumerable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
                descriptor.FastAddValue(KeyStrings.configurable, JSBoolean.True, JSPropertyAttributes.EnumerableConfigurableValue);
                var defineResult = matches.DefineProperty(matchCount++, descriptor);
                if (defineResult.IsBoolean && !defineResult.BooleanValue)
                    throw JSEngine.NewTypeError("Cannot define match result");
                if (matchString.Length != 0)
                    continue;

                // §22.2.6.9 step 8.g: empty match → lastIndex = AdvanceStringIndex(S,
                // thisIndex, fullUnicode); a surrogate pair counts as one position so the
                // loop does not emit a spurious empty match between the two code units.
                var nextIndex = (int)rx[KeyStrings.lastIndex].DoubleValue;
                var advanced = nextIndex + 1;
                if (matchFullUnicode && advanced < s.Length
                    && char.IsHighSurrogate(s[nextIndex]) && char.IsLowSurrogate(s[advanced]))
                    advanced++;
                rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), JSValue.CreateNumber(advanced));
            }
        }, "[Symbol.match]", 1), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.matchAll.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            // §22.2.6.10 RegExp.prototype [ @@matchAll ] ( string ). The algorithm does not
            // branch on whether the receiver is a "real" RegExp: it always reads the receiver's
            // own "flags" and constructs a fresh matcher through SpeciesConstructor, so every
            // observable read happens for an arbitrary object receiver too.
            //
            // Steps 1-2: the this value must be an Object.
            if (a.This is not JSObject r)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.matchAll] called on a non-object this value");

            // Step 3: S = ToString(string) — observable and sequenced before "flags".
            var s = JSValue.CreateString(a.Get1().StringValue);

            // Step 4: C = SpeciesConstructor(R, %RegExp%). The "constructor" read and the
            // @@species read both precede the "flags" read below.
            var constructor = r[KeyStrings.constructor];
            var species = GetSpeciesConstructor(constructor);

            // Step 5: flags = ToString(Get(R, "flags")). Both the read and its ToString are
            // observable, so a throwing getter / toString must propagate.
            var flagsString = r[KeyStrings.GetOrCreate("flags")].StringValue;
            var flags = JSValue.CreateString(flagsString);

            // Step 6: matcher = Construct(C, « R, flags »). With no @@species the default
            // %RegExp% is used; constructing it runs IsRegExp(R), an observable @@match probe.
            var matcher = species.IsNullOrUndefined
                ? new JSRegExp(new Arguments(JSUndefined.Value, r, flags))
                : species.CreateInstance(new Arguments(species, r, flags));

            // Steps 7-8: matcher.lastIndex = ToLength(Get(R, "lastIndex")).
            matcher[KeyStrings.lastIndex] = JSValue.CreateNumber(ToLength(r[KeyStrings.lastIndex]));

            // Steps 9-11: global / fullUnicode are derived from the flags STRING, not by
            // reading "global"/"unicode" off the constructed matcher (those reads are not
            // observable per spec — a throwing getter must not fire).
            return new JSRegExpStringIterator(
                matcher,
                s,
                flagsString.Contains('g'),
                flagsString.Contains('u') || flagsString.Contains('v'));
        }, "[Symbol.matchAll]", 1), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.replace.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var rx = a.This;
            if (rx is not JSObject)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.replace] called on incompatible receiver");

            var input = a.Get1().StringValue;
            var replaceValue = a.TryGetAt(1, out var second) ? second : JSUndefined.Value;
            var functionalReplace = replaceValue.IsFunction;
            // §22.2.6.11 step 5: when the replacement is not callable it is coerced with the
            // spec ToString (StringValue) — which throws for an object whose toString/valueOf
            // yield no primitive — not the lenient CLR ToString.
            var replacementText = functionalReplace ? null : replaceValue.StringValue;
            // §22.2.6.11 step 7: flags = ToString(Get(rx, "flags")). Read the
            // observable "flags" property so a user getter / toString runs and
            // its errors propagate.
            var flags = rx[KeyStrings.GetOrCreate("flags")].StringValue;
            var global = flags.Contains('g');
            // §22.2.6.11 step 8.c: fullUnicode comes from a `u`/`v` flag, so an empty
            // match advances by a whole code point rather than splitting a surrogate pair.
            var fullUnicode = flags.Contains('u') || flags.Contains('v');

            if (global)
                rx[KeyStrings.lastIndex] = JSValue.NumberZero;

            List<JSValue> results = [];
            while (true)
            {
                var result = RegExpExec(rx, JSValue.CreateString(input));
                if (result.IsNull)
                    break;

                results.Add(result);
                if (!global)
                    break;

                var matchString = result[0].ToString();
                if (matchString.Length != 0)
                    continue;

                // §22.2.6.11 step 14.d: thisIndex = ToLength(Get(rx, "lastIndex")); nextIndex
                // = AdvanceStringIndex(S, thisIndex, fullUnicode). ToLength clamps to 2^53-1,
                // so compute in double — a huge lastIndex (e.g. 2^54) must clamp and advance
                // to 2^53, not overflow a 32-bit int to a negative value.
                double thisIndex = ToLengthDouble(rx[KeyStrings.lastIndex]);
                double advanced = thisIndex + 1;
                if (fullUnicode && advanced < input.Length
                    && char.IsHighSurrogate(input[(int)thisIndex]) && char.IsLowSurrogate(input[(int)thisIndex + 1]))
                    advanced++;
                rx[KeyStrings.lastIndex] = JSValue.CreateNumber(advanced);
            }

            if (results.Count == 0)
                return JSValue.CreateString(input);

            var accumulatedResult = new StringBuilder();
            var nextSourcePosition = 0;
            foreach (var result in results)
            {
                // §22.2.6.11 step 16 evaluates the result properties in this exact order:
                // LengthOfArrayLike ("length") → Get "0" → Get "index" → captures loop
                // ("1" through "nCaptures") → Get "groups". The original implementation
                // read "0" before "length" AND read "groups" before the captures loop —
                // both observable through a Proxy result and breaking test262
                // sm/RegExp/replace-trace.
                var capturesLength = Math.Max((int)result[KeyStrings.length].DoubleValue, 0);
                var matched = result[0].ToString();
                var position = (int)result[KeyStrings.index].DoubleValue;

                List<JSValue> captures = [];
                for (var i = 1; i < capturesLength; i++)
                {
                    var capture = result[(uint)i];
                    captures.Add(capture.IsUndefined ? JSUndefined.Value : JSValue.CreateString(capture.ToString()));
                }

                var namedCaptures = result[KeyStrings.GetOrCreate("groups")];

                string replacement;
                if (functionalReplace)
                {
                    List<JSValue> replacerArgs = [JSValue.CreateString(matched)];
                    replacerArgs.AddRange(captures);
                    replacerArgs.Add(JSValue.CreateNumber(position));
                    replacerArgs.Add(JSValue.CreateString(input));
                    // The functional replacer receives the raw "groups" value (null, a number,
                    // an object, ...) unchanged as its final argument; only an undefined value
                    // is omitted. No ToObject coercion happens on this path.
                    if (!namedCaptures.IsUndefined)
                        replacerArgs.Add(namedCaptures);

                    replacement = replaceValue.InvokeFunction(new Arguments(JSUndefined.Value, replacerArgs.ToArray())).ToString();
                }
                else
                {
                    // The string path coerces a present "groups" value with ToObject, so a null
                    // (or other non-coercible) value is a TypeError before substitution.
                    JSValue normalizedNamedCaptures = JSUndefined.Value;
                    if (!namedCaptures.IsUndefined)
                    {
                        if (namedCaptures.IsNull)
                            throw JSEngine.NewTypeError("RegExp replacement named captures must be an object");

                        normalizedNamedCaptures = namedCaptures as JSObject
                            ?? (JSObject)JSObject.CreatePrimitiveObject(namedCaptures);
                    }

                    replacement = GetSubstitution(matched, input, position, captures, normalizedNamedCaptures, replacementText);
                }

                // §22.2.6.11 step 16.p: only accumulate the replacement when position has
                // not moved backwards. An ill-behaving subclass whose exec returns a result
                // with index < nextSourcePosition (e.g. the test262 g-pos-decrement case)
                // must skip both the gap and the substitution — and must NOT rewind
                // nextSourcePosition — so the previously emitted suffix stays intact.
                if (position >= nextSourcePosition)
                {
                    if (position > nextSourcePosition)
                        accumulatedResult.Append(input.AsSpan(nextSourcePosition, position - nextSourcePosition));

                    accumulatedResult.Append(replacement);
                    nextSourcePosition = Math.Min(position + matched.Length, input.Length);
                }
            }

            if (nextSourcePosition < input.Length)
                accumulatedResult.Append(input.AsSpan(nextSourcePosition));

            return JSValue.CreateString(accumulatedResult.ToString());
        }, "[Symbol.replace]", 2), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.search.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var rx = a.This;
            if (rx is not JSObject rxObj)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.search] called on incompatible receiver");

            var previousLastIndex = rx[KeyStrings.lastIndex];
            if (!IsPositiveZero(previousLastIndex))
                rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), JSValue.NumberZero);

            var result = RegExpExec(rx, JSValue.CreateString(a.Get1().StringValue));
            var currentLastIndex = rx[KeyStrings.lastIndex];
            if (!SameValue(currentLastIndex, previousLastIndex))
                rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), previousLastIndex);

            return result.IsObject ? result[KeyStrings.index] : JSValue.NumberMinusOne;
        }, "[Symbol.search]", 1), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.split.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var rx = a.This;
            if (rx is not JSObject)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.split] called on incompatible receiver");

            var input = a.Get1().StringValue;
            var constructor = rx[KeyStrings.constructor];
            var species = GetSpeciesConstructor(constructor);
            var flags = rx[KeyStrings.GetOrCreate("flags")].StringValue;
            var unicodeMatching = flags.Contains('u') || flags.Contains('v');
            var newFlags = flags.Contains('y') ? flags : $"{flags}y";
            var splitter = species.IsNullOrUndefined
                ? new JSRegExp(new Arguments(JSUndefined.Value, rx, JSValue.CreateString(newFlags)))
                : species.CreateInstance(new Arguments(species, rx, JSValue.CreateString(newFlags)));

            var limit = a.TryGetAt(1, out var second) ? second.UIntValue : uint.MaxValue;
            var results = JSValue.CreateArray();
            if (limit == 0)
                return results;

            if (input.Length == 0)
            {
                var emptyResult = RegExpExec(splitter, JSValue.CreateString(input));
                if (!emptyResult.IsNull)
                    return results;

                results.AddArrayItem(JSValue.CreateString(input));
                return results;
            }

            var p = 0;
            var q = 0;
            while (q < input.Length)
            {
                splitter[KeyStrings.lastIndex] = JSValue.CreateNumber(q);
                var z = RegExpExec(splitter, JSValue.CreateString(input));
                if (z.IsNull)
                {
                    q = AdvanceStringIndex(input, q, unicodeMatching);
                    continue;
                }

                var e = ToLength(splitter[KeyStrings.lastIndex]);
                if (e == p)
                {
                    q = AdvanceStringIndex(input, q, unicodeMatching);
                    continue;
                }

                results.AddArrayItem(JSValue.CreateString(input.Substring(p, q - p)));
                if (results.Length >= limit)
                    return results;

                p = Math.Min(e, input.Length);
                var captureCount = ToLength(z[KeyStrings.length]);
                for (var i = 1; i < captureCount; i++)
                {
                    results.AddArrayItem(z[(uint)i]);
                    if (results.Length >= limit)
                        return results;
                }

                q = p;
            }

            results.AddArrayItem(JSValue.CreateString(input.Substring(Math.Min(p, input.Length))));
            return results;
        }, "[Symbol.split]", 2), JSPropertyAttributes.ConfigurableValue);
        regExpCtor.prototype.FastAddProperty(
            KeyStrings.GetOrCreate("flags"),
            CreateNativeGetter(static (in Arguments a) =>
            {
                if (a.This is not JSObject receiver)
                    throw JSEngine.NewTypeError("RegExp.prototype.flags called on incompatible receiver");

                return GetObservableFlags(receiver);
            }, "flags"),
            null,
            JSPropertyAttributes.ConfigurableProperty);

        // §22.2.6 — The single-flag accessors (global, ignoreCase, …) and source
        // are generic getters that special-case %RegExp.prototype%. When invoked
        // with the prototype itself as the receiver they return undefined (or
        // "(?:)" for source) instead of throwing, because the prototype is an
        // ordinary object without the regular-expression internal slots.
        var regExpPrototype = regExpCtor.prototype;
        PatchRegExpFlagGetter(regExpPrototype, "global", 'g');
        PatchRegExpFlagGetter(regExpPrototype, "ignoreCase", 'i');
        PatchRegExpFlagGetter(regExpPrototype, "multiline", 'm');
        PatchRegExpFlagGetter(regExpPrototype, "dotAll", 's');
        PatchRegExpFlagGetter(regExpPrototype, "unicode", 'u');
        PatchRegExpFlagGetter(regExpPrototype, "unicodeSets", 'v');
        PatchRegExpFlagGetter(regExpPrototype, "sticky", 'y');
        PatchRegExpFlagGetter(regExpPrototype, "hasIndices", 'd');

        regExpPrototype.FastAddProperty(
            KeyStrings.GetOrCreate("source"),
            CreateNativeGetter((in Arguments a) =>
            {
                if (a.This is JSRegExp regExp)
                    return JSValue.CreateString(regExp.Source);
                if (ReferenceEquals(a.This, regExpPrototype))
                    return JSValue.CreateString("(?:)");
                throw JSEngine.NewTypeError("RegExp.prototype.source called on incompatible receiver");
            }, "source"),
            null,
            JSPropertyAttributes.ConfigurableProperty);

        PatchLegacyRegExpAccessor(regExpCtor, "lastMatch", "$&", static (in Arguments _) => LegacyRegExpValue(static s => s.LastMatch));
        PatchLegacyRegExpAccessor(regExpCtor, "lastParen", "$+", static (in Arguments _) => LegacyRegExpValue(static s => s.LastParen));
        PatchLegacyRegExpAccessor(regExpCtor, "leftContext", "$`", static (in Arguments _) => LegacyRegExpValue(static s => s.LeftContext));
        PatchLegacyRegExpAccessor(regExpCtor, "rightContext", "$'", static (in Arguments _) => LegacyRegExpValue(static s => s.RightContext));
        // RegExp.input / $_ are the only legacy statics with a setter
        // (SetLegacyRegExpStaticProperty); the rest are get-only.
        PatchLegacyRegExpAccessor(regExpCtor, "input", "$_",
            static (in Arguments _) => LegacyRegExpValue(static s => s.Input),
            static (in Arguments a) =>
            {
                if (JSEngine.Current?.LegacyRegExp is { } state)
                    state.Input = a.Get1().ToString();
                return JSUndefined.Value;
            });

        for (var i = 1; i <= 9; i++)
        {
            var n = i;
            PatchLegacyRegExpAccessor(regExpCtor, $"${i}", (in Arguments _) => LegacyRegExpValue(s => s.Paren(n)));
        }
    }

    // Reads a legacy RegExp static (RegExp.lastMatch, RegExp.$1, …) from the current
    // realm's match record. Before any successful match the record is empty, so each
    // accessor reports the empty string.
    private static JSValue LegacyRegExpValue(Func<LegacyRegExpState, string> selector)
    {
        var state = JSEngine.Current?.LegacyRegExp;
        return state == null ? JSValue.EmptyString : JSValue.CreateString(selector(state));
    }

    // Registers a spec-compliant single-flag accessor on %RegExp.prototype%.
    // The getter returns the flag's boolean state for a real RegExp instance,
    // undefined when invoked on %RegExp.prototype% itself, and throws otherwise.
    // It is a get-only accessor (no setter) per §22.2.6.
    private static void PatchRegExpFlagGetter(JSObject prototype, string name, char flag)
    {
        prototype.FastAddProperty(
            KeyStrings.GetOrCreate(name),
            CreateNativeGetter((in Arguments a) =>
            {
                if (a.This is IJSRegExp regExp)
                    return regExp.Flags.Contains(flag) ? JSValue.BooleanTrue : JSValue.BooleanFalse;
                if (ReferenceEquals(a.This, prototype))
                    return JSUndefined.Value;
                throw JSEngine.NewTypeError($"RegExp.prototype.{name} called on incompatible receiver");
            }, name),
            null,
            JSPropertyAttributes.ConfigurableProperty);
    }

    private static void EnsureRegExpEscape(JSObject regExpCtor)
    {
        var escapeKey = KeyStrings.GetOrCreate("escape");
        if (!regExpCtor.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(escapeKey.ToString(), escapeKey)).IsUndefined)
            return;

        regExpCtor.FastAddValue(escapeKey, CreateNativeFunction(JSRegExp.Escape, "escape", 1), JSPropertyAttributes.ConfigurableValue);
    }

    private static void PatchLegacyRegExpAccessor(JSObject regExpCtor, string propertyName, string alias, JSFunctionDelegate getter, JSFunctionDelegate setter = null)
    {
        PatchLegacyRegExpAccessor(regExpCtor, propertyName, getter, setter);
        PatchLegacyRegExpAccessor(regExpCtor, alias, getter, setter);
    }

    private static void PatchLegacyRegExpAccessor(JSObject regExpCtor, string propertyName, JSFunctionDelegate getter, JSFunctionDelegate setter = null)
    {
        // §B.2.4 GetLegacyRegExpStaticProperty / SetLegacyRegExpStaticProperty step 2:
        // these statics belong to %RegExp% itself. If SameValue(%RegExp%, thisValue) is
        // false — a subclass constructor, a RegExp instance, %RegExp.prototype%, or a
        // primitive receiver — throw a TypeError rather than reading or writing the slot
        // (test262 annexB/.../legacy-accessors/{this-not-regexp-constructor,this-subclass-constructor}).
        JSValue GuardedGet(in Arguments a)
        {
            if (!ReferenceEquals(a.This, regExpCtor))
                throw JSEngine.NewTypeError($"RegExp.{propertyName} getter called on incompatible receiver");
            return getter(a);
        }

        JSFunctionDelegate guardedSetter = null;
        if (setter != null)
        {
            guardedSetter = (in Arguments a) =>
            {
                if (!ReferenceEquals(a.This, regExpCtor))
                    throw JSEngine.NewTypeError($"RegExp.{propertyName} setter called on incompatible receiver");
                return setter(a);
            };
        }

        EnsureAccessorProperty(regExpCtor, KeyStrings.GetOrCreate(propertyName), propertyName, GuardedGet, guardedSetter);
    }

    private static void PatchArrayPrototype(JSContext context)
    {
        if (context[KeyStrings.Array] is not JSFunction arrayCtor)
            return;

        ref var symbols = ref arrayCtor.prototype.GetSymbols();
        if (!symbols.TryGetValue(JSSymbol.unscopables.Key, out var property) || property.IsEmpty || property.value is not JSObject unscopables)
        {
            // §23.1.3.40: Array.prototype[@@unscopables] is OrdinaryObjectCreate(null) —
            // a null-prototype object, so Object.getPrototypeOf(...) is null.
            unscopables = new JSObject();
            unscopables.BasePrototypeObject = null;
            // §23.1.3.40: { [[Writable]]: false, [[Enumerable]]: false, [[Configurable]]: true }
            symbols.Put(JSSymbol.unscopables.Key) = JSProperty.Property(unscopables, JSPropertyAttributes.ConfigurableReadonlyValue);
        }

        // §23.1.3.40: each entry is CreateDataPropertyOrThrow(list, name, true), i.e.
        // a { value: true, writable, enumerable, configurable } data property.
        foreach (var name in ArrayUnscopableNames)
            unscopables.FastAddValue(KeyStrings.GetOrCreate(name), JSValue.BooleanTrue, JSPropertyAttributes.EnumerableConfigurableValue);
    }

    // The @@unscopables list for Array.prototype, in spec order (§23.1.3.40).
    // Note: the change-array-by-copy proposal added toReversed/toSorted/toSpliced to this
    // list but deliberately NOT "with" — "with" is a reserved word and can never name a
    // binding shadowed inside a `with` statement, so it is absent from the spec list.
    private static readonly string[] ArrayUnscopableNames =
    {
        "at", "copyWithin", "entries", "fill", "find", "findIndex",
        "findLast", "findLastIndex", "flat", "flatMap", "includes",
        "keys", "toReversed", "toSorted", "toSpliced", "values"
    };

    private static readonly string[] TypedArrayConstructorNames =
    {
        "Int8Array", "Uint8Array", "Uint8ClampedArray",
        "Int16Array", "Uint16Array",
        "Int32Array", "Uint32Array",
        "BigInt64Array", "BigUint64Array",
        "Float16Array", "Float32Array", "Float64Array"
    };

    private static void PatchTypedArrayBuiltIns(JSContext context)
    {
        if (context[KeyStrings.GetOrCreate("TypedArray")] is not JSFunction typedArrayCtor)
            return;

        static JSTypedArray RequireTypedArray(JSValue value)
            => value as JSTypedArray ?? throw JSEngine.NewTypeError("Failed to convert this to JSTypedArray");

        var prototype = typedArrayCtor.prototype;

        // BYTES_PER_ELEMENT is a { writable: false, enumerable: false, configurable: false } data
        // property on each concrete TypedArray constructor AND its prototype. The source generator
        // only emits the constructor copy, so mirror that value onto the prototype here. (It must
        // not live on %TypedArray%.prototype as an accessor — that would throw on access.)
        foreach (var typedArrayName in TypedArrayConstructorNames)
        {
            if (context[KeyStrings.GetOrCreate(typedArrayName)] is not JSFunction ctor
                || ctor.prototype is not JSObject ctorPrototype)
            {
                continue;
            }

            var bytesPerElement = ctor[Names.BYTES_PER_ELEMENT];
            if (!bytesPerElement.IsUndefined)
                ctorPrototype.FastAddValue(Names.BYTES_PER_ELEMENT, bytesPerElement, JSPropertyAttributes.ReadonlyValue);
        }

        // %TypedArray%.prototype.toString is the SAME built-in function object as
        // Array.prototype.toString (spec 23.2.3.31), so alias it instead of installing a
        // distinct copy (so `Int8Array.prototype.toString === Array.prototype.toString`).
        if (context[KeyStrings.Array] is JSFunction arrayCtor
            && arrayCtor.prototype is JSObject arrayPrototype
            && arrayPrototype[KeyStrings.toString] is JSFunction arrayToString)
        {
            prototype.FastAddValue(KeyStrings.toString, arrayToString, JSPropertyAttributes.ConfigurableValue);
        }

        EnsureAccessorProperty(prototype, JSSymbol.toStringTag, "[Symbol.toStringTag]", static (in Arguments a) =>
        {
            return GetTypedArrayTag(a.This);
        });

        var findLastKey = KeyStrings.GetOrCreate("findLast");
        if (prototype[findLastKey].IsUndefined)
        {
            prototype.FastAddValue(findLastKey, CreateNativeFunction(static (in Arguments a) =>
            {
                var typedArray = RequireTypedArray(a.This);
                var (callback, thisArg) = a.Get2();
                if (callback is not IJSFunction fn)
                    throw JSEngine.NewTypeError($"{callback} is not a function in TypedArray.prototype.findLast");

                for (var index = typedArray.Length - 1; index >= 0; index--)
                {
                    var item = typedArray[(uint)index];
                    if (fn.InvokeFunction(new Arguments(thisArg, item, JSValue.CreateNumber(index), typedArray)).BooleanValue)
                        return item;
                }

                return JSUndefined.Value;
            }, "findLast", 1), JSPropertyAttributes.ConfigurableValue);
        }

        var findLastIndexKey = KeyStrings.GetOrCreate("findLastIndex");
        if (prototype[findLastIndexKey].IsUndefined)
        {
            prototype.FastAddValue(findLastIndexKey, CreateNativeFunction(static (in Arguments a) =>
            {
                var typedArray = RequireTypedArray(a.This);
                var (callback, thisArg) = a.Get2();
                if (callback is not IJSFunction fn)
                    throw JSEngine.NewTypeError($"{callback} is not a function in TypedArray.prototype.findLastIndex");

                for (var index = typedArray.Length - 1; index >= 0; index--)
                {
                    var item = typedArray[(uint)index];
                    if (fn.InvokeFunction(new Arguments(thisArg, item, JSValue.CreateNumber(index), typedArray)).BooleanValue)
                        return JSValue.CreateNumber(index);
                }

                return JSNumber.MinusOne;
            }, "findLastIndex", 1), JSPropertyAttributes.ConfigurableValue);
        }

        prototype.Dirty();
    }

    private static JSValue GetTypedArrayTag(JSValue value) => value switch
    {
        JSInt8Array => JSValue.CreateString("Int8Array"),
        JSUInt8Array => JSValue.CreateString("Uint8Array"),
        JSUint8ClampedArray => JSValue.CreateString("Uint8ClampedArray"),
        JSInt16Array => JSValue.CreateString("Int16Array"),
        JSUInt16Array => JSValue.CreateString("Uint16Array"),
        JSInt32Array => JSValue.CreateString("Int32Array"),
        JSUInt32Array => JSValue.CreateString("Uint32Array"),
        JSBigInt64Array => JSValue.CreateString("BigInt64Array"),
        JSBigUint64Array => JSValue.CreateString("BigUint64Array"),
        JSFloat16Array => JSValue.CreateString("Float16Array"),
        JSFloat32Array => JSValue.CreateString("Float32Array"),
        JSFloat64Array => JSValue.CreateString("Float64Array"),
        _ => JSUndefined.Value
    };

    private static void SetToStringTag(JSObject target, string tag)
    {
        if (!target.GetOwnPropertyDescriptor(JSSymbol.toStringTag).IsUndefined)
            return;

        target.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString(tag), JSPropertyAttributes.ConfigurableReadonlyValue);
    }

    private static void PatchToStringTags(JSContext context)
    {
        // BigInt.prototype[@@toStringTag] = "BigInt"
        if (context[KeyStrings.GetOrCreate("BigInt")] is JSFunction bigIntCtor && bigIntCtor.prototype is JSObject bigIntProto)
            SetToStringTag(bigIntProto, "BigInt");

        // Math[@@toStringTag] = "Math"
        if (context[KeyStrings.GetOrCreate("Math")] is JSObject mathObject)
            SetToStringTag(mathObject, "Math");

        // JSON[@@toStringTag] = "JSON"
        if (context[KeyStrings.GetOrCreate("JSON")] is JSObject jsonObject)
            SetToStringTag(jsonObject, "JSON");

        // WeakRef.prototype[@@toStringTag] = "WeakRef"
        if (context[KeyStrings.GetOrCreate("WeakRef")] is JSFunction weakRefCtor && weakRefCtor.prototype is JSObject weakRefProto)
            SetToStringTag(weakRefProto, "WeakRef");

        // FinalizationRegistry.prototype[@@toStringTag] = "FinalizationRegistry"
        if (context[KeyStrings.GetOrCreate("FinalizationRegistry")] is JSFunction finRegCtor && finRegCtor.prototype is JSObject finRegProto)
            SetToStringTag(finRegProto, "FinalizationRegistry");

        // Reflect[@@toStringTag] = "Reflect"
        if (context[KeyStrings.GetOrCreate("Reflect")] is JSObject reflect)
            SetToStringTag(reflect, "Reflect");

        // Map.prototype[@@toStringTag] = "Map"
        if (context[KeyStrings.Map] is JSFunction mapCtor && mapCtor.prototype is JSObject mapProto)
            SetToStringTag(mapProto, "Map");

        // Set.prototype[@@toStringTag] = "Set"
        if (context[KeyStrings.Set] is JSFunction setCtor && setCtor.prototype is JSObject setProto)
        {
            SetToStringTag(setProto, "Set");

            // §24.2.3.10: the initial value of Set.prototype.keys is the same function
            // object as Set.prototype.values (test262: Set/prototype/keys/keys).
            if (setProto[KeyStrings.GetOrCreate("values")] is JSFunction setValues)
                setProto.FastAddValue(KeyStrings.GetOrCreate("keys"), setValues, JSPropertyAttributes.ConfigurableValue);
        }

        // WeakMap.prototype[@@toStringTag] = "WeakMap"
        if (context[KeyStrings.GetOrCreate("WeakMap")] is JSFunction weakMapCtor && weakMapCtor.prototype is JSObject weakMapProto)
            SetToStringTag(weakMapProto, "WeakMap");

        // WeakSet.prototype[@@toStringTag] = "WeakSet"
        if (context[KeyStrings.GetOrCreate("WeakSet")] is JSFunction weakSetCtor && weakSetCtor.prototype is JSObject weakSetProto)
            SetToStringTag(weakSetProto, "WeakSet");

        // Promise.prototype[@@toStringTag] = "Promise"
        if (context[KeyStrings.Promise] is JSFunction promiseCtor && promiseCtor.prototype is JSObject promiseProto)
            SetToStringTag(promiseProto, "Promise");

        // Symbol.prototype[@@toStringTag] = "Symbol"
        if (context[KeyStrings.Symbol] is JSFunction symbolCtor && symbolCtor.prototype is JSObject symbolProto)
            SetToStringTag(symbolProto, "Symbol");

        // ArrayBuffer.prototype[@@toStringTag] = "ArrayBuffer"
        if (context[KeyStrings.GetOrCreate("ArrayBuffer")] is JSFunction abCtor && abCtor.prototype is JSObject abProto)
            SetToStringTag(abProto, "ArrayBuffer");

        // SharedArrayBuffer.prototype[@@toStringTag] = "SharedArrayBuffer"
        if (context[KeyStrings.GetOrCreate("SharedArrayBuffer")] is JSFunction sabCtor && sabCtor.prototype is JSObject sabProto)
            SetToStringTag(sabProto, "SharedArrayBuffer");

        // DataView.prototype[@@toStringTag] = "DataView"
        if (context[KeyStrings.GetOrCreate("DataView")] is JSFunction dvCtor && dvCtor.prototype is JSObject dvProto)
            SetToStringTag(dvProto, "DataView");

        // Generator.prototype[@@toStringTag] = "Generator"
        // AsyncGenerator.prototype[@@toStringTag] = "AsyncGenerator"
        // AsyncGeneratorFunction.prototype[@@toStringTag] = "AsyncGeneratorFunction"
        // (GeneratorFunction.prototype / AsyncGeneratorFunction.prototype handled in JSGeneratorFunctionV2)
        if (context[KeyStrings.GetOrCreate("Generator")] is JSFunction generatorCtor && generatorCtor.prototype is JSObject generatorProto)
            SetToStringTag(generatorProto, "Generator");
    }

    internal static void PatchAsyncIteratorPrototype(JSContext context)
    {
        if (context[KeyStrings.GetOrCreate("Generator")] is not JSFunction generator)
            return;

        var generatorPrototype = generator.prototype as JSObject;
        var currentAsyncGeneratorPrototype = generatorPrototype?.GetPrototypeOf() as JSObject;
        var currentAsyncIteratorPrototype = currentAsyncGeneratorPrototype?.GetPrototypeOf() as JSObject;

        if (generatorPrototype == null
            || currentAsyncGeneratorPrototype == null
            || (currentAsyncIteratorPrototype != null
                && !currentAsyncIteratorPrototype.GetOwnPropertyDescriptor(JSSymbol.asyncIterator).IsUndefined))
        {
            return;
        }

        var asyncGeneratorPrototype = new JSObject();
        asyncGeneratorPrototype.FastAddValue((IJSSymbol)JSSymbol.toStringTag, JSValue.CreateString("AsyncGenerator"), JSPropertyAttributes.ConfigurableReadonlyValue);
        var asyncIteratorPrototype = new JSObject
        {
            BasePrototypeObject = currentAsyncGeneratorPrototype
        };
        asyncIteratorPrototype.FastAddValue((IJSSymbol)JSSymbol.asyncIterator, CreateNativeFunction(static (in Arguments a) => a.This, "[Symbol.asyncIterator]"), JSPropertyAttributes.ConfigurableValue);

        // %AsyncIteratorPrototype% [ @@asyncDispose ] (): returns a promise that closes the async
        // iterator via its return method (the fulfillment value of return is ignored).
        asyncIteratorPrototype.FastAddValue((IJSSymbol)JSSymbol.asyncDispose, CreateNativeFunction(static (in Arguments a) =>
        {
            var iterator = a.This;
            return new JSPromise((resolve, reject) =>
            {
                try
                {
                    var ret = iterator[KeyStrings.GetOrCreate("return")]; // GetMethod(O, "return")
                    if (ret.IsNullOrUndefined) { resolve(JSUndefined.Value); return; }
                    if (ret is not IJSFunction returnFn)
                        throw JSEngine.NewTypeError("AsyncIterator.prototype[Symbol.asyncDispose]: return is not a function");
                    var result = returnFn.InvokeFunction(new Arguments(iterator));
                    if (!result.IsObject)
                        throw JSEngine.NewTypeError("AsyncIterator.prototype[Symbol.asyncDispose]: return must return an object");
                    resolve(JSUndefined.Value);
                }
                catch (JSException ex) { reject(ex.Error); }
            });
        }, "[Symbol.asyncDispose]", 0), JSPropertyAttributes.ConfigurableValue);

        asyncGeneratorPrototype.BasePrototypeObject = asyncIteratorPrototype;
        generator.prototype.BasePrototypeObject = asyncGeneratorPrototype;
    }

    private static void PatchErrorConstructor(JSContext context, KeyString key, JSFunctionDelegate factory, JSFunction baseConstructor = null, int length = 1)
    {
        if (context[key] is not JSFunction existing)
            return;

        var name = key.Value;
        var isErrorKey = KeyStrings.GetOrCreate("isError");
        var replacement = new JSFunction(factory, name, $"function {name}() {{ [native code] }}", length: length, createPrototype: false)
        {
            prototype = existing.prototype
        };

        // Per spec, the [[Prototype]] of each NativeError constructor is the Error constructor.
        if (baseConstructor != null)
            replacement.BasePrototypeObject = baseConstructor;
        var functionMetadata = new JSFunction(JSFunction.empty, "Function", "function Function() { [native code] }", length: 1, createPrototype: false);

        replacement.FastAddValue(KeyStrings.prototype, existing.prototype, JSPropertyAttributes.ReadonlyValue);
        replacement.FastAddValue(KeyStrings.constructor, functionMetadata, JSPropertyAttributes.ConfigurableValue);
        existing.prototype.FastAddValue(KeyStrings.name, JSValue.CreateString(name.Value), JSPropertyAttributes.ConfigurableValue);
        existing.prototype.FastAddValue(KeyStrings.message, JSValue.CreateString(string.Empty), JSPropertyAttributes.ConfigurableValue);

        if (!existing[isErrorKey].IsUndefined)
            replacement.FastAddValue(isErrorKey, existing[isErrorKey], JSPropertyAttributes.ConfigurableValue);

        existing.prototype[KeyStrings.constructor] = replacement;
        context.FastAddValue(key, replacement, JSPropertyAttributes.ConfigurableValue);
    }

    private static void PatchBuiltInConstructorPrototypeDescriptors(JSContext context)
    {
        HashSet<JSObject> visited = [];
        PatchBuiltInConstructorPrototypeDescriptors(context, visited, depth: 2);
    }

    private static void PatchBuiltInConstructorPrototypeDescriptors(JSObject holder, HashSet<JSObject> visited, int depth)
    {
        if (!visited.Add(holder))
            return;

        var properties = holder.GetOwnProperties(false).GetEnumerator();
        while (properties.MoveNext(out var _, out var property))
        {
            if (!property.IsValue || property.value is not JSValue value || !value.IsObject)
                continue;

            if (value is JSFunction function)
            {
                PatchBuiltInConstructorPrototypeDescriptor(function);
                continue;
            }

            if (depth > 0 && value is JSObject nestedObject)
                PatchBuiltInConstructorPrototypeDescriptors(nestedObject, visited, depth - 1);
        }
    }

    private static void PatchBuiltInConstructorPrototypeDescriptor(JSFunction function)
    {
        if (function.ToDetailString().IndexOf("[native", StringComparison.Ordinal) < 0)
            return;

        ref var ownProperties = ref function.GetOwnProperties(false);
        if (ownProperties.IsEmpty)
            return;

        ref var prototypeProperty = ref ownProperties.GetValue(KeyStrings.prototype.Key);
        if (prototypeProperty.IsEmpty
            || !prototypeProperty.IsValue
            || prototypeProperty.value is not JSObject
            || (prototypeProperty.IsReadOnly && !prototypeProperty.IsConfigurable))
        {
            return;
        }

        prototypeProperty = new JSProperty(
            KeyStrings.prototype,
            prototypeProperty.get,
            prototypeProperty.set,
            prototypeProperty.value,
            JSPropertyAttributes.ReadonlyValue);
    }
}
