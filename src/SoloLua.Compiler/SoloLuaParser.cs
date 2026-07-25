using System.Text.RegularExpressions;

namespace SoloLua.Compiler;

public sealed class SoloLuaParser
{
    public SoloLuaProgram Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var program = new SoloLuaProgram();
        var stack = new Stack<(int Indent, List<SoloLuaNode> Body)>();
        stack.Push((-1, program.Body));

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var indent = CountIndent(raw);
            var text = raw.Trim();

            if (text.StartsWith("//") || text.StartsWith("--"))
            {
                while (stack.Count > 1 && indent <= stack.Peek().Indent)
                    stack.Pop();
                var comment = text.StartsWith("//") ? text[2..].Trim() : text[2..].Trim();
                stack.Peek().Body.Add(new SoloLuaComment { Text = comment, Line = lineNo });
                continue;
            }

            while (stack.Count > 1 && indent <= stack.Peek().Indent)
                stack.Pop();

            var body = stack.Peek().Body;

            if (text.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("else:", StringComparison.OrdinalIgnoreCase))
            {
                var iff = FindElseTarget(body, lineNo);
                stack.Push((indent, iff.ElseBody));
                continue;
            }

            if (text.StartsWith("elif ", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("else if ", StringComparison.OrdinalIgnoreCase))
            {
                var cond = text.StartsWith("elif ", StringComparison.OrdinalIgnoreCase)
                    ? StripColon(text[5..].Trim())
                    : StripColon(text["else if ".Length..].Trim());
                var parentIf = FindElseTarget(body, lineNo);
                var nested = new SoloLuaIf { Condition = cond, Line = lineNo };
                parentIf.ElseBody.Add(nested);
                stack.Push((indent, nested.ThenBody));
                continue;
            }

            if (text.StartsWith("catch", StringComparison.OrdinalIgnoreCase))
            {
                var tryNode = FindLastTry(body, lineNo);
                var rest = text.Length > 5 ? text[5..].Trim().TrimStart(':').Trim() : "";
                if (!string.IsNullOrWhiteSpace(rest))
                    tryNode.CatchName = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                stack.Push((indent, tryNode.CatchBody));
                continue;
            }

            // map literal body entries: key: value (deeper indent under `name = map`)
            if (LooksLikeMapEntry(text) && FindOpenMap(body) is { } map)
            {
                var (key, value) = ParseMapEntry(text, lineNo);
                map.Entries.Add((key, value));
                continue;
            }

            var node = ParseStatement(text, lineNo);
            body.Add(node);

            switch (node)
            {
                case SoloLuaFn fn: stack.Push((indent, fn.Body)); break;
                case SoloLuaClass cls: stack.Push((indent, cls.Body)); break;
                case SoloLuaIf iff: stack.Push((indent, iff.ThenBody)); break;
                case SoloLuaWhile wh: stack.Push((indent, wh.Body)); break;
                case SoloLuaForRange fr: stack.Push((indent, fr.Body)); break;
                case SoloLuaForEach fe: stack.Push((indent, fe.Body)); break;
                case SoloLuaTry tr: stack.Push((indent, tr.TryBody)); break;
                case SoloLuaMapLiteral: break; // entries may follow at deeper indent OR same? we use same indent children via next lines with deeper indent
            }

            // For map: children at greater indent are entries — push a fake frame that still appends to map via special handling
            if (node is SoloLuaMapLiteral)
            {
                // Push indent frame so deeper lines are map entries (handled above when body[^1] is map)
                // Use a dedicated list that we don't use — instead keep map as last in parent and don't push.
                // Deeper indented map entries: when indent increases, stack still has parent body.
                // So map entries need deeper indent and body[^1] is the map — works without push.
            }
        }

        return program;
    }

    private static SoloLuaNode ParseStatement(string text, int line)
    {
        text = StripColon(text);

        if (text is "continue")
            return new SoloLuaContinue { Line = line };
        if (text is "break")
            return new SoloLuaBreak { Line = line };

        if (text.StartsWith("import ", StringComparison.OrdinalIgnoreCase))
            return ParseImport(text, line);

        if (text.StartsWith("class ", StringComparison.OrdinalIgnoreCase))
        {
            var name = text[6..].Trim();
            if (!IsIdent(name))
                throw new SoloLuaException($"line {line}: class needs a name");
            return new SoloLuaClass { Name = name, Line = line };
        }

        if (text.StartsWith("fn ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("def ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("method ", StringComparison.OrdinalIgnoreCase))
        {
            var isMethod = text.StartsWith("method ", StringComparison.OrdinalIgnoreCase);
            var rest = text[(text.IndexOf(' ') + 1)..].Trim();
            return ParseFn(rest, line, isMethod);
        }

        if (text.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
            return new SoloLuaIf { Condition = text[3..].Trim(), Line = line };

        if (text.StartsWith("while ", StringComparison.OrdinalIgnoreCase))
            return new SoloLuaWhile { Condition = text[6..].Trim(), Line = line };

        if (text.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
            return ParseFor(text[4..].Trim(), line);

        if (text.Equals("try", StringComparison.OrdinalIgnoreCase))
            return new SoloLuaTry { Line = line };

        if (text.StartsWith("print ", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("print", StringComparison.OrdinalIgnoreCase))
        {
            var args = text.Equals("print", StringComparison.OrdinalIgnoreCase) ? "" : text[6..].Trim();
            return new SoloLuaPrint { Args = args, Line = line };
        }

        if (text.StartsWith("return", StringComparison.OrdinalIgnoreCase))
        {
            var value = text.Length > 6 ? text[6..].Trim() : null;
            return new SoloLuaReturn { Value = string.IsNullOrWhiteSpace(value) ? null : value, Line = line };
        }

        // global name = ...
        var global = false;
        var work = text;
        if (work.StartsWith("global ", StringComparison.OrdinalIgnoreCase))
        {
            global = true;
            work = work[7..].Trim();
        }

        // name = list a, b, c
        var listMatch = Regex.Match(work, @"^([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*=\s*list\b(.*)$", RegexOptions.IgnoreCase);
        if (listMatch.Success)
        {
            return new SoloLuaListLiteral
            {
                Name = listMatch.Groups[1].Value,
                Items = listMatch.Groups[2].Value.Trim().TrimStart('(').TrimEnd(')'),
                IsGlobal = global,
                Line = line,
            };
        }

        // name = map
        var mapMatch = Regex.Match(work, @"^([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*=\s*map\s*$", RegexOptions.IgnoreCase);
        if (mapMatch.Success)
        {
            return new SoloLuaMapLiteral
            {
                Name = mapMatch.Groups[1].Value,
                IsGlobal = global,
                Line = line,
            };
        }

        // compound / plain assign
        foreach (var op in new[] { "..=", "+=", "-=", "*=", "/=", "=" })
        {
            var idx = FindAssignOp(work, op);
            if (idx < 0) continue;
            var name = work[..idx].Trim();
            var value = work[(idx + op.Length)..].Trim();
            if (!IsAssignTarget(name))
                break;
            return new SoloLuaAssign
            {
                Name = name,
                Value = value,
                Op = op == "=" ? null : op,
                IsGlobal = global,
                Line = line,
            };
        }

        return new SoloLuaExpr { Code = text, Line = line };
    }

    private static SoloLuaNode ParseFn(string rest, int line, bool isMethod)
    {
        var open = rest.IndexOf('(');
        var close = rest.LastIndexOf(')');
        if (open < 0 || close < open)
            throw new SoloLuaException($"line {line}: fn needs name(params)");

        var name = rest[..open].Trim();
        if (!IsIdent(name))
            throw new SoloLuaException($"line {line}: invalid function name '{name}'");

        var paramText = rest[(open + 1)..close].Trim();
        var parameters = string.IsNullOrWhiteSpace(paramText)
            ? new List<string>()
            : paramText.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).Select(StripType).ToList();

        return new SoloLuaFn { Name = name, Params = parameters, IsMethod = isMethod, Line = line };
    }

    private static string StripType(string param)
    {
        // a: number → a
        var colon = param.IndexOf(':');
        return colon > 0 ? param[..colon].Trim() : param;
    }

    private static SoloLuaNode ParseFor(string rest, int line)
    {
        // for i in 1..10
        // for x in items
        // for k, v in person
        var m = Regex.Match(rest, @"^(.+?)\s+in\s+(.+)$", RegexOptions.IgnoreCase);
        if (!m.Success)
            throw new SoloLuaException($"line {line}: for needs `for x in …`");

        var vars = m.Groups[1].Value.Trim();
        var iter = m.Groups[2].Value.Trim();

        var range = Regex.Match(iter, @"^(.+?)\.\.(.+)$");
        if (range.Success && !vars.Contains(','))
        {
            return new SoloLuaForRange
            {
                Variable = vars,
                Start = range.Groups[1].Value.Trim(),
                End = range.Groups[2].Value.Trim(),
                Line = line,
            };
        }

        if (vars.Contains(','))
        {
            var parts = vars.Split(',', 2);
            return new SoloLuaForEach
            {
                Key = parts[0].Trim(),
                Value = parts[1].Trim(),
                Iterable = iter,
                Line = line,
            };
        }

        return new SoloLuaForEach { Value = vars, Iterable = iter, Line = line };
    }

    private static SoloLuaImport ParseImport(string text, int line)
    {
        // import "mod"
        // import "mod" as alias
        var m = Regex.Match(text, @"^import\s+[""']([^""']+)[""'](?:\s+as\s+([A-Za-z_][A-Za-z0-9_]*))?\s*$", RegexOptions.IgnoreCase);
        if (!m.Success)
            throw new SoloLuaException($"line {line}: import needs a string module — try `import \"mod\" as name`");
        return new SoloLuaImport
        {
            Module = m.Groups[1].Value,
            Alias = m.Groups[2].Success ? m.Groups[2].Value : null,
            Line = line,
        };
    }

    private static bool LooksLikeMapEntry(string text)
    {
        var idx = text.IndexOf(':');
        if (idx <= 0) return false;
        var key = text[..idx].Trim();
        return IsIdent(key) || (key.Length >= 2 && key[0] == '"' && key[^1] == '"');
    }

    private static (string Key, string Value) ParseMapEntry(string text, int line)
    {
        var idx = text.IndexOf(':');
        if (idx <= 0)
            throw new SoloLuaException($"line {line}: map entry needs key: value");
        return (text[..idx].Trim(), text[(idx + 1)..].Trim());
    }

    private static int FindAssignOp(string text, string op)
    {
        // avoid matching == when looking for =
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '"' or '\'')
            {
                var q = text[i];
                i++;
                while (i < text.Length && text[i] != q)
                {
                    if (text[i] == '\\' && i + 1 < text.Length) i++;
                    i++;
                }
                continue;
            }

            if (i + op.Length <= text.Length && text.AsSpan(i, op.Length).SequenceEqual(op))
            {
                if (op == "=")
                {
                    // skip ==, ~=, !=, <=, >=
                    var prev = i > 0 ? text[i - 1] : '\0';
                    var next = i + 1 < text.Length ? text[i + 1] : '\0';
                    if (prev is '=' or '!' or '~' or '<' or '>') continue;
                    if (next == '=') continue;
                }
                return i;
            }
        }
        return -1;
    }

    private static bool IsAssignTarget(string name) =>
        Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*(?:\[.+\])*$");

    private static bool IsIdent(string s) => Regex.IsMatch(s, @"^[A-Za-z_][A-Za-z0-9_]*$");

    private static string StripColon(string text) =>
        text.EndsWith(':') ? text[..^1].TrimEnd() : text;

    private static int CountIndent(string raw)
    {
        var spaces = 0;
        foreach (var ch in raw)
        {
            if (ch == ' ') spaces++;
            else if (ch == '\t') spaces += 2;
            else break;
        }
        return spaces / 2;
    }

    private static SoloLuaIf FindElseTarget(List<SoloLuaNode> body, int line)
    {
        for (var i = body.Count - 1; i >= 0; i--)
        {
            if (body[i] is SoloLuaIf iff)
                return iff;
        }
        throw new SoloLuaException($"line {line}: `else` without matching `if`");
    }

    private static SoloLuaTry FindLastTry(List<SoloLuaNode> body, int line)
    {
        for (var i = body.Count - 1; i >= 0; i--)
        {
            if (body[i] is SoloLuaTry tr)
                return tr;
        }
        throw new SoloLuaException($"line {line}: `catch` without matching `try`");
    }

    private static SoloLuaMapLiteral? FindOpenMap(List<SoloLuaNode> body)
    {
        for (var i = body.Count - 1; i >= 0; i--)
        {
            if (body[i] is SoloLuaMapLiteral map)
                return map;
            if (body[i] is SoloLuaComment)
                continue;
            break;
        }
        return null;
    }
}
