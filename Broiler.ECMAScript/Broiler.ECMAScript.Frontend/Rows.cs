// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   212
// Annotated:        212/212
// Exempt:           0
// Human-reviewed:   0/212
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       212
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The catalogue rows the syntax half reports (stage SJL-13.2d; Broiler.VM's JSD-0083, decisions D10 and D11): one entry per
/// row the stage table (<c>Broiler.ECMAScript/docs/frontend/stages.tsv</c>) assigns <c>syntax</c>, each the row's shared code
/// and message identifier as the catalogue (<c>early-errors.tsv</c>) gives them. No <c>request</c>, <c>engine</c> or
/// <c>semantics</c> row has an entry, so the library cannot report one; the Frontend tests hold this list equal to the two
/// tables.
/// </summary>
// Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=27CB08
// Hairs-Human:        PENDING
internal static class Rows
{
    /// <summary>EE0001: <c>identifier.escape.invalid-start</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D93DCA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0001 = new("JSE1201", "identifier.escape.invalid-start");

    /// <summary>EE0002: <c>identifier.escape.invalid-part</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1ED90A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0002 = new("JSE1202", "identifier.escape.invalid-part");

    /// <summary>EE0003: <c>strict.legacy-octal-literal</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0C3CAE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0003 = new("JSE1203", "strict.legacy-octal-literal");

    /// <summary>EE0004: <c>strict.legacy-octal-escape</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=417799
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0004 = new("JSE1204", "strict.legacy-octal-escape");

    /// <summary>EE0005: <c>strict.binding.eval-or-arguments</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0DE8EE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0005 = new("JSE1301", "strict.binding.eval-or-arguments");

    /// <summary>EE0006: <c>strict.reserved-word</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=06244E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0006 = new("JSE1302", "strict.reserved-word");

    /// <summary>EE0007: <c>identifier.await.in-module</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=EA3BAD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0007 = new("JSE1303", "identifier.await.in-module");

    /// <summary>EE0008: <c>identifier.yield.in-generator</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=99F53E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0008 = new("JSE1304", "identifier.yield.in-generator");

    /// <summary>EE0009: <c>identifier.await.in-async</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F6DB64
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0009 = new("JSE1305", "identifier.await.in-async");

    /// <summary>EE0010: <c>identifier.yield.in-generator</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8D12C1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0010 = new("JSE1306", "identifier.yield.in-generator");

    /// <summary>EE0011: <c>identifier.await.in-async</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=9E2F07
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0011 = new("JSE1307", "identifier.await.in-async");

    /// <summary>EE0012: <c>strict.reserved-word</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=92F985
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0012 = new("JSE1308", "strict.reserved-word");

    /// <summary>EE0013: <c>identifier.await.in-module</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=33A294
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0013 = new("JSE1309", "identifier.await.in-module");

    /// <summary>EE0014: <c>identifier.reserved-word.escaped</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=FAD3EB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0014 = new("JSE1310", "identifier.reserved-word.escaped");

    /// <summary>EE0016: <c>object.method.private-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=ECA55E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0016 = new("JSE1312", "object.method.private-name");

    /// <summary>EE0017: <c>expression.shorthand-initializer</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8B4C27
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0017 = new("JSE1313", "expression.shorthand-initializer");

    /// <summary>EE0019: <c>regexp.literal.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E5B8EF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0019 = new("JSE1315", "regexp.literal.invalid");

    /// <summary>EE0020: <c>template.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=120AAD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0020 = new("JSE1316", "template.escape.invalid");

    /// <summary>EE0022: <c>template.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C87735
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0022 = new("JSE1318", "template.escape.invalid");

    /// <summary>EE0023: <c>template.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=271A84
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0023 = new("JSE1319", "template.escape.invalid");

    /// <summary>EE0024: <c>template.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=55E0A8
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0024 = new("JSE1320", "template.escape.invalid");

    /// <summary>EE0025: <c>cover.parenthesized-expression</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C261C2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0025 = new("JSE1321", "cover.parenthesized-expression");

    /// <summary>EE0026: <c>expression.optional-chain.tagged-template</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=AE2CDD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0026 = new("JSE1322", "expression.optional-chain.tagged-template");

    /// <summary>EE0027: <c>meta.import-meta.outside-module</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D1E5EC
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0027 = new("JSE1323", "meta.import-meta.outside-module");

    /// <summary>EE0028: <c>assignment.target.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7C012B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0028 = new("JSE1324", "assignment.target.invalid");

    /// <summary>EE0029: <c>assignment.target.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C58D29
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0029 = new("JSE1325", "assignment.target.invalid");

    /// <summary>EE0030: <c>strict.delete.unqualified-or-private</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2D9080
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0030 = new("JSE1326", "strict.delete.unqualified-or-private");

    /// <summary>EE0031: <c>strict.delete.unqualified-or-private</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8B69A0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0031 = new("JSE1327", "strict.delete.unqualified-or-private");

    /// <summary>EE0032: <c>cover.assignment-pattern</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D86303
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0032 = new("JSE1328", "cover.assignment-pattern");

    /// <summary>EE0033: <c>assignment.target.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D30766
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0033 = new("JSE1329", "assignment.target.invalid");

    /// <summary>EE0034: <c>assignment.target.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1CA4BC
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0034 = new("JSE1330", "assignment.target.invalid");

    /// <summary>EE0035: <c>assignment.target.not-simple</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=222866
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0035 = new("JSE1331", "assignment.target.not-simple");

    /// <summary>EE0036: <c>assignment.target.not-simple</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=02D373
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0036 = new("JSE1332", "assignment.target.not-simple");

    /// <summary>EE0037: <c>assignment.rest-property.nested-pattern</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1BD032
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0037 = new("JSE1333", "assignment.rest-property.nested-pattern");

    /// <summary>EE0038: <c>cover.assignment-pattern</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E3DE28
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0038 = new("JSE1334", "cover.assignment-pattern");

    /// <summary>EE0039: <c>assignment.target.not-simple</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F2F581
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0039 = new("JSE1335", "assignment.target.not-simple");

    /// <summary>EE0040: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F8F568
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0040 = new("JSE1401", "scope.lexical.duplicate");

    /// <summary>EE0041: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4306CB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0041 = new("JSE1402", "scope.lexical.var-collision");

    /// <summary>EE0042: <c>declaration.let.bound-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=937302
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0042 = new("JSE1403", "declaration.let.bound-name");

    /// <summary>EE0043: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=72B036
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0043 = new("JSE1404", "scope.lexical.duplicate");

    /// <summary>EE0044: <c>declaration.const.missing-initializer</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7BB62F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0044 = new("JSE1405", "declaration.const.missing-initializer");

    /// <summary>EE0045: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1E1010
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0045 = new("JSE1406", "statement.labelled-function");

    /// <summary>EE0046: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=AE11B9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0046 = new("JSE1407", "statement.labelled-function");

    /// <summary>EE0047: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=859A92
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0047 = new("JSE1408", "statement.labelled-function");

    /// <summary>EE0048: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7519C7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0048 = new("JSE1409", "statement.labelled-function");

    /// <summary>EE0049: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0699FF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0049 = new("JSE1410", "statement.labelled-function");

    /// <summary>EE0050: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D1E870
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0050 = new("JSE1411", "statement.labelled-function");

    /// <summary>EE0051: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=00A6C5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0051 = new("JSE1412", "scope.lexical.var-collision");

    /// <summary>EE0052: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F07DA6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0052 = new("JSE1413", "statement.labelled-function");

    /// <summary>EE0053: <c>cover.assignment-pattern</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E44E02
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0053 = new("JSE1414", "cover.assignment-pattern");

    /// <summary>EE0055: <c>declaration.let.bound-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5B250F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0055 = new("JSE1416", "declaration.let.bound-name");

    /// <summary>EE0056: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1F9E00
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0056 = new("JSE1417", "scope.lexical.var-collision");

    /// <summary>EE0057: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=682D19
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0057 = new("JSE1418", "scope.lexical.duplicate");

    /// <summary>EE0060: <c>strict.with</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D392F4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0060 = new("JSE1421", "strict.with");

    /// <summary>EE0061: <c>statement.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CA1AB9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0061 = new("JSE1422", "statement.labelled-function");

    /// <summary>EE0062: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D3E8EA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0062 = new("JSE1423", "scope.lexical.duplicate");

    /// <summary>EE0063: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=10D8F4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0063 = new("JSE1424", "scope.lexical.var-collision");

    /// <summary>EE0064: <c>strict.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=60A39D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0064 = new("JSE1425", "strict.labelled-function");

    /// <summary>EE0065: <c>scope.catch.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C1AEF4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0065 = new("JSE1426", "scope.catch.duplicate");

    /// <summary>EE0066: <c>scope.catch.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D6D9EB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0066 = new("JSE1427", "scope.catch.lexical-collision");

    /// <summary>EE0067: <c>scope.catch.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A0FB71
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0067 = new("JSE1428", "scope.catch.var-collision");

    /// <summary>EE0068: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5FEDC0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0068 = new("JSE1501", "scope.parameter.duplicate");

    /// <summary>EE0069: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7DD4EC
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0069 = new("JSE1502", "scope.parameter.duplicate");

    /// <summary>EE0070: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3D84E2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0070 = new("JSE1503", "scope.parameter.duplicate");

    /// <summary>EE0071: <c>strict.binding.eval-or-arguments</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=61DBD0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0071 = new("JSE1504", "strict.binding.eval-or-arguments");

    /// <summary>EE0072: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=049D55
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0072 = new("JSE1505", "function.use-strict.non-simple-parameters");

    /// <summary>EE0073: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B84297
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0073 = new("JSE1506", "scope.parameter.lexical-collision");

    /// <summary>EE0078: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=36FF10
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0078 = new("JSE1511", "scope.lexical.duplicate");

    /// <summary>EE0079: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=56E3F1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0079 = new("JSE1512", "scope.lexical.var-collision");

    /// <summary>EE0080: <c>jump.label.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2F585C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0080 = new("JSE1513", "jump.label.duplicate");

    /// <summary>EE0083: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=706FB7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0083 = new("JSE1516", "function.parameters.yield");

    /// <summary>EE0084: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A6251F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0084 = new("JSE1517", "function.parameters.await");

    /// <summary>EE0085: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5B2F48
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0085 = new("JSE1518", "function.use-strict.non-simple-parameters");

    /// <summary>EE0086: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=03B6FF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0086 = new("JSE1519", "scope.parameter.lexical-collision");

    /// <summary>EE0087: <c>cover.arrow-formal-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D44CD1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0087 = new("JSE1520", "cover.arrow-formal-parameters");

    /// <summary>EE0088: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=610149
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0088 = new("JSE1521", "function.use-strict.non-simple-parameters");

    /// <summary>EE0089: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=AD0A00
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0089 = new("JSE1522", "scope.parameter.lexical-collision");

    /// <summary>EE0090: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5F0A88
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0090 = new("JSE1523", "scope.parameter.duplicate");

    /// <summary>EE0091: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6BB96E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0091 = new("JSE1524", "function.use-strict.non-simple-parameters");

    /// <summary>EE0092: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3FDE58
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0092 = new("JSE1525", "scope.parameter.lexical-collision");

    /// <summary>EE0094: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=570C10
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0094 = new("JSE1527", "function.parameters.yield");

    /// <summary>EE0095: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5E8373
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0095 = new("JSE1528", "function.use-strict.non-simple-parameters");

    /// <summary>EE0096: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3E1825
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0096 = new("JSE1529", "scope.parameter.lexical-collision");

    /// <summary>EE0097: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=00EE41
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0097 = new("JSE1530", "scope.parameter.duplicate");

    /// <summary>EE0098: <c>strict.binding.eval-or-arguments</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=00AB31
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0098 = new("JSE1531", "strict.binding.eval-or-arguments");

    /// <summary>EE0099: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=665AE5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0099 = new("JSE1532", "function.use-strict.non-simple-parameters");

    /// <summary>EE0100: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E63027
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0100 = new("JSE1533", "scope.parameter.lexical-collision");

    /// <summary>EE0101: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7398F6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0101 = new("JSE1534", "function.parameters.yield");

    /// <summary>EE0107: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=16171E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0107 = new("JSE1540", "function.parameters.yield");

    /// <summary>EE0108: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4B39A4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0108 = new("JSE1541", "function.parameters.await");

    /// <summary>EE0109: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8ACF2A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0109 = new("JSE1542", "function.use-strict.non-simple-parameters");

    /// <summary>EE0110: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7F6EEF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0110 = new("JSE1543", "scope.parameter.lexical-collision");

    /// <summary>EE0111: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5CEAA9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0111 = new("JSE1544", "scope.parameter.duplicate");

    /// <summary>EE0112: <c>strict.binding.eval-or-arguments</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8FFA47
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0112 = new("JSE1545", "strict.binding.eval-or-arguments");

    /// <summary>EE0113: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BABF55
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0113 = new("JSE1546", "function.use-strict.non-simple-parameters");

    /// <summary>EE0114: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4C6A80
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0114 = new("JSE1547", "scope.parameter.lexical-collision");

    /// <summary>EE0115: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=04127D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0115 = new("JSE1548", "function.parameters.yield");

    /// <summary>EE0116: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0C4C16
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0116 = new("JSE1549", "function.parameters.await");

    /// <summary>EE0122: <c>class.constructor.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A951AA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0122 = new("JSE1555", "class.constructor.duplicate");

    /// <summary>EE0123: <c>class.private-name.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6E9249
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0123 = new("JSE1556", "class.private-name.duplicate");

    /// <summary>EE0125: <c>class.constructor.special-method</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0C152E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0125 = new("JSE1558", "class.constructor.special-method");

    /// <summary>EE0127: <c>class.static-method.prototype</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=82CA97
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0127 = new("JSE1560", "class.static-method.prototype");

    /// <summary>EE0128: <c>class.field.constructor</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=EDE718
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0128 = new("JSE1561", "class.field.constructor");

    /// <summary>EE0129: <c>class.static-field.prototype-or-constructor</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=179C1B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0129 = new("JSE1562", "class.static-field.prototype-or-constructor");

    /// <summary>EE0132: <c>class.private-name.constructor</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4B835F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0132 = new("JSE1565", "class.private-name.constructor");

    /// <summary>EE0133: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4A97A2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0133 = new("JSE1566", "scope.lexical.duplicate");

    /// <summary>EE0134: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D6A590
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0134 = new("JSE1567", "scope.lexical.var-collision");

    /// <summary>EE0135: <c>jump.label.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1FBBEB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0135 = new("JSE1568", "jump.label.duplicate");

    /// <summary>EE0140: <c>class.static-block.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=883703
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0140 = new("JSE1573", "class.static-block.await");

    /// <summary>EE0141: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0C119A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0141 = new("JSE1574", "function.use-strict.non-simple-parameters");

    /// <summary>EE0143: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=930D2C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0143 = new("JSE1576", "function.parameters.await");

    /// <summary>EE0144: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E8416B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0144 = new("JSE1577", "scope.parameter.lexical-collision");

    /// <summary>EE0145: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=76D1C9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0145 = new("JSE1578", "function.use-strict.non-simple-parameters");

    /// <summary>EE0146: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5C97BB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0146 = new("JSE1579", "function.parameters.await");

    /// <summary>EE0147: <c>scope.parameter.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=72A204
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0147 = new("JSE1580", "scope.parameter.duplicate");

    /// <summary>EE0148: <c>strict.binding.eval-or-arguments</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D68776
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0148 = new("JSE1581", "strict.binding.eval-or-arguments");

    /// <summary>EE0149: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4FB6D2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0149 = new("JSE1582", "scope.parameter.lexical-collision");

    /// <summary>EE0154: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CB05C2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0154 = new("JSE1587", "scope.parameter.lexical-collision");

    /// <summary>EE0155: <c>cover.async-arrow-head</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6DA1B6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0155 = new("JSE1588", "cover.async-arrow-head");

    /// <summary>EE0156: <c>function.parameters.yield</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=64F98D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0156 = new("JSE1589", "function.parameters.yield");

    /// <summary>EE0157: <c>function.parameters.await</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BF7D9A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0157 = new("JSE1590", "function.parameters.await");

    /// <summary>EE0158: <c>scope.parameter.lexical-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B53212
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0158 = new("JSE1591", "scope.parameter.lexical-collision");

    /// <summary>EE0159: <c>function.use-strict.non-simple-parameters</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B032C1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0159 = new("JSE1592", "function.use-strict.non-simple-parameters");

    /// <summary>EE0160: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=376397
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0160 = new("JSE1601", "scope.lexical.duplicate");

    /// <summary>EE0161: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C219AE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0161 = new("JSE1602", "scope.lexical.var-collision");

    /// <summary>EE0163: <c>meta.new-target.outside-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B2AE18
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0163 = new("JSE1604", "meta.new-target.outside-function");

    /// <summary>EE0164: <c>jump.label.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E2E0C0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0164 = new("JSE1605", "jump.label.duplicate");

    /// <summary>EE0168: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=DF729B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0168 = new("JSE1609", "scope.lexical.duplicate");

    /// <summary>EE0169: <c>scope.lexical.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3F032B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0169 = new("JSE1610", "scope.lexical.var-collision");

    /// <summary>EE0173: <c>meta.new-target.outside-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0C63F1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0173 = new("JSE1614", "meta.new-target.outside-function");

    /// <summary>EE0174: <c>jump.label.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0A95CA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0174 = new("JSE1615", "jump.label.duplicate");

    /// <summary>EE0178: <c>module.export-name.ill-formed</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3B52FB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0178 = new("JSE1619", "module.export-name.ill-formed");

    /// <summary>EE0179: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A17A72
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0179 = new("JSE1620", "scope.lexical.duplicate");

    /// <summary>EE0180: <c>module.import-attribute.duplicate-key</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=33C143
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0180 = new("JSE1621", "module.import-attribute.duplicate-key");

    /// <summary>EE0181: <c>module.export.string-local-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=318304
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0181 = new("JSE1622", "module.export.string-local-name");

    /// <summary>EE0182: <c>module.export.reserved-local-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E41812
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0182 = new("JSE1623", "module.export.reserved-local-name");

    /// <summary>EE0183: <c>regexp.pattern.too-many-groups</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=ADA0AA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0183 = new("JSE2201", "regexp.pattern.too-many-groups");

    /// <summary>EE0184: <c>regexp.group-name.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=927B0C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0184 = new("JSE2202", "regexp.group-name.duplicate");

    /// <summary>EE0185: <c>regexp.quantifier.out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2C272B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0185 = new("JSE2203", "regexp.quantifier.out-of-order");

    /// <summary>EE0186: <c>regexp.modifiers.repeated-flag</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B339A2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0186 = new("JSE2204", "regexp.modifiers.repeated-flag");

    /// <summary>EE0187: <c>regexp.modifiers.empty</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A1BB30
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0187 = new("JSE2205", "regexp.modifiers.empty");

    /// <summary>EE0188: <c>regexp.modifiers.repeated-flag</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=821EDB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0188 = new("JSE2206", "regexp.modifiers.repeated-flag");

    /// <summary>EE0189: <c>regexp.modifiers.repeated-flag</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1C644D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0189 = new("JSE2207", "regexp.modifiers.repeated-flag");

    /// <summary>EE0190: <c>regexp.modifiers.added-and-removed</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C6B864
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0190 = new("JSE2208", "regexp.modifiers.added-and-removed");

    /// <summary>EE0191: <c>regexp.backreference.undefined-group-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=67B174
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0191 = new("JSE2209", "regexp.backreference.undefined-group-name");

    /// <summary>EE0192: <c>regexp.backreference.group-number-too-large</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CE6B2A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0192 = new("JSE2210", "regexp.backreference.group-number-too-large");

    /// <summary>EE0193: <c>regexp.class.range-endpoint-is-class</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=52C15F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0193 = new("JSE2211", "regexp.class.range-endpoint-is-class");

    /// <summary>EE0194: <c>regexp.class.range-out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B6CF0A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0194 = new("JSE2212", "regexp.class.range-out-of-order");

    /// <summary>EE0195: <c>regexp.class.range-endpoint-is-class</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=32FEE6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0195 = new("JSE2213", "regexp.class.range-endpoint-is-class");

    /// <summary>EE0196: <c>regexp.class.range-out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B4593F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0196 = new("JSE2214", "regexp.class.range-out-of-order");

    /// <summary>EE0197: <c>regexp.group-name.invalid-start</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5C206D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0197 = new("JSE2215", "regexp.group-name.invalid-start");

    /// <summary>EE0198: <c>regexp.group-name.invalid-start</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7C437D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0198 = new("JSE2216", "regexp.group-name.invalid-start");

    /// <summary>EE0199: <c>regexp.group-name.invalid-part</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D988B4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0199 = new("JSE2217", "regexp.group-name.invalid-part");

    /// <summary>EE0200: <c>regexp.group-name.invalid-part</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=AD5568
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0200 = new("JSE2218", "regexp.group-name.invalid-part");

    /// <summary>EE0201: <c>regexp.property.unknown-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0080DC
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0201 = new("JSE2219", "regexp.property.unknown-name");

    /// <summary>EE0202: <c>regexp.property.unknown-value</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C63AE6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0202 = new("JSE2220", "regexp.property.unknown-value");

    /// <summary>EE0203: <c>regexp.property.unknown-lone-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=DFF22B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0203 = new("JSE2221", "regexp.property.unknown-lone-name");

    /// <summary>EE0204: <c>regexp.property.of-strings-outside-v-mode</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F5E86C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0204 = new("JSE2222", "regexp.property.of-strings-outside-v-mode");

    /// <summary>EE0205: <c>regexp.property.negated-strings</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=AA2154
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0205 = new("JSE2223", "regexp.property.negated-strings");

    /// <summary>EE0206: <c>regexp.class.negated-may-contain-strings</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=373504
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0206 = new("JSE2224", "regexp.class.negated-may-contain-strings");

    /// <summary>EE0207: <c>regexp.class.negated-may-contain-strings</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D6DC5F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0207 = new("JSE2225", "regexp.class.negated-may-contain-strings");

    /// <summary>EE0208: <c>regexp.class.range-out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=537EDD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0208 = new("JSE2226", "regexp.class.range-out-of-order");

    /// <summary>EE0209: <c>regexp.quantifier.braced-without-atom</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1EE480
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0209 = new("JSE8101", "regexp.quantifier.braced-without-atom");

    /// <summary>EE0210: <c>regexp.class.range-endpoint-is-class</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=922160
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0210 = new("JSE8102", "regexp.class.range-endpoint-is-class");

    /// <summary>EE0211: <c>regexp.class.range-out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A21A8B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0211 = new("JSE8103", "regexp.class.range-out-of-order");

    /// <summary>EE0212: <c>regexp.class.range-endpoint-is-class</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=045994
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0212 = new("JSE8104", "regexp.class.range-endpoint-is-class");

    /// <summary>EE0213: <c>regexp.class.range-out-of-order</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=559500
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0213 = new("JSE8105", "regexp.class.range-out-of-order");

    /// <summary>EA0001: <c>eval.source.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2681A4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0001 = new("JSE1901", "eval.source.invalid");

    /// <summary>EA0002: <c>meta.new-target.outside-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=9E535A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0002 = new("JSE1902", "meta.new-target.outside-function");

    /// <summary>EA0010: <c>dynamic-function.parameters.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BBD361
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0010 = new("JSE2001", "dynamic-function.parameters.invalid");

    /// <summary>EA0011: <c>dynamic-function.body.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CF929D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0011 = new("JSE2002", "dynamic-function.body.invalid");

    /// <summary>EA0012: <c>dynamic-function.source.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=667813
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0012 = new("JSE2003", "dynamic-function.source.invalid");

    /// <summary>EG0001: <c>annexb.html-comment.module</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=507335
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0001 = new("JSE8106", "annexb.html-comment.module");

    /// <summary>EG0002: <c>annexb.regexp.unicode-mode</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=FA78E4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0002 = new("JSE8107", "annexb.regexp.unicode-mode");

    /// <summary>EG0003: <c>strict.labelled-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3F760F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0003 = new("JSE8301", "strict.labelled-function");

    /// <summary>EG0004: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1E6AF2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0004 = new("JSE8302", "scope.lexical.duplicate");

    /// <summary>EG0005: <c>annexb.if-function.strict</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6ED1E6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0005 = new("JSE8303", "annexb.if-function.strict");

    /// <summary>EG0006: <c>scope.catch.var-collision</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4603F7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0006 = new("JSE8304", "scope.catch.var-collision");

    /// <summary>EG0007: <c>annexb.for-in-initializer.strict</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=9AC4AF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0007 = new("JSE8305", "annexb.for-in-initializer.strict");

    /// <summary>EG0008: <c>assignment.target.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1ABA68
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EG0008 = new("JSE8306", "assignment.target.invalid");

    /// <summary>EP0001: <c>declaration.let.bound-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3ACBA2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0001 = new("JSE7101", "declaration.let.bound-name");

    /// <summary>EP0002: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1C3BCD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0002 = new("JSE7102", "scope.lexical.duplicate");

    /// <summary>EP0003: <c>cover.await-using-declaration-head</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3AB0A1
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0003 = new("JSE7103", "cover.await-using-declaration-head");

    /// <summary>EP0004: <c>declaration.let.bound-name</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F1CCCE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0004 = new("JSE7104", "declaration.let.bound-name");

    /// <summary>EP0005: <c>scope.lexical.duplicate</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=FC6CAA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0005 = new("JSE7105", "scope.lexical.duplicate");

    /// <summary>EP0006: <c>using.declaration.no-disposal-scope</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=67DE4C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0006 = new("JSE7106", "using.declaration.no-disposal-scope");

    /// <summary>EP0007: <c>using.declaration.no-disposal-scope</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=656936
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0007 = new("JSE7107", "using.declaration.no-disposal-scope");

    /// <summary>EP0008: <c>using.declaration.no-disposal-scope</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CBD1EA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0008 = new("JSE7108", "using.declaration.no-disposal-scope");

    /// <summary>EP0009: <c>decorator.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=34D615
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0009 = new("JSE7201", "decorator.not-in-edition");

    /// <summary>EP0010: <c>decorator.auto-accessor.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=95E50B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0010 = new("JSE7202", "decorator.auto-accessor.not-in-edition");

    /// <summary>EP0011: <c>import-phase.source.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=C137A7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0011 = new("JSE7301", "import-phase.source.not-in-edition");

    /// <summary>EP0012: <c>import-phase.source-call.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=842807
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0012 = new("JSE7302", "import-phase.source-call.not-in-edition");

    /// <summary>EP0013: <c>import-phase.defer.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=EEC59D
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0013 = new("JSE7303", "import-phase.defer.not-in-edition");

    /// <summary>EP0014: <c>import-phase.defer-call.not-in-edition</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=9FFF1C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0014 = new("JSE7304", "import-phase.defer-call.not-in-edition");

    /// <summary>EP0015: <c>eval.source.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=20B332
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0015 = new("JSE7401", "eval.source.invalid");

    /// <summary>EP0016: <c>meta.new-target.outside-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D4A8E2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0016 = new("JSE7402", "meta.new-target.outside-function");

    /// <summary>EX0001: <c>syntax.character.unexpected</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=11BF50
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0001 = new("JSE9101", "syntax.character.unexpected");

    /// <summary>EX0002: <c>syntax.comment.unterminated</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=9FCDC2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0002 = new("JSE9102", "syntax.comment.unterminated");

    /// <summary>EX0003: <c>syntax.numeric-literal.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5CE1E3
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0003 = new("JSE9103", "syntax.numeric-literal.invalid");

    /// <summary>EX0004: <c>syntax.string.unterminated</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=80EF63
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0004 = new("JSE9104", "syntax.string.unterminated");

    /// <summary>EX0005: <c>syntax.string.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B54ADD
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0005 = new("JSE9105", "syntax.string.escape.invalid");

    /// <summary>EX0006: <c>syntax.regexp.unterminated</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6893EF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0006 = new("JSE9106", "syntax.regexp.unterminated");

    /// <summary>EX0007: <c>syntax.template.unterminated</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B62DC8
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0007 = new("JSE9107", "syntax.template.unterminated");

    /// <summary>EX0008: <c>syntax.token.unexpected</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=942359
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0008 = new("JSE9108", "syntax.token.unexpected");

    /// <summary>EX0009: <c>syntax.token.missing</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A71593
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0009 = new("JSE9109", "syntax.token.missing");

    /// <summary>EX0010: <c>syntax.module-item.in-script</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=482667
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0010 = new("JSE9110", "syntax.module-item.in-script");

    /// <summary>EX0011: <c>limit.nesting.too-deep</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=D2C860
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0011 = new("JSE9201", "limit.nesting.too-deep");

    /// <summary>EX0012: <c>limit.tree.too-deep</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3801FE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0012 = new("JSE9202", "limit.tree.too-deep");

    /// <summary>EX0025: <c>syntax.identifier.escape.invalid</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2366A4
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0025 = new("JSE9111", "syntax.identifier.escape.invalid");

    /// <summary>EX0032: <c>syntax.return.outside-function</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=DB5E1A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0032 = new("JSE9112", "syntax.return.outside-function");

    /// <summary>EX0033: <c>syntax.return.in-static-block</c>.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=707718
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EX0033 = new("JSE9113", "syntax.return.in-static-block");
}
