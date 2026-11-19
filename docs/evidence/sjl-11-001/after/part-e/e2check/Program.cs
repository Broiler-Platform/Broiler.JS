// e2check: part E's check E-2 of bundle sjl-11-001 (decision-rule.md, part E), read from the after commit's Release
// assemblies by System.Reflection.Metadata. Runs no code of the assemblies it reads.
//
//   dotnet e2check.dll <dir holding Broiler.JavaScript.{Parser,Ast,BuiltIns,Runtime}.dll>
//
// E-2 reads: "no TypeRef or MemberRef into System.Text.RegularExpressions from the Parser and Ast assemblies, from any
// type whose namespace is Broiler.JavaScript.BuiltIns.RegExp, from JSStringPrototype's search, match, matchAll,
// replace, replaceAll and split members, or from LegacyRegExpState".
//
// A reference "into System.Text.RegularExpressions" is read widely here, so that the check can only over-report:
// a TypeRef whose namespace (of its outermost enclosing TypeRef) is System.Text.RegularExpressions or begins with it
// and a dot; a MemberRef whose parent is such a TypeRef, or a TypeSpec or MethodDef whose decoded form names such a
// type, or whose own signature names one; a TypeSpec or MethodSpec whose decoded form names one; and an
// AssemblyRef named System.Text.RegularExpressions. For each assembly the tool lists its AssemblyRefs, every such
// TypeRef and MemberRef in its tables (whoever uses them), and then, per type and per method, every place such a
// reference is used: the type's base type and interfaces, its generic constraints, its fields' signatures, its
// methods' signatures, local signatures and IL operand tokens, its properties' and events' signatures, and the custom
// attributes on the type, its members and their parameters. Compiler-generated members of a scope are attributed to
// it: a nested type of a scope's type is the scope's; for JSStringPrototype's members, any method or nested type of
// JSStringPrototype whose name holds "<Name>" (a lambda, local function, iterator or display class of that member).
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

internal static class Program
{
    private const string Ns = "System.Text.RegularExpressions";
    private static readonly string[] Assemblies =
        ["Broiler.JavaScript.Parser", "Broiler.JavaScript.Ast", "Broiler.JavaScript.BuiltIns", "Broiler.JavaScript.Runtime"];
    private static readonly string[] PatternMembers = ["Search", "Match", "MatchAll", "Replace", "ReplaceAll", "Split"];

    private static int Main(string[] args)
    {
        var dir = args[0];
        var violations = 0;
        var o = new StringBuilder();
        o.Append("# e2check: part E's check E-2 (decision-rule.md, part E), from System.Reflection.Metadata over the Release\n");
        o.Append("# assemblies below; no code of theirs is run. A reference into System.Text.RegularExpressions is read widely\n");
        o.Append("# (see the tool's header), so the check can only over-report.\n\n");
        foreach (var name in Assemblies)
        {
            var path = Path.Combine(dir, name + ".dll");
            var bytes = File.ReadAllBytes(path);
            o.Append($"== {name}.dll sha256 {Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes))}\n");
            using var pe = new PEReader(new MemoryStream(bytes));
            var md = pe.GetMetadataReader();
            var a = new Reader(md, pe);
            o.Append("assembly refs: " + string.Join(", ", md.AssemblyReferences.Select(h => md.GetString(md.GetAssemblyReference(h).Name)).OrderBy(s => s, StringComparer.Ordinal)) + "\n");
            var trefs = md.TypeReferences.Where(a.IsRegexTypeRef).Select(h => a.TypeRefName(h)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            o.Append($"TypeRefs into {Ns}: {trefs.Count}" + (trefs.Count > 0 ? " - " + string.Join(", ", trefs) : "") + "\n");
            var mrefs = md.MemberReferences.Where(h => a.IsRegexMemberRef(h)).Select(h => a.Describe(h)).OrderBy(s => s, StringComparer.Ordinal).ToList();
            o.Append($"MemberRefs into {Ns}: {mrefs.Count}\n");
            foreach (var m in mrefs) o.Append("  " + m + "\n");

            // Every use, per type (nested types under their outermost type's name) and per method.
            var uses = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var th in md.TypeDefinitions)
            {
                var tn = a.TypeDefName(th);
                foreach (var (where, what) in a.UsesOfType(th))
                {
                    if (!uses.TryGetValue(where, out var set)) uses[where] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.Add(what);
                }
            }
            o.Append($"members using such a reference: {uses.Count}\n");
            foreach (var (where, set) in uses)
                o.Append($"  {where}: {string.Join("; ", set)}\n");

            // The scopes E-2 names in this assembly.
            var scoped = new List<string>();
            if (name is "Broiler.JavaScript.Parser" or "Broiler.JavaScript.Ast")
            {
                o.Append("E-2 scope: the whole assembly\n");
                if (trefs.Count > 0 || mrefs.Count > 0 || uses.Count > 0
                    || md.AssemblyReferences.Any(h => md.GetString(md.GetAssemblyReference(h).Name) == Ns))
                    scoped.Add("the assembly references " + Ns);
            }
            if (name == "Broiler.JavaScript.BuiltIns")
            {
                var rx = md.TypeDefinitions.Where(h => a.OuterNamespace(h) == "Broiler.JavaScript.BuiltIns.RegExp").Select(a.TypeDefName).OrderBy(s => s, StringComparer.Ordinal).ToList();
                o.Append($"E-2 scope: the types whose namespace is Broiler.JavaScript.BuiltIns.RegExp, with their nested types: {rx.Count} - {string.Join(", ", rx)}\n");
                foreach (var (where, _) in uses)
                    if (rx.Any(t => where == t || where.StartsWith(t + "::", StringComparison.Ordinal) || where.StartsWith(t + "/", StringComparison.Ordinal)))
                        scoped.Add(where);
                // JSStringPrototype's members are declared in JSStringPrototype*.cs as members of the partial class
                // Broiler.JavaScript.BuiltIns.String.JSString, the type that holds them in metadata.
                var sp = "Broiler.JavaScript.BuiltIns.String.JSString";
                var members = md.TypeDefinitions.Where(h => a.TypeDefName(h) == sp).SelectMany(h => md.GetTypeDefinition(h).GetMethods())
                    .Select(h => md.GetString(md.GetMethodDefinition(h).Name)).Where(n => PatternMembers.Contains(n)).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
                o.Append($"E-2 scope: JSStringPrototype's members, in metadata {sp}'s {string.Join(", ", PatternMembers)} (found: {string.Join(", ", members)}), with any method or nested type of it whose name holds <Member>\n");
                foreach (var (where, _) in uses)
                {
                    if (!where.StartsWith(sp + "::", StringComparison.Ordinal) && !where.StartsWith(sp + "/", StringComparison.Ordinal)) continue;
                    var rest = where[sp.Length..];
                    if (PatternMembers.Any(m => rest.StartsWith("::" + m + "(", StringComparison.Ordinal) || rest.Contains("<" + m + ">", StringComparison.Ordinal)))
                        scoped.Add(where);
                }
            }
            if (name == "Broiler.JavaScript.Runtime")
            {
                var lr = md.TypeDefinitions.Where(h => a.TypeDefName(h).StartsWith("Broiler.JavaScript.LegacyRegExpState", StringComparison.Ordinal) || a.TypeDefName(h).EndsWith(".LegacyRegExpState", StringComparison.Ordinal)).Select(a.TypeDefName).ToList();
                o.Append($"E-2 scope: LegacyRegExpState, with its nested types (found: {string.Join(", ", lr)})\n");
                foreach (var (where, _) in uses)
                    if (lr.Any(t => where == t || where.StartsWith(t + "::", StringComparison.Ordinal) || where.StartsWith(t + "/", StringComparison.Ordinal)))
                        scoped.Add(where);
                if (lr.Count == 0) { o.Append("  LegacyRegExpState NOT FOUND\n"); violations++; }
            }
            o.Append($"E-2 references in scope: {scoped.Count}\n");
            foreach (var s in scoped) o.Append("  IN SCOPE: " + s + "\n");
            violations += scoped.Count;

            if (name == "Broiler.JavaScript.BuiltIns")
            {
                // E-4's support: every call from JSStringPrototype.Search (and what it generates) to a JSRegExp member.
                var sp = "Broiler.JavaScript.BuiltIns.String.JSString";
                o.Append("E-4 support: every token naming a Broiler.JavaScript.BuiltIns.RegExp type that JSStringPrototype's Search (JSString::Search) and its generated code use:\n");
                foreach (var th in md.TypeDefinitions)
                {
                    var tdn = a.TypeDefName(th);
                    if (tdn != sp && !tdn.StartsWith(sp + "/", StringComparison.Ordinal)) continue;
                    foreach (var mh in md.GetTypeDefinition(th).GetMethods())
                    {
                        var mn = a.MethodName(mh);
                        if (!(mn.StartsWith(sp + "::Search(", StringComparison.Ordinal) || mn.Contains("<Search>", StringComparison.Ordinal))) continue;
                        foreach (var t in a.CalledTokens(mh).Where(t => t.Contains("Broiler.JavaScript.BuiltIns.RegExp.", StringComparison.Ordinal)).Distinct().OrderBy(s => s, StringComparer.Ordinal))
                            o.Append($"  {mn} -> {t}\n");
                    }
                }
            }
            o.Append('\n');
        }
        o.Append($"E-2 references in scope, all assemblies: {violations}\n");
        Console.Out.Write(o.ToString());
        return violations == 0 ? 0 : 1;
    }
}

internal sealed class Reader
{
    private const string Ns = "System.Text.RegularExpressions";
    private readonly MetadataReader md;
    private readonly PEReader pe;
    private readonly Provider provider;
    private static readonly Dictionary<short, OpCode> Ops = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => op.Value);

    public Reader(MetadataReader md, PEReader pe) { this.md = md; this.pe = pe; provider = new Provider(this); }

    public string TypeRefName(TypeReferenceHandle h)
    {
        var r = md.GetTypeReference(h);
        var n = md.GetString(r.Name);
        if (r.ResolutionScope.Kind == HandleKind.TypeReference) return TypeRefName((TypeReferenceHandle)r.ResolutionScope) + "/" + n;
        var ns = md.GetString(r.Namespace);
        return ns.Length == 0 ? n : ns + "." + n;
    }

    public string TypeDefName(TypeDefinitionHandle h)
    {
        var d = md.GetTypeDefinition(h);
        var n = md.GetString(d.Name);
        var outer = d.GetDeclaringType();
        if (!outer.IsNil) return TypeDefName(outer) + "/" + n;
        var ns = md.GetString(d.Namespace);
        return ns.Length == 0 ? n : ns + "." + n;
    }

    public string OuterNamespace(TypeDefinitionHandle h)
    {
        var d = md.GetTypeDefinition(h);
        while (!d.GetDeclaringType().IsNil) d = md.GetTypeDefinition(d.GetDeclaringType());
        return md.GetString(d.Namespace);
    }

    public static bool NamesRegex(string s) => s.Contains(Ns + ".", StringComparison.Ordinal);

    public bool IsRegexTypeRef(TypeReferenceHandle h) => NamesRegex(TypeRefName(h) + "");

    public string Type(EntityHandle h) => h.Kind switch
    {
        HandleKind.TypeReference => TypeRefName((TypeReferenceHandle)h),
        HandleKind.TypeDefinition => TypeDefName((TypeDefinitionHandle)h),
        HandleKind.TypeSpecification => md.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(provider, null),
        _ => h.Kind.ToString(),
    };

    public string Describe(MemberReferenceHandle h)
    {
        var r = md.GetMemberReference(h);
        var parent = r.Parent.Kind switch
        {
            HandleKind.MethodDefinition => MethodName((MethodDefinitionHandle)r.Parent),
            HandleKind.ModuleReference => "module " + md.GetString(md.GetModuleReference((ModuleReferenceHandle)r.Parent).Name),
            _ => Type(r.Parent),
        };
        string sig;
        try
        {
            sig = r.GetKind() == MemberReferenceKind.Method
                ? Sig(r.DecodeMethodSignature(provider, null))
                : r.DecodeFieldSignature(provider, null);
        }
        catch (Exception e) { sig = "<" + e.GetType().Name + ">"; }
        return $"{parent}::{md.GetString(r.Name)} {sig}";
    }

    public bool IsRegexMemberRef(MemberReferenceHandle h) => NamesRegex(Describe(h));

    private static string Sig(MethodSignature<string> s) => "(" + string.Join(", ", s.ParameterTypes) + ") : " + s.ReturnType;

    public string MethodName(MethodDefinitionHandle h)
    {
        var d = md.GetMethodDefinition(h);
        string sig;
        try { sig = string.Join(", ", d.DecodeSignature(provider, null).ParameterTypes); } catch { sig = "?"; }
        return $"{TypeDefName(d.GetDeclaringType())}::{md.GetString(d.Name)}({sig})";
    }

    private string FieldName(FieldDefinitionHandle h)
    {
        var d = md.GetFieldDefinition(h);
        return $"{TypeDefName(d.GetDeclaringType())}::{md.GetString(d.Name)}";
    }

    private string Token(int token)
    {
        var h = MetadataTokens.EntityHandle(token);
        try
        {
            return h.Kind switch
            {
                HandleKind.TypeReference or HandleKind.TypeDefinition or HandleKind.TypeSpecification => "type " + Type(h),
                HandleKind.MemberReference => "memberref " + Describe((MemberReferenceHandle)h),
                HandleKind.MethodDefinition => "method " + MethodName((MethodDefinitionHandle)h) + " : " + md.GetMethodDefinition((MethodDefinitionHandle)h).DecodeSignature(provider, null).ReturnType,
                HandleKind.FieldDefinition => "field " + FieldName((FieldDefinitionHandle)h) + " : " + md.GetFieldDefinition((FieldDefinitionHandle)h).DecodeSignature(provider, null),
                HandleKind.MethodSpecification => "methodspec " + MethodSpec((MethodSpecificationHandle)h),
                _ => "",
            };
        }
        catch (Exception e) { return "<" + e.GetType().Name + " " + token.ToString("x8") + ">"; }
    }

    private string MethodSpec(MethodSpecificationHandle h)
    {
        var s = md.GetMethodSpecification(h);
        var m = s.Method.Kind == HandleKind.MemberReference ? Describe((MemberReferenceHandle)s.Method) : MethodName((MethodDefinitionHandle)s.Method);
        return m + "<" + string.Join(", ", s.DecodeSignature(provider, null)) + ">";
    }

    /// <summary>Every token an IL body names (call, newobj, ldtoken, ldfld, ...), described.</summary>
    public IEnumerable<string> CalledTokens(MethodDefinitionHandle mh)
    {
        var d = md.GetMethodDefinition(mh);
        if (d.RelativeVirtualAddress == 0) yield break;
        var body = pe.GetMethodBody(d.RelativeVirtualAddress);
        var il = body.GetILBytes()!;
        var i = 0;
        while (i < il.Length)
        {
            short v = il[i++];
            if (v == 0xFE) v = (short)(0xFE00 | il[i++]);
            var op = Ops[v];
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: i += 1; break;
                case OperandType.InlineVar: i += 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: i += 8; break;
                case OperandType.InlineSwitch: var n = BitConverter.ToInt32(il, i); i += 4 + 4 * n; break;
                case OperandType.InlineBrTarget: case OperandType.InlineI: case OperandType.ShortInlineR: i += 4; break;
                case OperandType.InlineString: case OperandType.InlineSig: i += 4; break;
                case OperandType.InlineField: case OperandType.InlineMethod: case OperandType.InlineTok: case OperandType.InlineType:
                    var t = BitConverter.ToInt32(il, i); i += 4;
                    yield return op.Name + " " + Token(t);
                    break;
                default: throw new InvalidOperationException(op.OperandType.ToString());
            }
        }
    }

    private IEnumerable<string> Attributes(CustomAttributeHandleCollection c)
    {
        foreach (var h in c)
        {
            var ca = md.GetCustomAttribute(h);
            var ctor = ca.Constructor.Kind == HandleKind.MemberReference ? Describe((MemberReferenceHandle)ca.Constructor) : MethodName((MethodDefinitionHandle)ca.Constructor);
            yield return "attribute " + ctor;
        }
    }

    /// <summary>(where, what) for every use of a reference into System.Text.RegularExpressions in a type's own declarations.</summary>
    public IEnumerable<(string, string)> UsesOfType(TypeDefinitionHandle th)
    {
        var t = md.GetTypeDefinition(th);
        var tn = TypeDefName(th);
        var found = new List<(string, string)>();
        void Check(string where, string what) { if (NamesRegex(what)) found.Add((where, what)); }
        if (!t.BaseType.IsNil) Check(tn, "base " + Type(t.BaseType));
        foreach (var ih in t.GetInterfaceImplementations()) Check(tn, "interface " + Type(md.GetInterfaceImplementation(ih).Interface));
        foreach (var gp in t.GetGenericParameters())
            foreach (var ch in md.GetGenericParameter(gp).GetConstraints()) Check(tn, "constraint " + Type(md.GetGenericParameterConstraint(ch).Type));
        foreach (var s in Attributes(t.GetCustomAttributes())) Check(tn, s);
        foreach (var fh in t.GetFields())
        {
            var f = md.GetFieldDefinition(fh);
            Check(FieldName(fh), "field type " + f.DecodeSignature(provider, null));
            foreach (var s in Attributes(f.GetCustomAttributes())) Check(FieldName(fh), s);
        }
        foreach (var ph in t.GetProperties())
        {
            var p = md.GetPropertyDefinition(ph);
            var where = tn + "::" + md.GetString(p.Name) + " (property)";
            Check(where, "property type " + Sig(p.DecodeSignature(provider, null)));
            foreach (var s in Attributes(p.GetCustomAttributes())) Check(where, s);
        }
        foreach (var eh in t.GetEvents())
        {
            var e = md.GetEventDefinition(eh);
            Check(tn + "::" + md.GetString(e.Name) + " (event)", "event type " + Type(e.Type));
        }
        foreach (var mh in t.GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            var where = MethodName(mh);
            Check(where, "signature " + Sig(m.DecodeSignature(provider, null)));
            foreach (var s in Attributes(m.GetCustomAttributes())) Check(where, s);
            foreach (var ph in m.GetParameters())
                foreach (var s in Attributes(md.GetParameter(ph).GetCustomAttributes())) Check(where, s);
            foreach (var gp in m.GetGenericParameters())
                foreach (var ch in md.GetGenericParameter(gp).GetConstraints()) Check(where, "constraint " + Type(md.GetGenericParameterConstraint(ch).Type));
            if (m.RelativeVirtualAddress != 0)
            {
                var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                if (!body.LocalSignature.IsNil)
                {
                    var locals = md.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(provider, null);
                    Check(where, "locals " + string.Join(", ", locals));
                }
                foreach (var s in CalledTokens(mh)) Check(where, s);
            }
        }
        return found;
    }

    private sealed class Provider(Reader r) : ISignatureTypeProvider<string, object?>
    {
        public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
        public string GetByReferenceType(string e) => e + "&";
        public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr" + Sig(s);
        public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(", ", a) + ">";
        public string GetGenericMethodParameter(object? c, int i) => "!!" + i;
        public string GetGenericTypeParameter(object? c, int i) => "!" + i;
        public string GetModifiedType(string m, string u, bool req) => u + (req ? " modreq(" : " modopt(") + m + ")";
        public string GetPinnedType(string e) => e + " pinned";
        public string GetPointerType(string e) => e + "*";
        public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
        public string GetSZArrayType(string e) => e + "[]";
        public string GetTypeFromDefinition(MetadataReader m, TypeDefinitionHandle h, byte k) => r.TypeDefName(h);
        public string GetTypeFromReference(MetadataReader m, TypeReferenceHandle h, byte k) => r.TypeRefName(h);
        public string GetTypeFromSpecification(MetadataReader m, object? c, TypeSpecificationHandle h, byte k) => m.GetTypeSpecification(h).DecodeSignature(this, c);
    }
}
