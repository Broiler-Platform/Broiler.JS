namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Marker for JavaScript Arguments objects. ES2026 §20.1.3.6 step 14 tags an
/// object with a [[ParameterMap]] internal slot as "Arguments" from Object.prototype.toString;
/// the builtin tag is exposed via this interface so the Runtime layer can recognise an
/// Arguments instance without referencing the concrete <c>JSArguments</c> class
/// (which lives in <c>Broiler.JavaScript.Modules</c>, downstream of Runtime).
/// </summary>
public interface IJSArguments
{
}
