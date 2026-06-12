using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using Broiler.JavaScript.Engine.Core;

namespace Broiler.JavaScript.BuiltIns.Temporal;

// Stubs for the remaining Temporal types. Each constructor exists (so `typeof
// Temporal.PlainDate === "function"` and feature detection work) but throws until the
// type is implemented. They are attached to the Temporal namespace by
// BuiltInsAssemblyInitializer.PatchTemporal (Register = false → not globals).
//
// Temporal.Duration, Temporal.Instant, Temporal.PlainDate and Temporal.PlainTime are
// implemented; the calendar- and time-zone-dependent types below are the remaining work.

// Temporal.PlainDate is implemented in JSTemporalPlainDate.cs.
// Temporal.PlainTime is implemented in JSTemporalPlainTime.cs.
// Temporal.PlainDateTime is implemented in JSTemporalPlainDateTime.cs.
// Temporal.PlainYearMonth is implemented in JSTemporalPlainYearMonth.cs.
// Temporal.PlainMonthDay is implemented in JSTemporalPlainMonthDay.cs.

// Temporal.ZonedDateTime is implemented in JSTemporalZonedDateTime.cs.
