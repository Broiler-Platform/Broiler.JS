// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   1
// Annotated:        1/1
// Exempt:           0
// Human-reviewed:   0/1
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       1
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript;

/// <summary>
/// Charges work to the caller's budget, before the work it charges for is done.
/// </summary>
/// <param name="units">
/// The work about to be done, in the units the operation that charges documents: code units, limbs
/// or steps. An operation whose steps are fine-grained charges them in blocks.
/// </param>
/// <remarks>
/// <para>
/// This is the metering contract of decision SJL-D-10 (a) and contract convention 4: every shared
/// operation whose work grows with its input takes one, nullable for a caller with no budget. The
/// delegate may throw - Broiler.VM's raises its abort on exhaustion or cancellation - and an
/// operation leaves no shared state changed when it does. An operation never reads a clock and never
/// polls cancellation itself; the delegate is where a caller does both.
/// </para>
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=5E3A07
// Broiler-Human:        PENDING
public delegate void WorkCharge(ulong units);
