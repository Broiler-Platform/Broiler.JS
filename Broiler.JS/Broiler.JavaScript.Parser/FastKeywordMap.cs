using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Parser;

/// <summary>
/// Allocation-free classifier for ECMAScript and parser-contextual keywords.
/// </summary>
public class FastKeywordMap
{
    public static FastKeywordMap Instance { get; } = new();

    protected FastKeywordMap() { }

    public virtual bool IsKeyword(in StringSpan key, out FastKeywords keyword)
    {
        keyword = key.AsSpan() switch
        {
            "break" => FastKeywords.@break,
            "do" => FastKeywords.@do,
            "instanceof" => FastKeywords.instanceof,
            "typeof" => FastKeywords.@typeof,
            "case" => FastKeywords.@case,
            "else" => FastKeywords.@else,
            "new" => FastKeywords.@new,
            "var" => FastKeywords.@var,
            "catch" => FastKeywords.@catch,
            "finally" => FastKeywords.@finally,
            "return" => FastKeywords.@return,
            "void" => FastKeywords.@void,
            "continue" => FastKeywords.@continue,
            "for" => FastKeywords.@for,
            "switch" => FastKeywords.@switch,
            "while" => FastKeywords.@while,
            "debugger" => FastKeywords.@debugger,
            "function" => FastKeywords.@function,
            "this" => FastKeywords.@this,
            "with" => FastKeywords.@with,
            "default" => FastKeywords.@default,
            "if" => FastKeywords.@if,
            "throw" => FastKeywords.@throw,
            "delete" => FastKeywords.@delete,
            "in" => FastKeywords.@in,
            "try" => FastKeywords.@try,
            "class" => FastKeywords.@class,
            "enum" => FastKeywords.@enum,
            "extends" => FastKeywords.@extends,
            "super" => FastKeywords.@super,
            "const" => FastKeywords.@const,
            "export" => FastKeywords.@export,
            "import" => FastKeywords.@import,
            "implements" => FastKeywords.@implements,
            "let" => FastKeywords.@let,
            "private" => FastKeywords.@private,
            "public" => FastKeywords.@public,
            "interface" => FastKeywords.@interface,
            "package" => FastKeywords.@package,
            "protected" => FastKeywords.@protected,
            "static" => FastKeywords.@static,
            "yield" => FastKeywords.@yield,
            "async" => FastKeywords.@async,
            "using" => FastKeywords.@using,
            "await" => FastKeywords.@await,
            "null" => FastKeywords.@null,
            "true" => FastKeywords.@true,
            "false" => FastKeywords.@false,
            "get" => FastKeywords.get,
            "set" => FastKeywords.set,
            "of" => FastKeywords.of,
            "constructor" => FastKeywords.constructor,
            "from" => FastKeywords.from,
            "as" => FastKeywords.@as,
            "accessor" => FastKeywords.accessor,
            _ => FastKeywords.none
        };

        return keyword != FastKeywords.none;
    }
}
