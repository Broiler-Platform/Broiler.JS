// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           3
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Medium
// Criteria:         0/0
// Resource impact:  1/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System;

namespace Broiler.ECMAScript;

/// <summary>
/// An expected failure of a shared operation, as data: its status, where in the input it was found,
/// and a stable message identifier each engine renders as its own text (decision SJL-D-11).
/// </summary>
/// <remarks>
/// Message text is not part of the contract. The identifier is: an engine keys its own message on
/// it, so changing an identifier is a breaking change and changing an engine's text is not.
/// </remarks>
// Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Low; Resources=1; Fingerprint=B9AD3E
// Broiler-Human:        PENDING
public readonly record struct Failure
{
    /// <summary>The value that means "the failure has no position in the input".</summary>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Low; Resources=1; Fingerprint=55EA0C
    // Broiler-Human:        PENDING
    public const int NoPosition = -1;

    /// <summary>Creates a failure.</summary>
    /// <param name="status">Why the operation failed; never <see cref="ResultStatus.Completed"/>.</param>
    /// <param name="position">The UTF-16 index in the input where the failure was found, or <see cref="NoPosition"/>.</param>
    /// <param name="messageId">The stable identifier of the message, for example <c>number.malformed-exponent</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status is <see cref="ResultStatus.Completed"/>, or the position is below <see cref="NoPosition"/>.</exception>
    /// <exception cref="ArgumentException">The message identifier is empty.</exception>
    // Broiler-AI:           Origin=AI; Spec=SJL architecture s4; IP=Low; Security=Medium; Resources=1; Fingerprint=C260F1
    // Broiler-Human:        PENDING
    public Failure(ResultStatus status, int position, string messageId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(status, ResultStatus.Completed);
        ArgumentOutOfRangeException.ThrowIfLessThan(position, NoPosition);
        ArgumentException.ThrowIfNullOrEmpty(messageId);
        Status = status;
        Position = position;
        MessageId = messageId;
    }

    /// <summary>Why the operation failed.</summary>
    public ResultStatus Status { get; }

    /// <summary>The UTF-16 index in the input where the failure was found, or <see cref="NoPosition"/>.</summary>
    public int Position { get; }

    /// <summary>The stable identifier of the message an engine renders for this failure.</summary>
    public string MessageId { get; }
}
