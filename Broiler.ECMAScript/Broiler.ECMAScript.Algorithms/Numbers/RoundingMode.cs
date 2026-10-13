// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           14
// Human-reviewed:   0/2
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Numbers;

/// <summary>
/// The rounding modes of Temporal and of ECMA-402's <c>NumberFormat</c>, by their option names
/// (Temporal, Table 22 "Rounding modes"; ECMA-402 15.5.11): <c>"ceil"</c>, <c>"floor"</c>,
/// <c>"expand"</c>, <c>"trunc"</c>, <c>"halfCeil"</c>, <c>"halfFloor"</c>, <c>"halfExpand"</c>,
/// <c>"halfTrunc"</c> and <c>"halfEven"</c>.
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal Table 22; IP=Low; Security=Low; Resources=1; Fingerprint=FD394E
// Broiler-Human:        PENDING
public enum RoundingMode
{
    /// <summary><c>"ceil"</c>: toward +Infinity.</summary>
    Ceil = 0,

    /// <summary><c>"floor"</c>: toward -Infinity.</summary>
    Floor = 1,

    /// <summary><c>"expand"</c>: away from zero.</summary>
    Expand = 2,

    /// <summary><c>"trunc"</c>: toward zero.</summary>
    Trunc = 3,

    /// <summary><c>"halfCeil"</c>: to nearest, a tie toward +Infinity.</summary>
    HalfCeil = 4,

    /// <summary><c>"halfFloor"</c>: to nearest, a tie toward -Infinity.</summary>
    HalfFloor = 5,

    /// <summary><c>"halfExpand"</c>: to nearest, a tie away from zero.</summary>
    HalfExpand = 6,

    /// <summary><c>"halfTrunc"</c>: to nearest, a tie toward zero.</summary>
    HalfTrunc = 7,

    /// <summary><c>"halfEven"</c>: to nearest, a tie to the even multiple.</summary>
    HalfEven = 8,
}

/// <summary>
/// The unsigned rounding modes the signed ones reduce to once the sign is known (Temporal,
/// <c>GetUnsignedRoundingMode</c>, Table 23; ECMA-402 15.5.13).
/// </summary>
// Broiler-AI:           Origin=AI; Spec=Temporal Table 23; IP=Low; Security=Low; Resources=1; Fingerprint=147A93
// Broiler-Human:        PENDING
public enum UnsignedRoundingMode
{
    /// <summary><c>zero</c>: to the lower end.</summary>
    Zero = 0,

    /// <summary><c>infinity</c>: to the upper end.</summary>
    Infinity = 1,

    /// <summary><c>half-zero</c>: to the nearer end, a tie to the lower.</summary>
    HalfZero = 2,

    /// <summary><c>half-infinity</c>: to the nearer end, a tie to the upper.</summary>
    HalfInfinity = 3,

    /// <summary><c>half-even</c>: to the nearer end, a tie to the end that is an even multiple.</summary>
    HalfEven = 4,
}
