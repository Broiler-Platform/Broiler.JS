using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// An async body resumed by the rejection of a value it awaited continues correctly: the next
/// <c>await</c> suspends on its own operand and receives that operand's result.
/// </summary>
/// <remarks>
/// The driver resumed the body with <c>Throw</c>, which already runs the body to its next
/// <c>await</c> and returns that step as an iterator result, and then resumed it a second time
/// with that iterator result as the value. So after a caught awaited rejection the next
/// <c>await</c> produced <c>{ value, done }</c> without waiting, and a second awaited rejection was
/// never thrown into the body — its <c>catch</c> did not run. Measured on the unfixed engine: the
/// first case read <c>1,,5</c>, and the second did not report its later rejection.
/// </remarks>
public class AsyncResumeAfterRejectionTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static async Task<string> Run(string body)
    {
        Load();
        using var ctx = new JSContext();
        var result = await ctx.ExecuteAsync("(async () => { " + body + " })()");
        return result.ToString();
    }

    [Fact(Timeout = 600000)]
    public async Task AwaitsAfterACaughtRejectionWaitForTheirOwnOperands()
        => Assert.Equal("1,2,5", await Run(
            "let a, b; try { await Promise.reject(1); } catch (e) { a = e; }" +
            " try { await Promise.reject(2); } catch (e) { b = e; }" +
            " const v = await Promise.resolve(5); return [a, b, v].join();"));

    /// <summary>
    /// Awaiting a thenable whose <c>then</c> throws rejects the await, so the body's own
    /// <c>catch</c> sees the thrown value. It used to reject the async function's promise instead,
    /// bypassing the <c>catch</c> (measured on the unfixed engine).
    /// </summary>
    [Fact(Timeout = 600000)]
    public async Task AThenableWhoseThenThrowsRejectsTheAwaitInsideTheBody()
        => Assert.Equal("caught:true", await Run(
            "const error = {}; try { await { then() { throw error; } }; return 'no throw'; }" +
            " catch (e) { return 'caught:' + (e === error); }"));

    [Fact(Timeout = 600000)]
    public async Task ARejectionAfterACaughtRejectionRejectsTheFunction()
        => Assert.Equal("caught:1|rejected:2", await Run(
            "let first; try { await Promise.reject(1); } catch (e) { first = e; }" +
            " const inner = (async () => { try { await Promise.reject(0); } catch {} await Promise.reject(2); })();" +
            " return inner.then(() => 'resolved', e => 'caught:' + first + '|rejected:' + e);"));
}
