using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Integration.Tests;

// Pin the process culture and local time zone so the suite is deterministic regardless of the host's
// LANG / LC_ALL / TZ. Several behaviours are legitimately host-defined and would otherwise vary by
// environment:
//   * Intl / toLocaleString with no explicit locale resolves the *default* locale from
//     CultureInfo.CurrentCulture (JSIntl: empty invariant name → "en-US").
//   * Date.prototype.toString renders the time-zone long name from TimeZoneInfo.StandardName, which the
//     OS localizes by the current (UI) culture ("Coordinated Universal Time" vs "Koordinierte Weltzeit").
//   * Date.prototype.toString renders its date/time and offset in the *local* zone (TimeZoneInfo.Local);
//     tests that assert an absolute rendering (e.g. the epoch "...GMT+0000...") assume a UTC container.
//   * Test sources that interpolate a double into JS (e.g. `format({n})`) format it with the current
//     culture, so a comma decimal separator would corrupt the script.
// Forcing the invariant culture reproduces the en-US default these tests assume, and forcing UTC as the
// local zone reproduces the UTC container they assume; tests that exercise a specific locale or named
// time zone always pass it explicitly and are unaffected.
internal static class TestCultureInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // .NET derives TimeZoneInfo.Local from the TZ environment variable on Unix; set it to UTC and
        // drop the cache so the local zone is UTC even when the host runs in another zone.
        Environment.SetEnvironmentVariable("TZ", "UTC");
        TimeZoneInfo.ClearCachedData();
    }
}
