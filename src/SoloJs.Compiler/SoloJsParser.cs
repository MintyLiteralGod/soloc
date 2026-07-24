namespace SoloJs.Compiler;

public sealed class SoloJsParser
{
    public SoloJsProgram Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var program = new SoloJsProgram();
        var stack = new Stack<(int Indent, List<SoloJsNode> Body)>();
        stack.Push((-1, program.Body));

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var indent = CountIndent(raw);
            var text = raw.Trim();

            if (text.StartsWith("//"))
            {
                // Keep comments only at current indent level without popping oddly
                while (stack.Count > 1 && indent < stack.Peek().Indent)
                    stack.Pop();
                // comments at same indent as a block header stay in parent — use <= for non-block
                while (stack.Count > 1 && indent <= stack.Peek().Indent)
                    stack.Pop();
                stack.Peek().Body.Add(new SoloJsComment { Text = text[2..].Trim(), Line = lineNo });
                continue;
            }

            while (stack.Count > 1 && indent <= stack.Peek().Indent)
                stack.Pop();

            var body = stack.Peek().Body;

            // else / elif fold into previous if
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
                var nested = new SoloJsIf { Condition = cond, Line = lineNo };
                parentIf.ElseBody.Add(nested);
                stack.Push((indent, nested.ThenBody));
                continue;
            }

            var node = ParseStatement(text, lineNo);
            body.Add(node);

            switch (node)
            {
                case SoloJsFn fn: stack.Push((indent, fn.Body)); break;
                case SoloJsIf iff: stack.Push((indent, iff.ThenBody)); break;
                case SoloJsWhile wh: stack.Push((indent, wh.Body)); break;
                case SoloJsForRange fr: stack.Push((indent, fr.Body)); break;
                case SoloJsForEach fe: stack.Push((indent, fe.Body)); break;
                case SoloJsWhenReady wr: stack.Push((indent, wr.Body)); break;
                case SoloJsOn on: stack.Push((indent, on.Body)); break;
            }
        }

        return program;
    }

    private static SoloJsIf FindLastIf(List<SoloJsNode> body, int line)
    {
        for (var i = body.Count - 1; i >= 0; i--)
        {
            if (body[i] is SoloJsIf iff)
                return iff;
        }
        throw new SoloJsException($"line {line}: `else` without matching `if`");
    }

    /// <summary>Walk an if/elif chain to the deepest if that can still take else/elif.</summary>
    private static SoloJsIf FindElseTarget(List<SoloJsNode> body, int line)
    {
        var iff = FindLastIf(body, line);
        while (iff.ElseBody is [SoloJsIf only])
            iff = only;
        return iff;
    }

    private static SoloJsNode ParseStatement(string text, int line)
    {
        if (text.StartsWith("fn ", StringComparison.OrdinalIgnoreCase))
            return ParseFn(text, line);

        if (text.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
            return new SoloJsIf { Condition = StripColon(text[3..].Trim()), Line = line };

        if (text.StartsWith("while ", StringComparison.OrdinalIgnoreCase))
            return new SoloJsWhile { Condition = StripColon(text[6..].Trim()), Line = line };

        if (text.StartsWith("for ", StringComparison.OrdinalIgnoreCase))
            return ParseFor(text, line);

        if (text is "when ready" or "when ready:" ||
            text.Equals("ready", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("ready:", StringComparison.OrdinalIgnoreCase))
            return new SoloJsWhenReady { Line = line };

        if (text.StartsWith("on ", StringComparison.OrdinalIgnoreCase))
            return ParseOn(text, line);

        if (text.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            return ParseSet(text, line);

        if (text.StartsWith("print", StringComparison.OrdinalIgnoreCase))
        {
            var args = text.Length > 5 ? text[5..].Trim() : "\"\"";
            if (args.StartsWith('(') && args.EndsWith(')'))
                args = args[1..^1].Trim();
            if (string.IsNullOrWhiteSpace(args))
                args = "\"\"";
            return new SoloJsPrint { Args = args, Line = line };
        }

        if (text.StartsWith("return", StringComparison.OrdinalIgnoreCase))
        {
            var rest = text.Length > 6 ? text[6..].Trim() : "";
            return new SoloJsReturn { Value = string.IsNullOrWhiteSpace(rest) ? null : rest, Line = line };
        }

        if (TryParseAssign(text, out var keyword, out var name, out var value))
            return new SoloJsAssign { Keyword = keyword, Name = name, Value = value, Line = line };

        return new SoloJsExpr { Code = text.TrimEnd(';'), Line = line };
    }

    private static SoloJsFn ParseFn(string text, int line)
    {
        var rest = StripColon(text[3..].Trim());
        string name;
        var parameters = new List<string>();

        var paren = rest.IndexOf('(');
        if (paren >= 0)
        {
            name = rest[..paren].Trim();
            var end = rest.IndexOf(')');
            if (end < 0)
                throw new SoloJsException($"line {line}: missing ) in fn");
            var inside = rest[(paren + 1)..end].Trim();
            if (!string.IsNullOrWhiteSpace(inside))
                parameters.AddRange(inside.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0));
        }
        else
        {
            var parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            name = parts[0];
            if (parts.Length > 1)
                parameters.AddRange(parts[1].Split(',').Select(p => p.Trim()).Where(p => p.Length > 0));
        }

        if (string.IsNullOrWhiteSpace(name))
            throw new SoloJsException($"line {line}: fn needs a name");

        return new SoloJsFn { Name = name, Params = parameters, Line = line };
    }

    private static SoloJsNode ParseFor(string text, int line)
    {
        var rest = StripColon(text[4..].Trim());
        var inIdx = rest.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (inIdx < 0)
            throw new SoloJsException($"line {line}: use `for x in 0..10` or `for x in list`");

        var variable = rest[..inIdx].Trim();
        var iterable = rest[(inIdx + 4)..].Trim();
        var rangeIdx = iterable.IndexOf("..", StringComparison.Ordinal);
        if (rangeIdx >= 0)
        {
            return new SoloJsForRange
            {
                Variable = variable,
                Start = iterable[..rangeIdx].Trim(),
                End = iterable[(rangeIdx + 2)..].Trim(),
                Line = line,
            };
        }

        return new SoloJsForEach { Variable = variable, Iterable = iterable, Line = line };
    }

    private static SoloJsOn ParseOn(string text, int line)
    {
        var rest = StripColon(text[3..].Trim());
        string selector;
        string eventName;

        if (rest.StartsWith('"') || rest.StartsWith('\''))
        {
            var (sel, after) = ReadQuoted(rest, line);
            selector = sel;
            eventName = after.Trim();
        }
        else
        {
            var parts = SplitParts(rest);
            if (parts.Length < 2)
                throw new SoloJsException($"line {line}: use `on click \"#btn\"`");

            if (parts[1].StartsWith('"') || parts[1].StartsWith('\'') || parts[1].StartsWith('#') || parts[1].StartsWith('.'))
            {
                eventName = parts[0];
                selector = Unquote(parts[1]);
            }
            else
            {
                selector = Unquote(parts[0]);
                eventName = parts[1];
            }
        }

        if (string.IsNullOrWhiteSpace(selector) || string.IsNullOrWhiteSpace(eventName))
            throw new SoloJsException($"line {line}: on needs event and selector");

        return new SoloJsOn { Selector = selector, EventName = eventName, Line = line };
    }

    private static SoloJsSet ParseSet(string text, int line)
    {
        var rest = text[4..].Trim();
        string selector;
        string after;
        if (rest.StartsWith('"') || rest.StartsWith('\''))
        {
            (selector, after) = ReadQuoted(rest, line);
            after = after.Trim();
        }
        else
        {
            var sp = rest.IndexOf(' ');
            if (sp < 0)
                throw new SoloJsException($"line {line}: use `set \"#id\" text value`");
            selector = Unquote(rest[..sp]);
            after = rest[(sp + 1)..].Trim();
        }

        foreach (var prop in new[] { "text", "html", "value", "class" })
        {
            if (after.StartsWith(prop, StringComparison.OrdinalIgnoreCase))
            {
                var value = after[prop.Length..].Trim().TrimStart('=').Trim();
                if (string.IsNullOrWhiteSpace(value))
                    throw new SoloJsException($"line {line}: set needs a value");
                return new SoloJsSet { Selector = selector, Property = prop, Value = value, Line = line };
            }
        }

        throw new SoloJsException($"line {line}: set property must be text, html, value, or class");
    }

    private static bool TryParseAssign(string text, out string? keyword, out string name, out string value)
    {
        keyword = null;
        name = "";
        value = "";
        var rest = text;
        foreach (var kw in new[] { "const ", "let ", "var " })
        {
            if (rest.StartsWith(kw, StringComparison.OrdinalIgnoreCase))
            {
                keyword = kw.Trim();
                rest = rest[kw.Length..].Trim();
                break;
            }
        }

        var eq = rest.IndexOf('=');
        if (eq <= 0)
            return false;
        if (eq + 1 < rest.Length && rest[eq + 1] == '=')
            return false;
        if (eq > 0 && (rest[eq - 1] is '!' or '<' or '>' or '='))
            return false;

        name = rest[..eq].Trim();
        value = rest[(eq + 1)..].Trim().TrimEnd(';');
        if (string.IsNullOrWhiteSpace(name) || name.Contains(' ') || name.Contains('('))
            return false;

        return true;
    }

    private static string StripColon(string s) => s.TrimEnd().TrimEnd(':').Trim();

    private static string Unquote(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }

    private static (string Quoted, string After) ReadQuoted(string text, int line)
    {
        var q = text[0];
        var end = text.IndexOf(q, 1);
        if (end < 0)
            throw new SoloJsException($"line {line}: missing closing quote");
        return (text[1..end], text[(end + 1)..]);
    }

    private static string[] SplitParts(string text)
    {
        var parts = new List<string>();
        var cur = new System.Text.StringBuilder();
        var inQ = '\0';
        foreach (var ch in text)
        {
            if (inQ != '\0')
            {
                cur.Append(ch);
                if (ch == inQ) inQ = '\0';
                continue;
            }
            if (ch is '"' or '\'')
            {
                inQ = ch;
                cur.Append(ch);
                continue;
            }
            if (ch == ' ')
            {
                if (cur.Length > 0)
                {
                    parts.Add(cur.ToString());
                    cur.Clear();
                }
                continue;
            }
            cur.Append(ch);
        }
        if (cur.Length > 0)
            parts.Add(cur.ToString());
        return parts.ToArray();
    }

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
