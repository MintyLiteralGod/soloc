namespace SoloRust.Compiler;

public sealed class SoloRustParser
{
    public SoloRustProgram Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var program = new SoloRustProgram();
        var stack = new Stack<(int Indent, object Frame)>();
        stack.Push((-1, program));

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var indent = CountIndent(raw);
            var text = raw.Trim();

            while (stack.Count > 1 && indent <= stack.Peek().Indent)
                stack.Pop();

            var parent = stack.Peek().Frame;

            if (text.StartsWith("//"))
            {
                AddStmt(parent, new SoloRustComment { Text = text[2..].Trim(), Line = lineNo }, lineNo);
                continue;
            }

            if (text.StartsWith("fn ", StringComparison.OrdinalIgnoreCase))
            {
                if (parent is not SoloRustProgram)
                    throw new SoloRustException($"line {lineNo}: nested functions are not supported yet");
                var fn = ParseFn(text, lineNo);
                program.Items.Add(fn);
                stack.Push((indent, fn));
                continue;
            }

            if (text.Equals("else", StringComparison.OrdinalIgnoreCase) || text.Equals("else:", StringComparison.OrdinalIgnoreCase))
            {
                var iff = FindLastIf(parent, lineNo);
                stack.Push((indent, iff.ElseBody));
                continue;
            }

            var stmt = ParseStmt(text, lineNo);
            AddStmt(parent, stmt, lineNo);

            switch (stmt)
            {
                case SoloRustIf iff: stack.Push((indent, iff.ThenBody)); break;
                case SoloRustFor fr: stack.Push((indent, fr.Body)); break;
                case SoloRustWhile wh: stack.Push((indent, wh.Body)); break;
            }
        }

        if (program.Items.Count == 0)
            throw new SoloRustException("SoloRUST needs at least one `fn` (usually `fn main`)");

        return program;
    }

    private static SoloRustFn ParseFn(string text, int line)
    {
        // fn main()  OR  fn add(a: i32, b: i32) -> i32
        var rest = StripColon(text[3..].Trim());
        string name;
        var parameters = new List<(string, string)>();
        string? ret = null;

        var arrow = rest.IndexOf("->", StringComparison.Ordinal);
        if (arrow >= 0)
        {
            ret = rest[(arrow + 2)..].Trim();
            rest = rest[..arrow].Trim();
        }

        var paren = rest.IndexOf('(');
        if (paren < 0)
        {
            name = rest.Trim();
        }
        else
        {
            name = rest[..paren].Trim();
            var end = rest.LastIndexOf(')');
            if (end < 0)
                throw new SoloRustException($"line {line}: missing ) in fn");
            var inside = rest[(paren + 1)..end].Trim();
            if (!string.IsNullOrWhiteSpace(inside))
            {
                foreach (var part in inside.Split(','))
                {
                    var p = part.Trim();
                    if (p.Contains(':'))
                    {
                        var bits = p.Split(':', 2);
                        parameters.Add((bits[0].Trim(), bits[1].Trim()));
                    }
                    else
                    {
                        // default type i32 for ease
                        parameters.Add((p, "i32"));
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(name))
            throw new SoloRustException($"line {line}: fn needs a name");

        var fn = new SoloRustFn { Name = name, ReturnType = ret, Line = line };
        fn.Params.AddRange(parameters);
        return fn;
    }

    private static SoloRustStmt ParseStmt(string text, int line)
    {
        if (text.StartsWith("let mut ", StringComparison.OrdinalIgnoreCase))
            return ParseLet(text["let mut ".Length..].Trim(), mutable: true, line);
        if (text.StartsWith("let ", StringComparison.OrdinalIgnoreCase))
            return ParseLet(text[4..].Trim(), mutable: false, line);
        if (text.StartsWith("var ", StringComparison.OrdinalIgnoreCase))
            return ParseLet(text[4..].Trim(), mutable: true, line);

        if (text.StartsWith("println ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("println(", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("print ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("print(", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePrintln(text, line);
        }

        if (text.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
            return new SoloRustIf { Condition = StripColon(text[3..].Trim()), Line = line };

        if (text.StartsWith("while ", StringComparison.OrdinalIgnoreCase))
            return new SoloRustWhile { Condition = StripColon(text[6..].Trim()), Line = line };

        if (text.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
            return ParseFor(text, line);

        if (text.StartsWith("return", StringComparison.OrdinalIgnoreCase))
        {
            var rest = text.Length > 6 ? text[6..].Trim() : "";
            return new SoloRustReturn { Value = string.IsNullOrWhiteSpace(rest) ? null : rest, Line = line };
        }

        return new SoloRustExprStmt { Code = text.TrimEnd(';'), Line = line };
    }

    private static SoloRustLet ParseLet(string rest, bool mutable, int line)
    {
        rest = rest.Trim();
        string name;
        string? type = null;
        string value;

        var eq = rest.IndexOf('=');
        if (eq < 0)
            throw new SoloRustException($"line {line}: let needs `=` value");

        var left = rest[..eq].Trim();
        value = rest[(eq + 1)..].Trim().TrimEnd(';');

        if (left.Contains(':'))
        {
            var bits = left.Split(':', 2);
            name = bits[0].Trim();
            type = bits[1].Trim();
        }
        else name = left;

        if (string.IsNullOrWhiteSpace(name))
            throw new SoloRustException($"line {line}: invalid let");

        return new SoloRustLet { Mutable = mutable, Name = name, Type = type, Value = value, Line = line };
    }

    private static SoloRustPrintln ParsePrintln(string text, int line)
    {
        var isPrintln = text.StartsWith("println", StringComparison.OrdinalIgnoreCase);
        var rest = text[(isPrintln ? "println".Length : "print".Length)..].Trim().TrimStart('(').TrimEnd(')');
        if (string.IsNullOrWhiteSpace(rest))
            rest = "\"\"";

        // Support: println "Hello, {name}"   or  println("Hi {}", name)
        if ((rest.StartsWith('"') || rest.StartsWith('\'')))
        {
            var q = rest[0];
            var end = rest.IndexOf(q, 1);
            if (end < 0)
                throw new SoloRustException($"line {line}: missing quote");
            var template = rest[1..end];
            // Convert {name} → {} and collect args from interpolations
            var args = new List<string>();
            var rustTemplate = System.Text.RegularExpressions.Regex.Replace(template, "\\{([a-zA-Z_][a-zA-Z0-9_]*)\\}", m =>
            {
                args.Add(m.Groups[1].Value);
                return "{}";
            });
            // Also allow leftover , args after string
            var after = rest[(end + 1)..].Trim().TrimStart(',').Trim();
            if (!string.IsNullOrWhiteSpace(after))
                args.AddRange(after.Split(',').Select(a => a.Trim()).Where(a => a.Length > 0));

            return new SoloRustPrintln { Template = rustTemplate, Args = args, Line = line };
        }

        // println(name) → "{}"
        return new SoloRustPrintln { Template = "{}", Args = [rest], Line = line };
    }

    private static SoloRustFor ParseFor(string text, int line)
    {
        var rest = StripColon(text[4..].Trim());
        var inIdx = rest.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (inIdx < 0)
            throw new SoloRustException($"line {line}: use `for i in 0..10`");

        var variable = rest[..inIdx].Trim();
        var range = rest[(inIdx + 4)..].Trim();
        var dots = range.IndexOf("..", StringComparison.Ordinal);
        if (dots < 0)
            throw new SoloRustException($"line {line}: SoloRUST v0.1 ranges use `a..b`");

        return new SoloRustFor
        {
            Variable = variable,
            RangeStart = range[..dots].Trim(),
            RangeEnd = range[(dots + 2)..].Trim(),
            Line = line,
        };
    }

    private static void AddStmt(object parent, SoloRustStmt stmt, int line)
    {
        switch (parent)
        {
            case SoloRustFn fn:
                fn.Body.Add(stmt);
                break;
            case List<SoloRustStmt> list:
                list.Add(stmt);
                break;
            case SoloRustProgram:
                throw new SoloRustException($"line {line}: statements must be inside a `fn`");
            default:
                throw new SoloRustException($"line {line}: cannot place statement here");
        }
    }

    private static SoloRustIf FindLastIf(object parent, int line)
    {
        var list = parent switch
        {
            SoloRustFn fn => fn.Body,
            List<SoloRustStmt> stmts => stmts,
            _ => throw new SoloRustException($"line {line}: `else` without matching `if`"),
        };

        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] is SoloRustIf iff)
                return iff;
        }
        throw new SoloRustException($"line {line}: `else` without matching `if`");
    }

    private static string StripColon(string s) => s.TrimEnd().TrimEnd(':').Trim();

    private static int CountIndent(string line)
    {
        var n = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') n++;
            else if (ch == '\t') n += 2;
            else break;
        }
        return n;
    }
}
