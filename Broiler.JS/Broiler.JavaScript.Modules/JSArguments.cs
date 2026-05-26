using System.Collections.Generic;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.LinqExpressions.LinqExpressions;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Modules;

public class JSArguments: JSObject
{
    private readonly Dictionary<uint, JSVariable> mappedParameters;

    public static JSValue Callee(in Arguments a) => throw JSEngine.NewTypeError($"Cannot access callee in strict mode");

    public new JSValue Values(in Arguments a) => JSGeneratorBuilder.CreateFromEnumerator(GetElementEnumerator(), "Arguments");

    public static JSValue[] Empty = [];

    public override bool BooleanValue => true;

    public override JSValue TypeOf() => JSConstants.Object;

    internal override PropertyKey ToKey(bool create = false) => KeyStrings.arguments;

    public JSArguments(in Arguments args)
    {
        // arguments = args;
        ref var properties = ref GetOwnProperties(true);
        var throwTypeError = JSFunction.CreateFrozenThrowTypeErrorFunction("ThrowTypeError", "Cannot access callee in strict mode");
        properties.Put(KeyStrings.length, JSValue.CreateNumber(args.Length), JSPropertyAttributes.ConfigurableValue);
        properties.Put(KeyStrings.callee, throwTypeError, throwTypeError, JSPropertyAttributes.Property);

        ref var symbols = ref GetSymbols();
        symbols.Put(JSValue.SymbolIterator.Key) = JSProperty.Property(new JSFunction(Values), JSPropertyAttributes.ConfigurableValue);
        ref var elements = ref CreateElements();
        
        for (int i = 0; i < args.Length; i++)
            elements.Put((uint)i, args.GetAt(i));
    }

    public JSArguments(in Arguments args, JSVariable[] mappedParametersByIndex)
        : this(args)
    {
        if (mappedParametersByIndex == null || mappedParametersByIndex.Length == 0)
            return;

        mappedParameters = [];
        for (uint i = 0; i < mappedParametersByIndex.Length; i++)
        {
            var mappedParameter = mappedParametersByIndex[i];
            if (mappedParameter != null)
                mappedParameters[i] = mappedParameter;
        }
    }

    public override JSValue GetOwnPropertyDescriptor(JSValue name)
    {
        var descriptor = base.GetOwnPropertyDescriptor(name);
        if (descriptor is not JSObject descriptorObject)
            return descriptor;

        var propertyKey = name.ToKey(false);
        ref var elements = ref GetElements(false);
        if (propertyKey.Type == KeyType.UInt
            && mappedParameters != null
            && mappedParameters.TryGetValue(propertyKey.Index, out var mappedParameter)
            && elements.TryGetValue(propertyKey.Index, out var property)
            && property.IsValue)
        {
            descriptorObject[KeyStrings.value] = mappedParameter.Value;
        }

        return descriptor;
    }

    public override JSValue GetOwnProperty(uint name)
    {
        ref var elements = ref GetElements(false);
        return mappedParameters != null
               && mappedParameters.TryGetValue(name, out var mappedParameter)
               && elements.TryGetValue(name, out var property)
               && property.IsValue
            ? mappedParameter.Value
            : base.GetOwnProperty(name);
    }

    public override JSValue GetValue(uint key, JSValue receiver, bool throwError = true)
    {
        ref var elements = ref GetElements(false);
        return mappedParameters != null
               && mappedParameters.TryGetValue(key, out var mappedParameter)
               && ReferenceEquals(receiver as JSObject ?? this, this)
               && elements.TryGetValue(key, out var property)
               && property.IsValue
            ? mappedParameter.Value
            : base.GetValue(key, receiver, throwError);
    }

    public override bool SetValue(uint name, JSValue value, JSValue receiver, bool throwError = true)
    {
        var result = base.SetValue(name, value, receiver, throwError);
        ref var elements = ref GetElements(false);
        if (result
            && mappedParameters != null
            && mappedParameters.TryGetValue(name, out var mappedParameter)
            && ReferenceEquals(receiver as JSObject ?? this, this)
            && elements.TryGetValue(name, out var property)
            && property.IsValue)
        {
            mappedParameter.Value = value;
        }

        return result;
    }

    public override JSValue DefineProperty(uint key, JSObject pd)
    {
        SynchronizeMappedElementValue(key);

        var result = base.DefineProperty(key, pd);
        if (mappedParameters == null || !mappedParameters.TryGetValue(key, out var mappedParameter))
            return result;

        var hasGet = !pd.GetOwnPropertyDescriptor(JSValue.CreateString("get")).IsUndefined;
        var hasSet = !pd.GetOwnPropertyDescriptor(JSValue.CreateString("set")).IsUndefined;
        var hasValue = !pd.GetOwnPropertyDescriptor(JSValue.CreateString("value")).IsUndefined;
        var hasWritable = !pd.GetOwnPropertyDescriptor(JSValue.CreateString("writable")).IsUndefined;

        if (hasGet || hasSet)
        {
            mappedParameters.Remove(key);
            return result;
        }

        if (hasValue)
        {
            mappedParameter.Value = pd[KeyStrings.value];
            SynchronizeMappedElementValue(key);
        }

        if (hasWritable && !pd[KeyStrings.writable].BooleanValue)
            mappedParameters.Remove(key);

        return result;
    }

    public override JSValue Delete(uint key)
    {
        var result = base.Delete(key);
        if (result.BooleanValue && mappedParameters != null)
            mappedParameters.Remove(key);

        return result;
    }

    private void SynchronizeMappedElementValue(uint key)
    {
        ref var elements = ref GetElements(true);
        if (mappedParameters == null
            || !mappedParameters.TryGetValue(key, out var mappedParameter)
            || !elements.TryGetValue(key, out var property)
            || !property.IsValue)
        {
            return;
        }

        elements.Put(key) = JSProperty.Property(key, mappedParameter.Value, property.Attributes);
    }

    public override string ToString() => "[object Arguments]";
}
