using Broiler.JavaScript.Ast.Expressions;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Ast;


public class AstClassProperty(FastToken begin, FastToken last, AstPropertyKind propertyKind, bool isPrivate, bool isStatic, AstExpression propertyName, bool computed,
    AstExpression init, bool usesColon = false, bool usesAssign = false, bool isAutoAccessor = false) : AstNode(begin, FastNodeType.ClassProperty, last)
{
    public readonly bool IsStatic = isStatic;
    public readonly bool IsPrivate = isPrivate;
    public readonly AstPropertyKind Kind = propertyKind;
    public readonly AstExpression Key = propertyName;
    public readonly AstExpression Init = init;
    public readonly bool Computed = computed;
    public readonly bool UsesColon = usesColon;
    public readonly bool UsesAssign = usesAssign;

    // A class `accessor x = v` auto-accessor (decorators proposal). The field is parsed
    // as a Data element so the field/computed-key machinery applies, but a public
    // auto-accessor is compiled into a private backing field plus a getter/setter pair
    // installed on the home object (see FastCompiler.CreateClass).
    public readonly bool IsAutoAccessor = isAutoAccessor;

    public AstClassProperty Reduce(AstExpression key, AstExpression init)
        => new(Start, End, Kind, IsPrivate, IsStatic, key, Computed, init, UsesColon, UsesAssign, IsAutoAccessor);

    public override string ToString()
    {
        if (Kind == AstPropertyKind.Constructor)
            return $"constructor: {Init}";

        if (Kind == AstPropertyKind.Init)
            return $"static {Init}";

        if (IsStatic)
        {
            if (Kind == AstPropertyKind.Get)
                return $"static get {Key} {Init}";

            if (Kind == AstPropertyKind.Set)
                return $"static set {Key} {Init}";

            if (Computed)
            {
                if (Kind == AstPropertyKind.Data)
                    return $"static [{Key}]: {Init}";
            }

            if (Kind == AstPropertyKind.Data)
                return $"static {Key}: {Init}";
        }

        if (Kind == AstPropertyKind.Get)
            return $"get {Key} {Init}";

        if (Kind == AstPropertyKind.Set)
            return $"set {Key} {Init}";

        if (Kind == AstPropertyKind.Data)
            return $"{Key}: {Init}";

        if (Computed)
        {
            if (Kind == AstPropertyKind.Data)
                return $"[{Key}]: {Init}";
        }

        return "AstClassProperty";
    }
}
