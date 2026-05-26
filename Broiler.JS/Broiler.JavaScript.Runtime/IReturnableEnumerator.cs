namespace Broiler.JavaScript.Runtime;

internal interface IReturnableEnumerator
{
    JSValue Return();
    JSValue Return(JSValue value);
}
