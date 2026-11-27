// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// HAIRS Code Assurance
// --------------------
// Relevant units:   55
// Annotated:        55/55
// Exempt:           0
// Human-reviewed:   0/55
// IP risk:          Low
// Security risk:    Low
// Criteria:         0/0
// Resource impact:  0/10 max
// Unverified:       55
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.ECMAScript.Syntax;

/// <summary>
/// The catalogue rows the semantic model reports (milestone SJL-15.2, from stage 2; Broiler.VM's JSD-0080, one early-error
/// stage): one entry per row the stage table (<c>Broiler.ECMAScript/docs/frontend/stages.tsv</c>) assigns <c>semantics</c>
/// that the library reports, each the row's shared code and message identifier as the catalogue (<c>early-errors.tsv</c>)
/// gives them: stage 2's rows of the function context and of expressions, stage 3's rows of jump targets (the
/// session's decision D-js-jump-rows), and stage 4's rows of private names (the session's decision D-js-private-rows).
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 3, when its rows joined the table; it
/// read "(milestone SJL-15.2, stage 2; Broiler.VM's JSD-0080, one early-error stage)" and ended "gives them".)</i>
/// <i>(Revised 2026-11-25, at SJL-15.2 stage 4, when its rows joined the table; it read "stage 2's rows of the function
/// context and of expressions, and stage 3's rows of jump targets (the session's decision D-js-jump-rows).")</i> The declarations walk reports them only when the request sets
/// <see cref="ParseRequest.BuildSemanticModel"/>, through the same report as <see cref="Rows"/>' rows, in decision D11's
/// order (the session's decisions D-js-channel and D-js-semantic-rows). EP0017 and EP0018 have no report of their own:
/// no goal is ShadowRealm's, and EA0003, EA0004 or EE0162 refuses their source first. The Frontend tests hold this list a
/// subset of the stage table's <c>semantics</c> rows, to be equal at stage 8 but for EA0006, and hold that no code here is
/// one of <see cref="Rows"/>'.
/// Stage 5c's rows of eval code's <c>var</c>s against its caller's records, EvalDeclarationInstantiation step 3.d
/// (the session's decision D-js-caller-scope-rows), join them. <i>(Added 2026-11-26, at SJL-15.2 stage 5c.)</i>
/// Stage 6's rows of the module record, ExportedNames' duplicates and ExportedBindings not declared (the session's decision
/// D-js-module-rows), join them; the full walk alone reports them, after its visit, the whole ModuleItemList deciding
/// them. <i>(Added 2026-11-27, at SJL-15.2 stage 6.)</i>
/// </summary>
// Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=92ED1E
// Hairs-Human:        PENDING
internal static class SemanticRows
{
    /// <summary>EE0015: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of an object literal's method, getter or setter (clause 13.2.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3F62A5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0015 = new("JSE1311", "super.call.outside-derived-constructor");

    /// <summary>EE0018: <c>object.proto.duplicate</c>, a second <c>__proto__: value</c> entry of one object literal (clause 13.2.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2B5654
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0018 = new("JSE1314", "object.proto.duplicate");

    /// <summary>EE0021: <c>template.too-many-strings</c>, a template of 2^32 or more strings (clause 13.2.8.1); no source reaches it.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=CC02F7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0021 = new("JSE1317", "template.too-many-strings");

    /// <summary>EE0054: <c>assignment.target.invalid</c>, a for-in or for-of head whose target is not a reference (clause 14.7.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=0D15D9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0054 = new("JSE1415", "assignment.target.invalid");

    /// <summary>EE0058: <c>jump.continue.no-iteration</c>, a <c>continue</c> in no iteration statement of its function, static block or program (clause 14.8.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A5E68A
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0058 = new("JSE1419", "jump.continue.no-iteration");

    /// <summary>EE0059: <c>jump.break.no-target</c>, a <c>break;</c> in no iteration statement or <c>switch</c> of its function, static block or program (clause 14.9.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1B07B7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0059 = new("JSE1420", "jump.break.no-target");

    /// <summary>EE0074: <c>super.property.outside-method</c>, a SuperProperty in a function's parameters (clause 15.2.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8CD953
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0074 = new("JSE1507", "super.property.outside-method");

    /// <summary>EE0075: <c>super.property.outside-method</c>, a SuperProperty in a function's body (clause 15.2.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1E94D0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0075 = new("JSE1508", "super.property.outside-method");

    /// <summary>EE0076: <c>super.call.outside-derived-constructor</c>, a SuperCall in a function's parameters (clause 15.2.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BC1A09
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0076 = new("JSE1509", "super.call.outside-derived-constructor");

    /// <summary>EE0077: <c>super.call.outside-derived-constructor</c>, a SuperCall in a function's body (clause 15.2.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E0AE5B
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0077 = new("JSE1510", "super.call.outside-derived-constructor");

    /// <summary>EE0081: <c>jump.break.undefined-label</c>, ContainsUndefinedBreakTarget of a function body (clause 15.2.1): a <c>break L</c> no enclosing statement of the body labels; stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8EC093
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0081 = new("JSE1514", "jump.break.undefined-label");

    /// <summary>EE0082: <c>jump.continue.undefined-label</c>, ContainsUndefinedContinueTarget of a function body (clause 15.2.1): a <c>continue L</c> no enclosing iteration statement of the body has in its label set; stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6A00E0
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0082 = new("JSE1515", "jump.continue.undefined-label");

    /// <summary>EE0093: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of a generator method (clause 15.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=659070
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0093 = new("JSE1526", "super.call.outside-derived-constructor");

    /// <summary>EE0102: <c>super.property.outside-method</c>, a SuperProperty in a generator's parameters (clause 15.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=35FFCC
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0102 = new("JSE1535", "super.property.outside-method");

    /// <summary>EE0103: <c>super.property.outside-method</c>, a SuperProperty in a generator's body (clause 15.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=21E2BB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0103 = new("JSE1536", "super.property.outside-method");

    /// <summary>EE0104: <c>super.call.outside-derived-constructor</c>, a SuperCall in a generator's parameters (clause 15.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3EB83C
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0104 = new("JSE1537", "super.call.outside-derived-constructor");

    /// <summary>EE0105: <c>super.call.outside-derived-constructor</c>, a SuperCall in a generator's body (clause 15.5.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B5912F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0105 = new("JSE1538", "super.call.outside-derived-constructor");

    /// <summary>EE0106: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of an async generator method (clause 15.6.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=389CE7
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0106 = new("JSE1539", "super.call.outside-derived-constructor");

    /// <summary>EE0117: <c>super.property.outside-method</c>, a SuperProperty in an async generator's parameters (clause 15.6.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B0BE35
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0117 = new("JSE1550", "super.property.outside-method");

    /// <summary>EE0118: <c>super.property.outside-method</c>, a SuperProperty in an async generator's body (clause 15.6.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B08482
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0118 = new("JSE1551", "super.property.outside-method");

    /// <summary>EE0119: <c>super.call.outside-derived-constructor</c>, a SuperCall in an async generator's parameters (clause 15.6.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=904907
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0119 = new("JSE1552", "super.call.outside-derived-constructor");

    /// <summary>EE0120: <c>super.call.outside-derived-constructor</c>, a SuperCall in an async generator's body (clause 15.6.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=24CB92
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0120 = new("JSE1553", "super.call.outside-derived-constructor");

    /// <summary>EE0121: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of the constructor of a class with no heritage (clause 15.7.1, ClassTail).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=673DEA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0121 = new("JSE1554", "super.call.outside-derived-constructor");

    /// <summary>EE0124: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of a class's method that is not its constructor (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=348042
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0124 = new("JSE1557", "super.call.outside-derived-constructor");

    /// <summary>EE0126: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of a class's static method (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=F35861
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0126 = new("JSE1559", "super.call.outside-derived-constructor");

    /// <summary>EE0130: <c>class.initializer.arguments</c>, ContainsArguments of a field's initialiser (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4D63F5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0130 = new("JSE1563", "class.initializer.arguments");

    /// <summary>EE0131: <c>super.call.outside-derived-constructor</c>, a SuperCall in a field's initialiser (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=2FE3E6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0131 = new("JSE1564", "super.call.outside-derived-constructor");

    /// <summary>EE0136: <c>jump.break.undefined-label</c>, ContainsUndefinedBreakTarget of a class static block (clause 15.7.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=5E973E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0136 = new("JSE1569", "jump.break.undefined-label");

    /// <summary>EE0137: <c>jump.continue.undefined-label</c>, ContainsUndefinedContinueTarget of a class static block (clause 15.7.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8B7A00
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0137 = new("JSE1570", "jump.continue.undefined-label");

    /// <summary>EE0138: <c>class.initializer.arguments</c>, ContainsArguments of a static block (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=B3D47F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0138 = new("JSE1571", "class.initializer.arguments");

    /// <summary>EE0139: <c>super.call.outside-derived-constructor</c>, a SuperCall in a static block (clause 15.7.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7D13A2
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0139 = new("JSE1572", "super.call.outside-derived-constructor");

    /// <summary>EE0142: <c>super.call.outside-derived-constructor</c>, HasDirectSuper of an async method (clause 15.8.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4974E3
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0142 = new("JSE1575", "super.call.outside-derived-constructor");

    /// <summary>EE0150: <c>super.property.outside-method</c>, a SuperProperty in an async function's parameters (clause 15.8.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=00FEED
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0150 = new("JSE1583", "super.property.outside-method");

    /// <summary>EE0151: <c>super.property.outside-method</c>, a SuperProperty in an async function's body (clause 15.8.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=17A87F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0151 = new("JSE1584", "super.property.outside-method");

    /// <summary>EE0152: <c>super.call.outside-derived-constructor</c>, a SuperCall in an async function's parameters (clause 15.8.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E91BAA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0152 = new("JSE1585", "super.call.outside-derived-constructor");

    /// <summary>EE0153: <c>super.call.outside-derived-constructor</c>, a SuperCall in an async function's body (clause 15.8.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=E7A702
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0153 = new("JSE1586", "super.call.outside-derived-constructor");

    /// <summary>EE0162: <c>super.outside-method</c>, <c>super</c> in a Script's own code (clause 16.1.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BC265F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0162 = new("JSE1603", "super.outside-method");

    /// <summary>EE0165: <c>jump.break.undefined-label</c>, ContainsUndefinedBreakTarget of a Script's statements, eval code's too (clause 16.1.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=27B16F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0165 = new("JSE1606", "jump.break.undefined-label");

    /// <summary>EE0166: <c>jump.continue.undefined-label</c>, ContainsUndefinedContinueTarget of a Script's statements, eval code's too (clause 16.1.1); stage 3.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=788F27
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0166 = new("JSE1607", "jump.continue.undefined-label");

    /// <summary>EE0167: <c>class.private-name.undeclared</c>, AllPrivateIdentifiersValid of a Script's statements with no names (clause 16.1.1); stage 4.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=067FF5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0167 = new("JSE1608", "class.private-name.undeclared");

    /// <summary>EE0170: <c>module.export.duplicate</c>, ExportedNames of a Module's items contains a duplicate entry (clause 16.2.1.1); stage 6.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=941B01
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0170 = new("JSE1611", "module.export.duplicate");

    /// <summary>EE0171: <c>module.export.undeclared</c>, an ExportedBindings element of a Module's items is in neither VarDeclaredNames nor LexicallyDeclaredNames (clause 16.2.1.1); stage 6.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=3B2CFA
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0171 = new("JSE1612", "module.export.undeclared");

    /// <summary>EE0172: <c>super.outside-method</c>, <c>super</c> in a Module's own code (clause 16.2.1.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=120EF9
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0172 = new("JSE1613", "super.outside-method");

    /// <summary>EE0175: <c>jump.break.undefined-label</c>, ContainsUndefinedBreakTarget of a Module's items (clause 16.2.1.1); stage 3, moved here from stage 6.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4580F6
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0175 = new("JSE1616", "jump.break.undefined-label");

    /// <summary>EE0176: <c>jump.continue.undefined-label</c>, ContainsUndefinedContinueTarget of a Module's items (clause 16.2.1.1); stage 3, moved here from stage 6.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=7099A8
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0176 = new("JSE1617", "jump.continue.undefined-label");

    /// <summary>EE0177: <c>class.private-name.undeclared</c>, AllPrivateIdentifiersValid of a Module's items with no names (clause 16.2.1.1); stage 4, moved here from stage 6.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=275C4F
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EE0177 = new("JSE1618", "class.private-name.undeclared");

    /// <summary>EA0003: <c>super.property.outside-method</c>, a SuperProperty in eval code whose caller has no super binding (PerformEval step 11.f, clause 19.2.1.1).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=6652EF
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0003 = new("JSE1903", "super.property.outside-method");

    /// <summary>EA0004: <c>super.call.outside-derived-constructor</c>, a SuperCall in eval code whose caller is not a derived constructor (PerformEval step 11.g).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=82DD8E
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0004 = new("JSE1904", "super.call.outside-derived-constructor");

    /// <summary>EA0005: <c>class.initializer.arguments</c>, <c>arguments</c> in eval code whose caller is a field initialiser (PerformEval step 11.h).</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=1F12F3
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0005 = new("JSE1905", "class.initializer.arguments");

    /// <summary>EA0007: <c>eval.var.lexical-collision</c>, a var-declared name of sloppy eval code that one of its caller's records binds, the record not a <c>Catch</c> clause's, with Annex B on: EvalDeclarationInstantiation step 3.d.i.2.a.i.1, a host that supports Annex B.3.4 (clause 19.2.1.3); reported only where the request gives the caller's records; stage 5c.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=BB4A38
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0007 = new("JSE1907", "eval.var.lexical-collision");

    /// <summary>EA0008: <c>eval.var.lexical-collision</c>, the same name against any of the caller's records, a <c>Catch</c> clause's among them, with Annex B off: step 3.d.i.2.a.ii.1, a host that does not support Annex B.3.4; reported only where the request gives the caller's records; stage 5c.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=811176
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0008 = new("JSE1908", "eval.var.lexical-collision");

    /// <summary>EA0009: <c>class.private-name.undeclared</c>, AllPrivateIdentifiersValid of eval code against the private names its caller sees (EvalDeclarationInstantiation step 7, clause 19.2.1.3); reported only where the request gives them; stage 4.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=A367B5
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EA0009 = new("JSE1909", "class.private-name.undeclared");

    /// <summary>EP0017: <c>super.property.outside-method</c>, a SuperProperty in ShadowRealm's evaluated code (PerformShadowRealmEval step 2.f); no goal reaches it on its own.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=8980CB
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0017 = new("JSE7403", "super.property.outside-method");

    /// <summary>EP0018: <c>super.call.outside-derived-constructor</c>, a SuperCall in ShadowRealm's evaluated code (PerformShadowRealmEval step 2.g); no goal reaches it on its own.</summary>
    // Hairs-AI:           Origin=AI; Spec=catalogue shared codes and message identifiers, stage table semantics rows, JSD-0083 D11; IP=Low; Security=Low; Resources=0; Fingerprint=4CF3AE
    // Hairs-Human:        PENDING
    internal static readonly LexicalRefusal EP0018 = new("JSE7404", "super.call.outside-derived-constructor");
}
