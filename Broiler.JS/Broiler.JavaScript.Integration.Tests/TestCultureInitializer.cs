using System.Globalization;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Integration.Tests;

// Pin the process culture so the suite is deterministic regardless of the host's LANG / LC_ALL.
// Several behaviours are legitimately host-locale-defined and would otherwise vary by environment:
//   * Intl / toLocaleString with no explicit locale resolves the *default* locale from
//     CultureInfo.CurrentCulture (JSIntl: empty invariant name → "en-US").
//   * Date.prototype.toString renders the time-zone long name from TimeZoneInfo.StandardName, which the
//     OS localizes by the current (UI) culture ("Coordinated Universal Time" vs "Koordinierte Weltzeit").
//   * Test sources that interpolate a double into JS (e.g. `format({n})`) format it with the current
//     culture, so a comma decimal separator would corrupt the script.
// Forcing the invariant culture reproduces the en-US default these tests assume; tests that exercise a
// specific locale always pass it explicitly and are unaffected.
internal static class TestCultureInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}
