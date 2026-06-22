using Broiler.JavaScript.ExpressionCompiler.Expressions;
using Broiler.JavaScript.Storage;

namespace Broiler.JavaScript.Runtime;

public static class JSPropertyAttributesBuilder
{
    public static BExpression Configurable = BExpression.Constant(JSPropertyAttributes.Configurable);
    public static BExpression ConfigurableProperty = BExpression.Constant(JSPropertyAttributes.ConfigurableProperty);
    public static BExpression ConfigurableReadonlyProperty = BExpression.Constant(JSPropertyAttributes.ConfigurableReadonlyProperty);
    public static BExpression ConfigurableReadonlyValue = BExpression.Constant(JSPropertyAttributes.ConfigurableReadonlyValue);
    public static BExpression ConfigurableValue = BExpression.Constant(JSPropertyAttributes.ConfigurableValue);
    public static BExpression Enumerable = BExpression.Constant(JSPropertyAttributes.Enumerable);
    public static BExpression EnumerableConfigurableProperty = BExpression.Constant(JSPropertyAttributes.EnumerableConfigurableProperty);
    public static BExpression EnumerableConfigurableReadonlyProperty = BExpression.Constant(JSPropertyAttributes.EnumerableConfigurableReadonlyProperty);
    public static BExpression EnumerableConfigurableReadonlyValue = BExpression.Constant(JSPropertyAttributes.EnumerableConfigurableReadonlyValue);
    public static BExpression EnumerableConfigurableValue = BExpression.Constant(JSPropertyAttributes.EnumerableConfigurableValue);
    public static BExpression EnumerableReadonlyProperty = BExpression.Constant(JSPropertyAttributes.EnumerableReadonlyProperty);
    public static BExpression EnumerableReadonlyValue = BExpression.Constant(JSPropertyAttributes.EnumerableReadonlyValue);
    public static BExpression Property= BExpression.Constant(JSPropertyAttributes.Property);
    public static BExpression Readonly = BExpression.Constant(JSPropertyAttributes.Readonly);
    public static BExpression ReadonlyProperty = BExpression.Constant(JSPropertyAttributes.ReadonlyProperty);
    public static BExpression ReadonlyValue = BExpression.Constant(JSPropertyAttributes.ReadonlyValue);
    public static BExpression Value = BExpression.Constant(JSPropertyAttributes.Value);
}
