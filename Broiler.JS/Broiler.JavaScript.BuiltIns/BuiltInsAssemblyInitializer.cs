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
            JSPrimitive primitive => new JSPrimitiveObject(primitive),
            JSSymbol symbol => new JSSymbolObject(symbol),
            _ => throw JSEngine.NewTypeError($"Cannot convert {value} to object")
        };

        // Initialize JSArrayBuilder with the concrete JSArray type so the
        // Compiler can build array expression trees without a direct reference.
        JSArrayBuilder.Initialize(typeof(JSArray));

        // Wire factory delegate for Intl date formatting so JSDatePrototype
        // does not directly reference JSIntlDateTimeFormat.
        JSDate.IntlDateFormatter = static (culture, value, options) =>
            JSIntlDateTimeFormat.Get(culture).Format(value, options);

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
                foreach (var entry in map.GetEntries())
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
                foreach (var item in set.Keys())
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
            DefaultBuiltInRegistry.AddProto(proto, "next", JSIteratorObject.StaticNext, 0);
            DefaultBuiltInRegistry.AddProto(proto, "return", JSIteratorObject.StaticReturn, 1);
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

    private static JSTypedArray CloneTypedArray(JSTypedArray typedArray, JSArrayBuffer clonedBuffer)
    {
        var args = new Arguments(
            JSUndefined.Value,
            clonedBuffer,
            new JSNumber(typedArray.byteOffset),
            new JSNumber(typedArray.Length * typedArray.bytesPerElement));

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
        PatchErrorConstructor(context, KeyStrings.GetOrCreate("AggregateError"), static (in Arguments a) => new JSAggregateError(in a), errorCtor);
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
    }

    private static JSFunction CreateNativeFunction(JSFunctionDelegate fx, string name, int length = 0)
        => new(fx, name, $"function {name}() {{ [native code] }}", length: length, createPrototype: false);

    private static JSFunction CreateNativeGetter(JSFunctionDelegate fx, string name)
        => new(fx, $"get {name}", $"function get {name}() {{ [native code] }}", createPrototype: false, length: 0);

    private static JSFunction CreateNativeSetter(JSFunctionDelegate fx, string name)
        => new(fx, $"set {name}", $"function set {name}() {{ [native code] }}", createPrototype: false, length: 1);

    private static void EnsureAccessorProperty(JSObject target, JSValue key, string name, JSFunctionDelegate getter, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableProperty)
    {
        if (!target.GetOwnPropertyDescriptor(key).IsUndefined)
            return;

        target.FastAddProperty(key, CreateNativeGetter(getter, name), null, attributes);
    }

    private static void EnsureAccessorProperty(JSObject target, KeyString key, string name, JSFunctionDelegate getter, JSPropertyAttributes attributes = JSPropertyAttributes.ConfigurableProperty)
    {
        if (!target.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(key.ToString(), key)).IsUndefined)
            return;

        target.FastAddProperty(key, CreateNativeGetter(getter, name), null, attributes);
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
            prototype.FastAddValue((IJSSymbol)JSSymbol.iterator, CreateNativeFunction(static (in Arguments a) =>
            {
                var text = a.This.AsString();
                return new JSGenerator(new JSString(text).GetElementEnumerator(), "String Iterator");
            }, "[Symbol.iterator]"), JSPropertyAttributes.ConfigurableValue);
        }

        if (!trimStart.IsUndefined)
            prototype.FastAddValue(KeyStrings.GetOrCreate("trimLeft"), trimStart, JSPropertyAttributes.ConfigurableValue);
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

            var nameString = name.IsUndefined ? "Error" : name.ToString();
            var messageString = message.IsUndefined ? string.Empty : message.ToString();

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
            var key = ToPropertyKeyValue(propertyName);
            var current = CoerceObject(thisValue);
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
                if (!a.This.TryAsObjectThrowIfNullOrUndefined(out var @object))
                    return JSUndefined.Value;

                return @object.GetPrototypeOf();
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

                return JSValue.CreateString(a.This?.TypeOf() == JSConstants.Function ? "[object Function]" : "[object Object]");
            }, "toLocaleString"), JSPropertyAttributes.ConfigurableValue);
        }

        prototype.FastAddValue(KeyStrings.GetOrCreate("__defineGetter__"), CreateNativeFunction(static (in Arguments a) =>
        {
            var key = ToPropertyKeyValue(a.Get1());
            var target = CoerceObject(a.This);
            var getter = a.GetAt(1);
            if (getter is not IJSFunction)
                throw JSEngine.NewTypeError("Getter must be a function");

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
            var key = ToPropertyKeyValue(a.Get1());
            var target = CoerceObject(a.This);
            var setter = a.GetAt(1);
            if (setter is not IJSFunction)
                throw JSEngine.NewTypeError("Setter must be a function");

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

        var callerKey = KeyStrings.GetOrCreate("caller");
        if (functionCtor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(callerKey.ToString(), callerKey)).IsUndefined)
        {
            functionCtor.prototype.FastAddProperty(
                callerKey,
                CreateNativeGetter(static (in Arguments a) => throw JSEngine.NewTypeError("Cannot access caller in strict mode"), "caller"),
                CreateNativeSetter(static (in Arguments a) => throw JSEngine.NewTypeError("Cannot access caller in strict mode"), "caller"),
                JSPropertyAttributes.ConfigurableProperty);
        }

        if (functionCtor.prototype.GetOwnPropertyDescriptor(JSValue.CreateStringWithKey(KeyStrings.arguments.ToString(), KeyStrings.arguments)).IsUndefined)
        {
            functionCtor.prototype.FastAddProperty(
                KeyStrings.arguments,
                CreateNativeGetter(static (in Arguments a) => throw JSEngine.NewTypeError("Cannot access arguments in strict mode"), "arguments"),
                CreateNativeSetter(static (in Arguments a) => throw JSEngine.NewTypeError("Cannot access arguments in strict mode"), "arguments"),
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

        ref var ownProperties = ref proxyCtor.GetOwnProperties();
        ownProperties.Put(KeyStrings.prototype.Key) = JSProperty.Property(KeyStrings.prototype, JSUndefined.Value, JSPropertyAttributes.Value);
    }

    private static void PatchSymbolPrototype(JSContext context)
    {
        if (context[KeyStrings.Symbol] is not JSFunction symbolCtor)
            return;

        ref var symbols = ref symbolCtor.prototype.GetSymbols();
        symbols.Put(JSSymbol.toPrimitive.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            if (a.This is JSSymbol symbol)
                return symbol;

            throw JSEngine.NewTypeError("Symbol.prototype[Symbol.toPrimitive] requires a symbol receiver");
        }, "[Symbol.toPrimitive]", 1), JSPropertyAttributes.ConfigurableValue);

        EnsureAccessorProperty(symbolCtor.prototype, KeyStrings.GetOrCreate("description"), "description", static (in Arguments a) =>
        {
            if (a.This is JSSymbol symbol)
                return symbol.Description == null ? JSUndefined.Value : JSValue.CreateString(symbol.Description);

            if (a.This is JSObject symbolObject && symbolObject.ValueOf() is JSSymbol boxed)
                return boxed.Description == null ? JSUndefined.Value : JSValue.CreateString(boxed.Description);

            throw JSEngine.NewTypeError("Symbol.prototype.description requires a symbol receiver");
        });
    }

    private static void PatchDatePrototype(JSContext context)
    {
        if (context[KeyStrings.Date] is not JSFunction dateCtor)
            return;

        EnsureAccessorProperty(dateCtor.prototype, JSSymbol.toStringTag, "[Symbol.toStringTag]", static (in Arguments a)
            => JSValue.CreateString("Date"));
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
                if (flags.IsUndefined && JSRegExp.IsRegExpLike(pattern))
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

            replacement.FastAddValue(KeyStrings.prototype, originalCtor.prototype, JSPropertyAttributes.ConfigurableValue);
            replacement.FastAddValue(KeyStrings.constructor, functionMetadata, JSPropertyAttributes.ConfigurableValue);
            originalCtor.prototype[KeyStrings.constructor] = replacement;
            context.FastAddValue(KeyStrings.RegExp, replacement, JSPropertyAttributes.ConfigurableValue);
            regExpCtor = replacement;
        }

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

        static JSValue InvokeSpeciesConstructor(JSRegExp regExp, JSValue flags)
        {
            var constructor = regExp[KeyStrings.constructor];
            var species = GetSpeciesConstructor(constructor);
            if (species.IsNullOrUndefined)
                return JSUndefined.Value;

            return species.CreateInstance(new Arguments(species, regExp, flags));
        }

        static JSValue RegExpExec(JSValue rx, JSValue input)
        {
            var exec = rx[KeyStrings.GetOrCreate("exec")];
            if (exec.IsUndefined)
            {
                if (rx is not JSRegExp regExp)
                    throw JSEngine.NewTypeError("RegExp.prototype[Symbol.replace] called on incompatible receiver");

                return regExp.Exec(new Arguments(rx, input));
            }

            if (!exec.IsFunction)
                throw JSEngine.NewTypeError("RegExp exec property is not callable");

            var result = exec.InvokeFunction(new Arguments(rx, input));
            if (!result.IsObject && !result.IsNull)
                throw JSEngine.NewTypeError("RegExp exec result must be an object or null");

            return result;
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
                        var end = replacement.IndexOf('>', i + 1);
                        if (end < 0)
                        {
                            replacementBuilder.Append("$<");
                            break;
                        }

                        if (!namedCaptures.IsUndefined)
                        {
                            if (namedCaptures is not JSObject namedCapturesObject)
                                throw JSEngine.NewTypeError("RegExp replacement named captures must be an object");

                            var groupName = replacement.Substring(i + 1, end - i - 1);
                            var capture = namedCapturesObject[KeyStrings.GetOrCreate(groupName)];
                            if (!capture.IsUndefined)
                                replacementBuilder.Append(capture.ToString());
                        }

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
            var flags = GetObservableFlags(rx).ToString();
            if (!flags.Contains('g'))
            {
                if (rx is JSRegExp regExp)
                    return regExp.Match(input);

                return RegExpExec(rx, input);
            }

            rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), JSValue.NumberZero);
            var matches = JSValue.CreateArray();
            uint matchCount = 0;
            while (true)
            {
                var result = RegExpExec(rx, input);
                if (result.IsNull)
                    return matchCount == 0 ? JSValue.NullValue : matches;

                var matchString = result[0].ToString();
                matches[matchCount++] = JSValue.CreateString(matchString);
                if (matchString.Length != 0)
                    continue;

                var nextIndex = (int)rx[KeyStrings.lastIndex].DoubleValue;
                rxObj.SetPropertyOrThrow(KeyStrings.lastIndex.ToJSValue(), JSValue.CreateNumber(nextIndex + 1));
            }
        }, "[Symbol.match]", 1), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.matchAll.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            if (a.This is JSRegExp regExp)
            {
                var flags = GetObservableFlags(regExp);
                var matcher = InvokeSpeciesConstructor(regExp, flags);
                if (matcher.IsUndefined)
                    matcher = new JSRegExp(new Arguments(JSUndefined.Value, regExp, flags));
                matcher[KeyStrings.lastIndex] = regExp[KeyStrings.lastIndex];
                return new JSRegExpStringIterator(
                    matcher,
                    JSValue.CreateString(a.Get1().ToString()),
                    matcher[KeyStrings.GetOrCreate("global")].BooleanValue,
                    matcher[KeyStrings.GetOrCreate("unicode")].BooleanValue || matcher[KeyStrings.GetOrCreate("unicodeSets")].BooleanValue);
            }

            if (JSRegExp.IsRegExpLike(a.This))
            {
                var flags = a.This[KeyStrings.GetOrCreate("flags")];
                if (flags.IsNullOrUndefined)
                    throw JSEngine.NewTypeError("RegExp.prototype[Symbol.matchAll] requires a non-null flags value");

                if (!flags.ToString().Contains('g'))
                    throw JSEngine.NewTypeError("RegExp.prototype[Symbol.matchAll] requires a global regular expression");

                var matcher = new JSRegExp(new Arguments(JSUndefined.Value, a.This, flags));
                return new JSRegExpStringIterator(
                    matcher,
                    JSValue.CreateString(a.Get1().ToString()),
                    matcher.globalSearch,
                    matcher.unicode);
            }

            var defaultMatcher = new JSRegExp(new Arguments(JSUndefined.Value, a.This, JSValue.CreateString("g")));
            return new JSRegExpStringIterator(defaultMatcher, JSValue.CreateString(a.Get1().ToString()), defaultMatcher.globalSearch, defaultMatcher.unicode);
        }, "[Symbol.matchAll]", 1), JSPropertyAttributes.ConfigurableValue);
        symbols.Put(JSSymbol.replace.Key) = JSProperty.Property(CreateNativeFunction((in Arguments a) =>
        {
            var rx = a.This;
            if (rx is not JSObject)
                throw JSEngine.NewTypeError("RegExp.prototype[Symbol.replace] called on incompatible receiver");

            var input = a.Get1().ToString();
            var replaceValue = a.TryGetAt(1, out var second) ? second : JSUndefined.Value;
            var functionalReplace = replaceValue.IsFunction;
            var replacementText = functionalReplace ? null : replaceValue.ToString();
            var flags = GetObservableFlags(rx).ToString();
            var global = flags.Contains('g');

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

                var nextIndex = (int)rx[KeyStrings.lastIndex].DoubleValue;
                rx[KeyStrings.lastIndex] = JSValue.CreateNumber(nextIndex + 1);
            }

            if (results.Count == 0)
                return JSValue.CreateString(input);

            var accumulatedResult = new StringBuilder();
            var nextSourcePosition = 0;
            foreach (var result in results)
            {
                var matched = result[0].ToString();
                var position = (int)result[KeyStrings.index].DoubleValue;
                var capturesLength = Math.Max((int)result[KeyStrings.length].DoubleValue, 0);
                var namedCaptures = result[KeyStrings.GetOrCreate("groups")];
                JSValue normalizedNamedCaptures = JSUndefined.Value;
                if (!namedCaptures.IsUndefined)
                {
                    if (namedCaptures.IsNull)
                        throw JSEngine.NewTypeError("RegExp replacement named captures must be an object");

                    normalizedNamedCaptures = namedCaptures as JSObject
                        ?? (JSObject)JSObject.CreatePrimitiveObject(namedCaptures);
                }

                List<JSValue> captures = [];
                for (var i = 1; i < capturesLength; i++)
                {
                    var capture = result[(uint)i];
                    captures.Add(capture.IsUndefined ? JSUndefined.Value : JSValue.CreateString(capture.ToString()));
                }

                string replacement;
                if (functionalReplace)
                {
                    List<JSValue> replacerArgs = [JSValue.CreateString(matched)];
                    replacerArgs.AddRange(captures);
                    replacerArgs.Add(JSValue.CreateNumber(position));
                    replacerArgs.Add(JSValue.CreateString(input));
                    if (!normalizedNamedCaptures.IsUndefined)
                        replacerArgs.Add(normalizedNamedCaptures);

                    replacement = replaceValue.InvokeFunction(new Arguments(JSUndefined.Value, replacerArgs.ToArray())).ToString();
                }
                else
                {
                    replacement = GetSubstitution(matched, input, position, captures, normalizedNamedCaptures, replacementText);
                }

                if (position > nextSourcePosition)
                    accumulatedResult.Append(input.AsSpan(nextSourcePosition, position - nextSourcePosition));

                accumulatedResult.Append(replacement);
                nextSourcePosition = Math.Min(position + matched.Length, input.Length);
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

        EnsureAccessorProperty(regExpCtor.prototype, KeyStrings.GetOrCreate("dotAll"), "dotAll", static (in Arguments a) =>
        {
            if (a.This is not IJSRegExp regExp)
                throw JSEngine.NewTypeError("RegExp.prototype.dotAll called on incompatible receiver");

            return regExp.Flags.Contains('s') ? JSValue.BooleanTrue : JSValue.BooleanFalse;
        });

        PatchLegacyRegExpAccessor(regExpCtor, "lastMatch", "$&");
        PatchLegacyRegExpAccessor(regExpCtor, "lastParen", "$+");
        PatchLegacyRegExpAccessor(regExpCtor, "leftContext", "$`");
        PatchLegacyRegExpAccessor(regExpCtor, "rightContext", "$'");
        PatchLegacyRegExpAccessor(regExpCtor, "input", "$_");

        for (var i = 1; i <= 9; i++)
            PatchLegacyRegExpAccessor(regExpCtor, $"${i}");
    }

    private static void PatchLegacyRegExpAccessor(JSObject regExpCtor, string propertyName, string alias)
    {
        PatchLegacyRegExpAccessor(regExpCtor, propertyName);
        PatchLegacyRegExpAccessor(regExpCtor, alias);
    }

    private static void PatchLegacyRegExpAccessor(JSObject regExpCtor, string propertyName)
    {
        EnsureAccessorProperty(regExpCtor, KeyStrings.GetOrCreate(propertyName), propertyName, static (in Arguments _) => JSValue.EmptyString);
    }

    private static void PatchArrayPrototype(JSContext context)
    {
        if (context[KeyStrings.Array] is not JSFunction arrayCtor)
            return;

        ref var symbols = ref arrayCtor.prototype.GetSymbols();
        if (!symbols.TryGetValue(JSSymbol.unscopables.Key, out var property) || property.IsEmpty || property.value is not JSObject unscopables)
        {
            unscopables = new JSObject();
            symbols.Put(JSSymbol.unscopables.Key) = JSProperty.Property(unscopables, JSPropertyAttributes.ConfigurableValue);
        }

        unscopables.FastAddValue(KeyStrings.GetOrCreate("toReversed"), JSValue.BooleanTrue, JSPropertyAttributes.ConfigurableValue);
        unscopables.FastAddValue(KeyStrings.GetOrCreate("toSorted"), JSValue.BooleanTrue, JSPropertyAttributes.ConfigurableValue);
        unscopables.FastAddValue(KeyStrings.GetOrCreate("toSpliced"), JSValue.BooleanTrue, JSPropertyAttributes.ConfigurableValue);
        unscopables.FastAddValue(KeyStrings.GetOrCreate("with"), JSValue.BooleanTrue, JSPropertyAttributes.ConfigurableValue);
    }

    private static void PatchTypedArrayBuiltIns(JSContext context)
    {
        if (context[KeyStrings.GetOrCreate("TypedArray")] is not JSFunction typedArrayCtor)
            return;

        static JSTypedArray RequireTypedArray(JSValue value)
            => value as JSTypedArray ?? throw JSEngine.NewTypeError("Failed to convert this to JSTypedArray");

        var prototype = typedArrayCtor.prototype;

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
            SetToStringTag(setProto, "Set");

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
        asyncGeneratorPrototype.BasePrototypeObject = asyncIteratorPrototype;
        generator.prototype.BasePrototypeObject = asyncGeneratorPrototype;
    }

    private static void PatchErrorConstructor(JSContext context, KeyString key, JSFunctionDelegate factory, JSFunction baseConstructor = null)
    {
        if (context[key] is not JSFunction existing)
            return;

        var name = key.Value;
        var isErrorKey = KeyStrings.GetOrCreate("isError");
        var replacement = new JSFunction(factory, name, $"function {name}() {{ [native code] }}", length: 1, createPrototype: false)
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
