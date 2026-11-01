using System;
using System.Collections.Generic;
using System.Globalization;
using Broiler.ECMAScript.Time;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

// THE STAND-INS FOR BROILER.VM'S ENGINE that the pinned copies of its realm code name (Differential/Vm/JsRealm.*.cs): the
// smallest object model in which JsRealm's Intl.Locale part, its locale-list operations and Intl.supportedValuesOf run
// unchanged. None of it is a copy and none of it decides an answer of the area: it carries the harness's Strings,
// Booleans and option bags to the copied code and its answers back, as Broiler.VM's engine carries a script's. A value
// the population never gives (a Symbol, a getter on an options bag, a Number tag) is refused, so that the stand-ins
// never answer for the engine. This file lives outside Vm/, which VmCopiesTests holds to the copies alone.

/// <summary>A script value's type, as the copied code tests it.</summary>
internal enum JsType
{
    Undefined,
    Null,
    Boolean,
    Number,
    String,
    Object,
}

/// <summary>A script value: <c>undefined</c>, <c>null</c>, a Boolean, a Number, a String or an object.</summary>
internal readonly struct JsValue
{
    private readonly object? payload;

    private JsValue(JsType type, object? payload)
    {
        Type = type;
        this.payload = payload;
    }

    internal JsType Type { get; }

    internal bool IsString => Type == JsType.String;

    internal bool IsObject => Type == JsType.Object;

    internal static JsValue Undefined => default;

    internal static JsValue Null => new(JsType.Null, null);

    internal static JsValue String(string value) => new(JsType.String, value);

    internal static JsValue Object(JsObject value) => new(JsType.Object, value);

    internal static JsValue Boolean(bool value) => new(JsType.Boolean, value);

    internal static JsValue Number(double value) => new(JsType.Number, value);

    internal JsObject? AsObjectOrNull() => payload as JsObject;

    internal string AsString() => (string)payload!;

    internal double AsNumber() => (double)payload!;

    internal bool AsBoolean() => (bool)payload!;

    /// <summary>ToBoolean (ECMA-262 7.1.2).</summary>
    internal bool ToBooleanValue() => Type switch
    {
        JsType.Undefined or JsType.Null => false,
        JsType.Boolean => AsBoolean(),
        JsType.Number => AsNumber() is not (0 or double.NaN),
        JsType.String => AsString().Length != 0,
        _ => true,
    };
}

/// <summary>A property's attributes.</summary>
[Flags]
internal enum JsPropertyAttributes
{
    None = 0,
    Writable = 1,
    Enumerable = 2,
    Configurable = 4,
}

/// <summary>A property: a data value, or a getter and a setter.</summary>
internal sealed class JsProperty
{
    private JsProperty(JsValue value, JsNativeFunction? getter, JsNativeFunction? setter, JsPropertyAttributes attributes)
    {
        Value = value;
        Getter = getter;
        Setter = setter;
        Attributes = attributes;
    }

    internal JsValue Value { get; }

    internal JsNativeFunction? Getter { get; }

    internal JsNativeFunction? Setter { get; }

    internal JsPropertyAttributes Attributes { get; }

    internal static JsProperty Data(JsValue value, JsPropertyAttributes attributes) => new(value, null, null, attributes);

    internal static JsProperty Accessor(JsNativeFunction? getter, JsNativeFunction? setter, JsPropertyAttributes attributes) => new(default, getter, setter, attributes);
}

/// <summary>A symbol, as a property key.</summary>
internal sealed class JsSymbol(string description, bool described = false)
{
    internal string Description { get; } = description;

    internal bool Described { get; } = described;
}

/// <summary>An ordinary object: its prototype, and its own properties by String and by symbol.</summary>
internal class JsObject(JsObject? prototype)
{
    internal JsObject? Prototype { get; } = prototype;

    internal Dictionary<string, JsProperty> Properties { get; } = new(StringComparer.Ordinal);

    internal Dictionary<JsSymbol, JsProperty> Symbols { get; } = [];

    internal void SetOwnProperty(string key, JsProperty property) => Properties[key] = property;

    internal void SetOwnSymbol(JsSymbol key, JsProperty property) => Symbols[key] = property;

    /// <summary>CreateDataProperty: writable, enumerable and configurable.</summary>
    internal void DefineOrdinary(string key, JsValue value) =>
        Properties[key] = JsProperty.Data(value, JsPropertyAttributes.Writable | JsPropertyAttributes.Enumerable | JsPropertyAttributes.Configurable);

    /// <summary>The property <paramref name="key"/> names on this object or its prototypes, or nothing.</summary>
    internal JsProperty? Find(string key)
    {
        for (var o = this; o is not null; o = o.Prototype)
        {
            if (o.Properties.TryGetValue(key, out var property))
            {
                return property;
            }
        }

        return null;
    }
}

/// <summary>A function's call, with its <c>this</c> and arguments.</summary>
internal delegate JsValue JsNativeCall(JsEngine engine, JsValue thisValue, JsValue[] arguments);

/// <summary>A function's construction, with its new target and arguments.</summary>
internal delegate JsValue JsNativeConstruct(JsEngine engine, JsValue newTarget, JsValue[] arguments);

/// <summary>A built-in function.</summary>
internal sealed class JsNativeFunction(JsRealm realm, JsObject prototype, string name, int length, JsNativeCall call, JsNativeConstruct? construct = null)
    : JsObject(prototype)
{
    internal JsRealm Realm { get; } = realm;

    internal string Name { get; } = name;

    internal int Length { get; } = length;

    internal bool BuildsFromNewTarget { get; init; }

    internal JsValue Call(JsEngine engine, JsValue thisValue, JsValue[] arguments) => call(engine, thisValue, arguments);

    internal JsValue Construct(JsEngine engine, JsValue newTarget, JsValue[] arguments) =>
        (construct ?? throw new InvalidOperationException(Name + " is not a constructor"))(engine, newTarget, arguments);
}

/// <summary>A script error the copied code throws: its constructor's name and its message.</summary>
internal sealed class VmError(string kind, string message) : Exception(message)
{
    internal string Kind { get; } = kind;
}

/// <summary>
/// Broiler.VM's decoded Intl tables, as <c>JsEngine.Intl</c> holds them, with the shared time zone database its
/// <c>JsIntlTables.TimeZones</c> answers since SJL-8.5, which the copy of <c>JsIntlTables</c> leaves out (milestone SJL-8's).
/// The copied code passes it where it takes a <see cref="JsIntlTables"/>, and reads its tables through it.
/// </summary>
internal sealed class VmIntl(JsIntlTables tables, ITimeZoneDatabase timeZones)
{
    internal JsIntlTables Tables { get; } = tables;

    internal ITimeZoneDatabase TimeZones { get; } = timeZones;

    internal IReadOnlyList<string> Locales => Tables.Locales;

    internal JsNumberData Numbers => Tables.Numbers;

    internal JsDateData Dates => Tables.Dates;

    internal JsLocaleInfo LocaleInfo => Tables.LocaleInfo;

    internal bool TryType(char singleton, string key, string type, out string preferred) => Tables.TryType(singleton, key, type, out preferred);

    public static implicit operator JsIntlTables(VmIntl intl) => intl.Tables;
}

/// <summary>Broiler.VM's engine, as the copied realm code calls it.</summary>
internal sealed class JsEngine
{
    internal JsEngine(VmIntl intl)
    {
        Intl = intl;
        Realm = new JsRealm();
        Realm.SetupLocaleForTheHarness();
    }

    internal JsRealm Realm { get; }

    internal VmIntl? Intl { get; }

    /// <summary>The units the copied code charged, as Broiler.VM's meter counts them.</summary>
    internal long Charged { get; private set; }

    internal void Charge(long units) => Charged += units;

    internal VmError Error(string kind, string message) => new(kind, message);

    /// <summary>ToString of the values the harness gives: Strings, Booleans and Numbers.</summary>
    internal string ToStringValue(JsValue value) => value.Type switch
    {
        JsType.String => value.AsString(),
        JsType.Boolean => value.AsBoolean() ? "true" : "false",
        JsType.Number => value.AsNumber().ToString("R", CultureInfo.InvariantCulture),
        JsType.Undefined => "undefined",
        JsType.Null => "null",
        _ => value.AsObjectOrNull() is JsLocaleObject locale ? locale.Locale : throw new NotSupportedException("the harness gives no object to ToString"),
    };

    /// <summary>[[Get]] of a String key, through the prototype chain and a getter.</summary>
    internal JsValue GetProperty(JsValue target, string key)
    {
        var o = target.AsObjectOrNull() ?? throw new NotSupportedException("the harness reads properties of objects alone");
        var property = o.Find(key);
        return property switch
        {
            null => JsValue.Undefined,
            { Getter: { } getter } => getter.Call(this, target, []),
            _ => property.Value,
        };
    }

    /// <summary>ToObject of the values the harness gives: an object is itself; <c>undefined</c> and <c>null</c> are refused.</summary>
    internal JsObject ToObject(JsValue value) => value.Type switch
    {
        JsType.Object => value.AsObjectOrNull()!,
        JsType.Undefined or JsType.Null => throw Error("TypeError", "Cannot convert undefined or null to object"),
        _ => new JsObject(Realm.ObjectPrototype),
    };

    /// <summary>GetPrototypeFromConstructor for the constructors the harness calls, whose prototype is the realm's.</summary>
    internal JsObject PrototypeFromConstructor(JsValue newTarget, JsObject fallback) => fallback;

    /// <summary>HasProperty.</summary>
    internal static bool HasProperty(JsObject o, string key) => o.Find(key) is not null;
}

/// <summary>The parts of Broiler.VM's realm the copied <c>JsRealm</c> files call and do not declare, and the harness's entry points to them.</summary>
internal sealed partial class JsRealm
{
    internal JsRealm()
    {
        ObjectPrototype = new JsObject(null);
        FunctionPrototype = new JsObject(ObjectPrototype);
    }

    internal JsObject ObjectPrototype { get; }

    internal JsObject FunctionPrototype { get; }

    internal static JsSymbol ToStringTagSymbol { get; } = new("Symbol.toStringTag", described: true);

    /// <summary>Builds <c>Intl.Locale</c> on an <c>Intl</c> object of its own, as the realm's <c>SetupIntl</c> does.</summary>
    internal void SetupLocaleForTheHarness() => SetupLocale(new JsObject(ObjectPrototype));

    private JsNativeFunction Native(string name, int length, JsNativeCall call) => new(this, FunctionPrototype, name, length, call);

    private void Method(JsObject target, string name, int length, JsNativeCall call) =>
        target.SetOwnProperty(name, JsProperty.Data(JsValue.Object(Native(name, length, call)), JsPropertyAttributes.Writable | JsPropertyAttributes.Configurable));

    /// <summary>CreateArrayFromList.</summary>
    internal JsObject NewArray(List<JsValue> values)
    {
        var array = new JsObject(ObjectPrototype);

        for (var i = 0; i < values.Count; i++)
        {
            array.DefineOrdinary(i.ToString(CultureInfo.InvariantCulture), values[i]);
        }

        array.SetOwnProperty("length", JsProperty.Data(JsValue.Number(values.Count), JsPropertyAttributes.Writable));
        return array;
    }

    /// <summary>LengthOfArrayLike of the arrays the harness gives.</summary>
    private static double ArrayLengthOf(JsEngine engine, JsValue array) => engine.GetProperty(array, "length").AsNumber();

    /// <summary>ToString of an array index.</summary>
    private static string ArrayKeyOf(double index) => index.ToString("R", CultureInfo.InvariantCulture);

    // ---- the harness's entry points to the copied members ----------------------------------------------------------

    internal static List<string> VmCanonicalizeLocaleList(JsEngine engine, JsValue locales) => CanonicalizeLocaleList(engine, locales);

    internal static List<string> VmAvailableLocales(JsEngine engine) => AvailableLocales(engine);

    internal static string?[] VmCollatorKeyData(string locale, string key) => CollatorKeyData(locale, key, "sort");

    internal static IReadOnlyList<string?> VmCalendarKeyValues => CalendarKeyValues;

    internal static string VmNullOption => NullOption;

    internal static JsValue VmSupportedValuesOf(JsEngine engine, JsValue key) => SupportedValuesOf(engine, key);
}
