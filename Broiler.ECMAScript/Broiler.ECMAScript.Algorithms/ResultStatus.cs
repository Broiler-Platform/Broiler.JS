// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           3
// Human-reviewed:   0/1
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript;

/// <summary>
/// How a shared operation ended. An expected failure is a status, never an exception (contract
/// convention 3).
/// </summary>
/// <remarks>
/// A shared operation throws only when its caller violates a documented precondition; the adapters
/// in each engine establish those, so no library exception reaches a script.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Low; Resources=1; Fingerprint=8B42D1
// Broiler-Human:        PENDING
public enum ResultStatus
{
    /// <summary>The operation completed, and its result is valid.</summary>
    Completed = 0,

    /// <summary>
    /// The input is not one the operation accepts: a malformed number, a bad escape, an invalid date
    /// string or a syntax error. A <see cref="Failure"/> says where, when there is a position.
    /// </summary>
    Malformed = 1,

    /// <summary>
    /// Completing would exceed a limit the caller gave (contract convention 5). No partial result is
    /// returned, and nothing is silently shortened.
    /// </summary>
    LimitExceeded = 2,
}
