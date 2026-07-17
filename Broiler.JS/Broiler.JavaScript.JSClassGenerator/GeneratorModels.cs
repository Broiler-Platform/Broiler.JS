using System;
using System.Collections.Immutable;

namespace Broiler.JavaScript.JSClassGenerator;

internal sealed class GeneratedTypeModel : IEquatable<GeneratedTypeModel>
{
    internal GeneratedTypeModel(
        string hintName,
        string code,
        RegistrationTypeModel registration)
    {
        HintName = hintName;
        Code = code;
        Registration = registration;
    }

    internal string HintName { get; }
    internal string Code { get; }
    internal RegistrationTypeModel Registration { get; }

    public bool Equals(GeneratedTypeModel? other)
    {
        if (other == null
            || HintName != other.HintName
            || Code != other.Code
            || !Registration.Equals(other.Registration))
            return false;
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as GeneratedTypeModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = HintName.GetHashCode();
            hash = (hash * 397) ^ Code.GetHashCode();
            return (hash * 397) ^ Registration.GetHashCode();
        }
    }
}

internal sealed class RegistrationTypeModel : IEquatable<RegistrationTypeModel>
{
    internal RegistrationTypeModel(
        string @namespace,
        string clrClassName,
        string baseClrClassName,
        string featureName,
        bool register,
        ImmutableArray<string> names)
    {
        Namespace = @namespace;
        ClrClassName = clrClassName;
        BaseClrClassName = baseClrClassName;
        FeatureName = featureName;
        Register = register;
        Names = names;
    }

    internal string Namespace { get; }
    internal string ClrClassName { get; }
    internal string BaseClrClassName { get; }
    internal string FeatureName { get; }
    internal bool Register { get; }
    internal ImmutableArray<string> Names { get; }
    internal string QualifiedClrName => string.IsNullOrEmpty(Namespace)
        ? ClrClassName
        : Namespace + "." + ClrClassName;

    public bool Equals(RegistrationTypeModel? other)
    {
        if (other == null
            || Namespace != other.Namespace
            || ClrClassName != other.ClrClassName
            || BaseClrClassName != other.BaseClrClassName
            || FeatureName != other.FeatureName
            || Register != other.Register
            || Names.Length != other.Names.Length)
            return false;
        for (var i = 0; i < Names.Length; i++)
            if (Names[i] != other.Names[i])
                return false;
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as RegistrationTypeModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = QualifiedClrName.GetHashCode();
            hash = (hash * 397) ^ BaseClrClassName.GetHashCode();
            hash = (hash * 397) ^ FeatureName.GetHashCode();
            hash = (hash * 397) ^ Register.GetHashCode();
            foreach (var name in Names)
                hash = (hash * 397) ^ name.GetHashCode();
            return hash;
        }
    }
}

internal sealed class RegistrationTargetModel : IEquatable<RegistrationTargetModel>
{
    internal RegistrationTargetModel(
        string hintName,
        string @namespace,
        string className,
        ImmutableArray<string> existingFields)
    {
        HintName = hintName;
        Namespace = @namespace;
        ClassName = className;
        ExistingFields = existingFields;
    }

    internal string HintName { get; }
    internal string Namespace { get; }
    internal string ClassName { get; }
    internal ImmutableArray<string> ExistingFields { get; }

    public bool Equals(RegistrationTargetModel? other)
    {
        if (other == null
            || HintName != other.HintName
            || Namespace != other.Namespace
            || ClassName != other.ClassName
            || ExistingFields.Length != other.ExistingFields.Length)
            return false;
        for (var i = 0; i < ExistingFields.Length; i++)
            if (ExistingFields[i] != other.ExistingFields[i])
                return false;
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as RegistrationTargetModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = HintName.GetHashCode();
            hash = (hash * 397) ^ Namespace.GetHashCode();
            hash = (hash * 397) ^ ClassName.GetHashCode();
            foreach (var field in ExistingFields)
                hash = (hash * 397) ^ field.GetHashCode();
            return hash;
        }
    }
}
