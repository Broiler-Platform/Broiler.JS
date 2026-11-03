#nullable enable

using System.Globalization;
using Broiler.ECMAScript.Syntax;

namespace Broiler.JavaScript.Parser.Bridge;

/// <summary>
/// This engine's message table (milestone SJL-14.2, decision D46): the V8-style text it renders for each of the
/// shared library's message identifiers, from the identifier and its arguments. The library renders no text of its
/// own (its design's section 3); each engine renders its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kept texts.</b> The texts hosts and this engine's tests read are kept: "Unexpected await", which the shell's
/// script host reads to run a script again with top-level <c>await</c> (<c>Broiler.JavaScript/Program.cs</c>), is
/// what a refusal at an <c>await</c> of a script parsed without that permission renders; "is already defined in
/// current scope", which tests read for a redeclaration, and in a module "has already been declared" and "Duplicate
/// export name", which the compiler's module checks gave; the old parser's texts for a unary operator before
/// <c>**</c> and for an unexpected character with its code point; "Invalid attribute key", the import attribute this engine
/// cannot honour (catalogue row EX0021), which the bridge refuses itself. Every other text differs from the old
/// parser's where the old parser's differs from this table: class M of bundle <c>sjl-14-001</c>'s rule, listed in
/// the generated message table the side-by-side run writes, and stated once in the changelog.
/// </para>
/// <para>
/// A refusal of the grammar keeps the old parser's form, the position after the text ("Unexpected token ... at
/// line, column"), so that a message read for its position still finds one.
/// </para>
/// </remarks>
internal static class BridgeMessages
{
    // The text of each identifier, {0} standing for its first argument; null for one this table does not name.
    private static string? Text(string messageId) => messageId switch
    {
        "syntax.token.unexpected" => "Unexpected token {0}",
        "syntax.token.missing" => "Expected {0}",
        "syntax.character.unexpected" => "Unexpected character {0}",
        "syntax.comment.unterminated" => "Unterminated comment",
        "syntax.string.unterminated" => "Unterminated string literal",
        "syntax.string.escape.invalid" => "Invalid escape sequence in a string literal",
        "syntax.template.unterminated" => "Unterminated template literal",
        "syntax.regexp.unterminated" => "Unterminated regular expression literal",
        "syntax.numeric-literal.invalid" => "Invalid numeric literal",
        "syntax.identifier.escape.invalid" => "Invalid Unicode escape sequence in an identifier",
        "syntax.return.outside-function" => "Illegal return statement",
        "syntax.return.in-static-block" => "Illegal return statement in a class static block",
        "syntax.module-item.in-script" => "Cannot use import or export outside a module",
        "identifier.escape.invalid-start" => "Invalid Unicode escape sequence",
        "identifier.escape.invalid-part" => "Invalid Unicode escape sequence",
        "identifier.reserved-word.escaped" => "Keyword must not contain escaped characters",
        "identifier.yield.in-generator" => "Unexpected yield: 'yield' is a reserved word in a generator",
        "identifier.await.in-async" => "Unexpected await: 'await' is a reserved word in an async function",
        "identifier.await.in-module" => "Unexpected await: 'await' is reserved in module code",
        "strict.legacy-octal-literal" => "Octal literals are not allowed in strict mode",
        "strict.legacy-octal-escape" => "Octal escape sequences are not allowed in strict mode",
        "strict.binding.eval-or-arguments" => "Unexpected eval or arguments in strict mode",
        "strict.reserved-word" => "Unexpected strict mode reserved word",
        "strict.with" => "Strict mode code may not include a with statement",
        "strict.delete.unqualified-or-private" => "Delete of an unqualified identifier in strict mode",
        "strict.labelled-function" => "In strict mode code, functions can only be declared at top level or inside a block",
        "scope.lexical.duplicate" => "{0} is already defined in current scope",
        "scope.lexical.var-collision" => "{0} is already defined in current scope",
        "scope.parameter.duplicate" => "Duplicate parameter name {0} not allowed in this context",
        "scope.parameter.lexical-collision" => "{0} is already defined in current scope",
        "scope.catch.duplicate" => "Duplicate binding {0} in catch parameter",
        "scope.catch.lexical-collision" => "{0} is already defined in current scope",
        "scope.catch.var-collision" => "{0} is already defined in current scope",
        "declaration.const.missing-initializer" => "Missing initializer in const declaration",
        "declaration.let.bound-name" => "let is disallowed as a lexically bound name",
        "assignment.target.invalid" => "Invalid left-hand side in assignment",
        "assignment.target.not-simple" => "Invalid left-hand side expression",
        "assignment.rest-property.nested-pattern" => "`...` must be followed by an assignable reference in assignment contexts",
        "cover.assignment-pattern" => "Invalid destructuring assignment target",
        "cover.parenthesized-expression" => "Invalid parenthesized expression",
        "cover.arrow-formal-parameters" => "Invalid arrow function parameters",
        "cover.async-arrow-head" => "Invalid async arrow function parameters",
        "cover.await-using-declaration-head" => "Invalid await using declaration",
        "expression.shorthand-initializer" => "Invalid shorthand property initializer",
        "expression.optional-chain.tagged-template" => "Tagged template expressions are not permitted in an optional chain",
        "object.proto.duplicate" => "Duplicate __proto__ fields are not allowed in object literals",
        "object.method.private-name" => "Private field '{0}' must be declared in an enclosing class",
        "template.escape.invalid" => "Invalid escape sequence in template",
        "regexp.literal.invalid" => "Invalid regular expression: {0}",
        "jump.label.duplicate" => "Label '{0}' has already been declared",
        "jump.break.undefined-label" => "Undefined label '{0}'",
        "jump.continue.undefined-label" => "Undefined label '{0}'",
        "jump.continue.no-iteration" => "Illegal continue statement: no surrounding iteration statement",
        "jump.break.no-target" => "Illegal break statement",
        "statement.labelled-function" => "Function declarations are not allowed as the body of a loop or with statement",
        "function.use-strict.non-simple-parameters" => "Illegal 'use strict' directive in function with non-simple parameter list",
        "function.parameters.yield" => "Yield expression not allowed in formal parameter",
        "function.parameters.await" => "Illegal await-expression in formal parameters of async function",
        "super.property.outside-method" => "'super' keyword unexpected here",
        "super.call.outside-derived-constructor" => "'super' keyword unexpected here",
        "super.outside-method" => "'super' keyword unexpected here",
        "meta.new-target.outside-function" => "new.target expression is not allowed here",
        "meta.import-meta.outside-module" => "Cannot use 'import.meta' outside a module",
        "class.constructor.duplicate" => "A class may only have one constructor",
        "class.constructor.special-method" => "Class constructor may not be an accessor, a generator or an async method",
        "class.field.constructor" => "Classes may not have a field named 'constructor'",
        "class.static-field.prototype-or-constructor" => "Classes may not have a static property named 'prototype' or 'constructor'",
        "class.static-method.prototype" => "Classes may not have a static property named 'prototype'",
        "class.private-name.duplicate" => "Identifier '{0}' has already been declared",
        "class.private-name.constructor" => "Classes may not have a private field named '#constructor'",
        "class.private-name.undeclared" => "Private field '{0}' must be declared in an enclosing class",
        "class.initializer.arguments" => "'arguments' is not allowed in class field initializer or static initialization block",
        "class.static-block.await" => "Unexpected await in a class static block",
        "module.export.duplicate" => "Duplicate export name '{0}'",
        "module.export.string-local-name" => "A string literal cannot be used as an exported binding without 'from'",
        "module.export.reserved-local-name" => "'{0}' is not a valid exported binding name",
        "module.export-name.ill-formed" => "A module export name must be well-formed Unicode",
        "module.import-attribute.duplicate-key" => "Import attribute has duplicate key '{0}'",
        "using.declaration.no-disposal-scope" => "'using' is not allowed at the top level of a script",
        "annexb.html-comment.module" => "HTML-like comments are not allowed in module code",
        "annexb.if-function.strict" => "In strict mode code, functions can only be declared at top level or inside a block",
        "annexb.for-in-initializer.strict" => "for-in loop variable declaration may not have an initializer",
        "annexb.regexp.unicode-mode" => "Invalid regular expression",
        "eval.source.invalid" => "Unexpected token in eval code",
        "dynamic-function.source.invalid" => "Unexpected token in the Function constructor's source",
        "dynamic-function.parameters.invalid" => "Unexpected token in the Function constructor's parameters",
        "dynamic-function.body.invalid" => "Unexpected token in the Function constructor's body",
        "decorator.not-in-edition" => "Decorators are not supported here",
        "decorator.auto-accessor.not-in-edition" => "Auto-accessors are not supported here",
        "limit.nesting.too-deep" => "The source is nested too deeply to parse (maximum depth {0})",
        "limit.tree.too-deep" => "The source's syntax tree is too deep to parse (maximum depth {0})",
        _ => null,
    };

    /// <summary>
    /// The text this engine gives <paramref name="diagnostic"/>, at the line and column its span begins at, over
    /// <paramref name="text"/> (the parse's text, whose offset 0 is <paramref name="baseOffset"/> in the spans).
    /// </summary>
    internal static string Render(SyntaxDiagnostic diagnostic, string text, int baseOffset, bool scriptWithoutTopLevelAwait, bool module = false)
    {
        var span = diagnostic.Span;
        var position = $"{span.Line.ToString(CultureInfo.InvariantCulture)}, {span.Column.ToString(CultureInfo.InvariantCulture)}";
        var relative = span.Offset - baseOffset;

        // The shell's retry reads this text (decision D46).
        if (scriptWithoutTopLevelAwait && relative >= 0 && StartsWithWord(text, relative, "await"))
            return "Unexpected await";

        var argument = diagnostic.Arguments.Count > 0 ? diagnostic.Arguments[0] : string.Empty;
        if (diagnostic.MessageId == "syntax.token.unexpected" && argument.Length == 0)
            argument = relative >= 0 && relative < text.Length ? text[relative].ToString() : "EOF";

        if (Kept(diagnostic.MessageId, argument, text, relative, module) is { } kept)
            return diagnostic.MessageId.StartsWith("syntax.", System.StringComparison.Ordinal) ? kept + " at " + position : kept;

        var template = Text(diagnostic.MessageId)
            ?? (diagnostic.MessageId.StartsWith("regexp.", System.StringComparison.Ordinal)
                ? "Invalid regular expression: {0}"
                : "Unexpected token {0}");
        var message = template.Replace("{0}", argument, System.StringComparison.Ordinal);
        return diagnostic.MessageId.StartsWith("syntax.", System.StringComparison.Ordinal)
            ? message + " at " + position
            : message;
    }

    // The other kept texts (decision D46), each one a test of this engine reads: a module's second declaration of a
    // name and its second default export, as the compiler's module checks worded them (ModuleDuplicateBindingTests);
    // a unary operator or await before `**`, as the old parser worded it (Issue761Tests); and an unexpected
    // character with its code point, as the old scanner described it (MinifiedSourceTests).
    private static string? Kept(string messageId, string argument, string text, int relative, bool module)
    {
        if (messageId.StartsWith("scope.", System.StringComparison.Ordinal) && argument == "*default*")
            return "Duplicate export name 'default'";
        if (module && messageId is "scope.lexical.duplicate" or "scope.lexical.var-collision")
            return "Identifier '" + argument + "' has already been declared";
        if (messageId == "syntax.token.unexpected" && argument == "**")
            return "Unary operator used immediately before exponentiation expression; parentheses must be used to disambiguate operator precedence";
        if (messageId == "syntax.character.unexpected" && relative >= 0 && relative < text.Length)
            return "Unexpected character " + Describe(text, relative);
        return null;
    }

    // As FastScanner.Unexpected describes a character: a surrogate pair's code point, U+FFFD's likely cause, a control
    // character or a lone surrogate by its code point, and any other character with its code point.
    private static string Describe(string text, int offset)
    {
        var c = text[offset];
        if (char.IsHighSurrogate(c) && offset + 1 < text.Length && char.IsLowSurrogate(text[offset + 1]))
        {
            var pair = char.ConvertToUtf32(c, text[offset + 1]);
            return "'" + char.ConvertFromUtf32(pair) + "' (U+" + pair.ToString("X4", CultureInfo.InvariantCulture) + ")";
        }

        var code = "U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);
        return c == '\uFFFD'
            ? "U+FFFD (the replacement character \u2014 the source is not valid text in the encoding it was decoded with)"
            : char.IsControl(c)
                ? code + " (a control character)"
                : char.IsSurrogate(c)
                    ? code + " (an unpaired surrogate)"
                    : "'" + c + "' (" + code + ")";
    }

    private static bool StartsWithWord(string text, int offset, string word)
    {
        if (offset + word.Length > text.Length || string.CompareOrdinal(text, offset, word, 0, word.Length) != 0)
            return false;
        var after = offset + word.Length;
        return after == text.Length || !(char.IsLetterOrDigit(text[after]) || text[after] is '_' or '$');
    }
}
