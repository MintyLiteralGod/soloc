namespace SoloJs.Compiler;

public sealed class SoloJsParser
{
    public SoloJsProgram Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var program = new SoloJsProgram();
        var stack = new Stack<(int Indent, List<SoloJsNode> Body, bool ElementMode)>();
        stack.Push((-1, program.Body, false));

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
                while (stack.Count > 1 && indent <= stack.Peek().Indent)
                    stack.Pop();
                stack.Peek().Body.Add(new SoloJsComment { Text = text[2..].Trim(), Line = lineNo });
                continue;
            }

            while (stack.Count > 1 && indent <= stack.Peek().Indent)
                stack.Pop();

            var (frameIndent, body, elementMode) = stack.Peek();

            if (text.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("else:", StringComparison.OrdinalIgnoreCase))
            {
                var iff = FindElseTarget(body, lineNo);
                stack.Push((indent, iff.ElseBody, false));
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
                stack.Push((indent, nested.ThenBody, false));
                continue;
            }

            if (text.Equals("catch", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("catch:", StringComparison.OrdinalIgnoreCase))
            {
                var fetch = FindLastFetch(body, lineNo);
                stack.Push((indent, fetch.CatchBody, false));
                continue;
            }

            if (elementMode)
            {
                var el = ParseElement(text, lineNo);
                body.Add(el);
                stack.Push((indent, el.Children, true));
                continue;
            }

            var node = ParseStatement(text, lineNo, program);
            body.Add(node);

            switch (node)
            {
                case SoloJsFn fn: stack.Push((indent, fn.Body, false)); break;
                case SoloJsIf iff: stack.Push((indent, iff.ThenBody, false)); break;
                case SoloJsWhile wh: stack.Push((indent, wh.Body, false)); break;
                case SoloJsForRange fr: stack.Push((indent, fr.Body, false)); break;
                case SoloJsForEach fe: stack.Push((indent, fe.Body, false)); break;
                case SoloJsWhenReady wr: stack.Push((indent, wr.Body, false)); break;
                case SoloJsOn on: stack.Push((indent, on.Body, false)); break;
                case SoloJsFetch fetch: stack.Push((indent, fetch.ThenBody, false)); break;
                case SoloJsAfter after: stack.Push((indent, after.Body, false)); break;
                case SoloJsEvery every: stack.Push((indent, every.Body, false)); break;
                case SoloJsFrame frame: stack.Push((indent, frame.Body, false)); break;
                case SoloJsWhenVisible vis: stack.Push((indent, vis.Body, false)); break;
                case SoloJsComponent comp:
                    program.UsesReact = true;
                    stack.Push((indent, comp.Body, false));
                    break;
                case SoloJsRender render:
                    program.UsesReact = true;
                    stack.Push((indent, render.Children, true));
                    break;
                case SoloJsMount:
                    program.UsesReact = true;
                    break;
                case SoloJsReactEnable:
                    program.UsesReact = true;
                    break;
            }
        }

        if (program.Body.Any(n => n is SoloJsComponent or SoloJsMount or SoloJsReactEnable))
            program.UsesReact = true;

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

    private static SoloJsIf FindElseTarget(List<SoloJsNode> body, int line)
    {
        var iff = FindLastIf(body, line);
        while (iff.ElseBody is [SoloJsIf only])
            iff = only;
        return iff;
    }

    private static SoloJsFetch FindLastFetch(List<SoloJsNode> body, int line)
    {
        for (var i = body.Count - 1; i >= 0; i--)
        {
            if (body[i] is SoloJsFetch fetch)
                return fetch;
        }
        throw new SoloJsException($"line {line}: `catch` without matching `fetch`");
    }

    private static SoloJsNode ParseStatement(string text, int line, SoloJsProgram program)
    {
        if (text is "react" or "react:" || text.Equals("use react", StringComparison.OrdinalIgnoreCase))
            return new SoloJsReactEnable { Line = line };

        if (text.StartsWith("component ", StringComparison.OrdinalIgnoreCase))
        {
            var componentName = StripColon(text["component ".Length..].Trim());
            if (string.IsNullOrWhiteSpace(componentName))
                throw new SoloJsException($"line {line}: component needs a name");
            return new SoloJsComponent { Name = componentName, Line = line };
        }

        if (text.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = text[6..].Trim();
            var eq = rest.IndexOf('=');
            if (eq <= 0)
                throw new SoloJsException($"line {line}: use `state count = 0`");
            return new SoloJsState
            {
                Name = rest[..eq].Trim(),
                Value = rest[(eq + 1)..].Trim(),
                Line = line,
            };
        }

        if (text is "render" or "render:" or "view" or "view:")
            return new SoloJsRender { Line = line };

        if (text.StartsWith("mount ", StringComparison.OrdinalIgnoreCase))
            return ParseMount(text, line);

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

        if (text.StartsWith("fetch ", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("fetch", StringComparison.OrdinalIgnoreCase))
            return ParseFetch(text, line);

        if (text.StartsWith("after ", StringComparison.OrdinalIgnoreCase))
            return ParseAfter(text, line);

        if (text.StartsWith("every ", StringComparison.OrdinalIgnoreCase))
            return ParseEvery(text, line);

        if (text.StartsWith("on ", StringComparison.OrdinalIgnoreCase))
            return ParseOn(text, line);

        if (text.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            return ParseSet(text, line);

        if (text.StartsWith("addClass ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("removeClass ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("toggleClass ", StringComparison.OrdinalIgnoreCase))
            return ParseClassOp(text, line);

        if (text.StartsWith("focus ", StringComparison.OrdinalIgnoreCase))
            return ParseFocus(text, line);

        if (text is "preventDefault" or "preventDefault:" or "prevent default")
            return new SoloJsPreventDefault { Line = line };

        if (text is "stopPropagation" or "stopPropagation:" or "stop propagation")
            return new SoloJsStopPropagation { Line = line };

        if (text is "frame" or "frame:" or "raf" or "raf:" ||
            text.Equals("requestAnimationFrame", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("requestAnimationFrame:", StringComparison.OrdinalIgnoreCase))
            return new SoloJsFrame { Line = line };

        if (text.StartsWith("canvas ", StringComparison.OrdinalIgnoreCase))
            return ParseCanvas(text, line);

        if (text.StartsWith("when visible ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("whenVisible ", StringComparison.OrdinalIgnoreCase))
        {
            var sel = text.StartsWith("when visible ", StringComparison.OrdinalIgnoreCase)
                ? Unquote(StripColon(text["when visible ".Length..].Trim()))
                : Unquote(StripColon(text["whenVisible ".Length..].Trim()));
            return new SoloJsWhenVisible { Selector = sel, Line = line };
        }

        if (text.StartsWith("clipboard ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("copy ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = text.StartsWith("copy ", StringComparison.OrdinalIgnoreCase)
                ? text[5..].Trim()
                : text["clipboard ".Length..].Trim();
            return new SoloJsClipboard { Value = rest, Line = line };
        }

        if (text.StartsWith("formData ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("formdata ", StringComparison.OrdinalIgnoreCase))
            return ParseFormData(text, line);

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

    private static SoloJsMount ParseMount(string text, int line)
    {
        // mount Counter into "#root"
        // mount Counter into #root
        // mount Counter "#root"
        var rest = StripColon(text[6..].Trim());
        var intoIdx = rest.IndexOf(" into ", StringComparison.OrdinalIgnoreCase);
        string component;
        string selector;
        if (intoIdx >= 0)
        {
            component = rest[..intoIdx].Trim();
            selector = Unquote(rest[(intoIdx + 6)..].Trim());
        }
        else
        {
            var parts = SplitParts(rest);
            if (parts.Length < 2)
                throw new SoloJsException($"line {line}: use `mount Counter into \"#root\"`");
            component = parts[0];
            selector = Unquote(parts[1]);
        }

        if (string.IsNullOrWhiteSpace(component) || string.IsNullOrWhiteSpace(selector))
            throw new SoloJsException($"line {line}: mount needs a component and selector");

        // Optional props: mount Counter into "#root" name="Gem"
        var mount = new SoloJsMount { Component = component.Split(' ')[0], Selector = selector, Line = line };
        return mount;
    }

    private static SoloJsElement ParseElement(string text, int line)
    {
        // Examples:
        // div
        // div.class "hi"
        // button onClick=bump "+1"
        // h1 {count}
        // img src=logo.png
        // Hello name="Gem"   (component reference — Capital tag)
        var parts = SplitParts(text);
        if (parts.Length == 0)
            throw new SoloJsException($"line {line}: empty element");

        var tagToken = parts[0];
        var tag = tagToken;
        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? textLiteral = null;
        string? textExpr = null;

        // .class / #id on tag token: div.card#main
        if (tag.Contains('.') || tag.Contains('#'))
        {
            var (clean, classNames, id) = SplitTagToken(tag);
            tag = clean;
            if (classNames.Count > 0)
                props["className"] = string.Join(" ", classNames);
            if (id is not null)
                props["id"] = id;
        }

        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.StartsWith('{') && p.EndsWith('}'))
            {
                textExpr = p[1..^1].Trim();
                continue;
            }

            if ((p.StartsWith('"') && p.EndsWith('"')) || (p.StartsWith('\'') && p.EndsWith('\'')))
            {
                textLiteral = p[1..^1];
                continue;
            }

            if (p.StartsWith('.'))
            {
                var cls = p[1..];
                props["className"] = props.TryGetValue("className", out var existing)
                    ? existing + " " + cls
                    : cls;
                continue;
            }

            if (p.StartsWith('#'))
            {
                props["id"] = p[1..];
                continue;
            }

            var eq = p.IndexOf('=');
            if (eq > 0)
            {
                var key = p[..eq];
                var val = Unquote(p[(eq + 1)..]);
                props[NormalizeProp(key)] = val;
                continue;
            }

            // Remaining bare words are plain text (not boolean flags).
            var textBits = parts.Skip(i).Where(x => !x.Contains('=') && !x.StartsWith('.') && !x.StartsWith('#') && !x.StartsWith('{'));
            textLiteral = string.Join(" ", textBits.Select(Unquote));
            break;
        }

        return new SoloJsElement
        {
            Tag = tag,
            Text = textLiteral,
            TextExpr = textExpr,
            Line = line,
            Props = props,
        };
    }

    private static (string Tag, List<string> Classes, string? Id) SplitTagToken(string token)
    {
        var classes = new List<string>();
        string? id = null;
        var tag = token;
        var hash = token.IndexOf('#');
        if (hash >= 0)
        {
            var after = token[(hash + 1)..];
            var dot = after.IndexOf('.');
            if (dot >= 0)
            {
                id = after[..dot];
                after = after[dot..];
            }
            else
            {
                id = after;
                after = "";
            }
            tag = token[..hash] + after;
        }

        var bits = tag.Split('.', StringSplitOptions.RemoveEmptyEntries);
        tag = bits[0];
        for (var i = 1; i < bits.Length; i++)
            classes.Add(bits[i]);
        return (tag, classes, id);
    }

    private static string NormalizeProp(string key) =>
        key switch
        {
            "class" => "className",
            "onclick" => "onClick",
            "onchange" => "onChange",
            "onsubmit" => "onSubmit",
            "oninput" => "onInput",
            "for" => "htmlFor",
            _ => key,
        };

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

    private static SoloJsFetch ParseFetch(string text, int line)
    {
        var rest = StripColon(text.Length > 5 ? text[5..].Trim() : "");
        if (string.IsNullOrWhiteSpace(rest))
            throw new SoloJsException($"line {line}: fetch needs a URL");

        string url;
        string? into = null;
        var intoIdx = rest.IndexOf(" into ", StringComparison.OrdinalIgnoreCase);
        string optionsPart;
        if (intoIdx >= 0)
        {
            // URL may include options before into: fetch "u" method=POST into x
            var before = rest[..intoIdx].Trim();
            into = rest[(intoIdx + 6)..].Trim();
            var parts = SplitFetchHead(before);
            url = parts.Url;
            optionsPart = parts.Options;
        }
        else
        {
            var parts = SplitFetchHead(rest);
            url = parts.Url;
            optionsPart = parts.Options;
        }

        var fetch = new SoloJsFetch { Url = url, Into = into, Line = line };
        ApplyFetchOptions(fetch, optionsPart, line);
        return fetch;
    }

    private static (string Url, string Options) SplitFetchHead(string text)
    {
        text = text.Trim();
        if (text.StartsWith('"') || text.StartsWith('\''))
        {
            var (url, after) = ReadQuoted(text, 1);
            return (url, after.Trim());
        }

        var sp = text.IndexOf(' ');
        if (sp < 0)
            return (Unquote(text), "");
        return (Unquote(text[..sp]), text[(sp + 1)..].Trim());
    }

    private static void ApplyFetchOptions(SoloJsFetch fetch, string options, int line)
    {
        if (string.IsNullOrWhiteSpace(options))
            return;

        foreach (var token in TokenizeOptions(options))
        {
            var eq = token.IndexOf('=');
            if (eq <= 0)
            {
                if (token.Equals("json", StringComparison.OrdinalIgnoreCase))
                    fetch.AsJson = true;
                continue;
            }

            var key = token[..eq].Trim();
            var value = Unquote(token[(eq + 1)..].Trim());
            switch (key.ToLowerInvariant())
            {
                case "method": fetch.Method = value; break;
                case "body": fetch.Body = value; break;
                case "mode": fetch.Mode = value; break;
                case "headers": fetch.Headers = value; break;
                case "as" when value.Equals("json", StringComparison.OrdinalIgnoreCase):
                    fetch.AsJson = true;
                    break;
                default:
                    throw new SoloJsException($"line {line}: unknown fetch option `{key}`");
            }
        }
    }

    private static List<string> TokenizeOptions(string options)
    {
        var list = new List<string>();
        var i = 0;
        while (i < options.Length)
        {
            while (i < options.Length && char.IsWhiteSpace(options[i])) i++;
            if (i >= options.Length) break;
            var start = i;
            if (options[i] is '"' or '\'')
            {
                var q = options[i++];
                while (i < options.Length && options[i] != q) i++;
                if (i < options.Length) i++;
                // this was a bare quoted token — skip
                continue;
            }

            while (i < options.Length && !char.IsWhiteSpace(options[i]))
            {
                if (options[i] == '=' && i + 1 < options.Length && options[i + 1] is '"' or '\'')
                {
                    i++; // =
                    var q = options[i++];
                    while (i < options.Length && options[i] != q)
                    {
                        if (options[i] == '\\' && i + 1 < options.Length) i++;
                        i++;
                    }
                    if (i < options.Length) i++;
                    break;
                }
                i++;
            }
            list.Add(options[start..i]);
        }
        return list;
    }

    private static SoloJsFormData ParseFormData(string text, int line)
    {
        // formData "#form" into payload
        var rest = StripColon(text[text.IndexOf(' ')..].Trim());
        var intoIdx = rest.IndexOf(" into ", StringComparison.OrdinalIgnoreCase);
        if (intoIdx < 0)
            throw new SoloJsException($"line {line}: use `formData \"#form\" into payload`");
        var selector = Unquote(rest[..intoIdx].Trim());
        var into = rest[(intoIdx + 6)..].Trim();
        return new SoloJsFormData { Selector = selector, Into = into, Line = line };
    }

    private static SoloJsAfter ParseAfter(string text, int line)
    {
        var rest = StripColon(text[5..].Trim());
        if (string.IsNullOrWhiteSpace(rest))
            throw new SoloJsException($"line {line}: after needs a delay — try `after 500`");
        rest = rest.TrimEnd('m', 's', 'M', 'S').Trim();
        return new SoloJsAfter { DelayMs = rest, Line = line };
    }

    private static SoloJsEvery ParseEvery(string text, int line)
    {
        var rest = StripColon(text[5..].Trim());
        if (string.IsNullOrWhiteSpace(rest))
            throw new SoloJsException($"line {line}: every needs an interval — try `every 1000`");
        rest = rest.TrimEnd('m', 's', 'M', 'S').Trim();
        return new SoloJsEvery { IntervalMs = rest, Line = line };
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
                throw new SoloJsException($"line {line}: use `on click \"#btn\"` or `on scroll window`");

            if (parts[1].StartsWith('"') || parts[1].StartsWith('\'') || parts[1].StartsWith('#') || parts[1].StartsWith('.')
                || parts[1] is "window" or "document" or "body")
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

        // dataset.foo / style.color / aria-expanded
        foreach (var prop in new[]
                 {
                     "text", "html", "value", "class", "style", "focus",
                     "dataset", "href", "src", "disabled", "checked", "hidden",
                 })
        {
            if (after.StartsWith(prop, StringComparison.OrdinalIgnoreCase))
            {
                var value = after[prop.Length..].Trim().TrimStart('=').Trim();
                // style.color red  OR  dataset.open true
                if ((prop is "style" or "dataset") && value.Length > 0 && !value.StartsWith('{'))
                {
                    var sp = value.IndexOf(' ');
                    if (sp > 0)
                    {
                        var sub = value[..sp].Trim().TrimStart('.');
                        var restVal = value[(sp + 1)..].Trim().TrimStart('=').Trim();
                        return new SoloJsSet
                        {
                            Selector = selector,
                            Property = prop + "." + sub,
                            Value = restVal,
                            Line = line,
                        };
                    }
                }

                if (string.IsNullOrWhiteSpace(value) && prop is not "focus")
                    throw new SoloJsException($"line {line}: set needs a value");
                return new SoloJsSet
                {
                    Selector = selector,
                    Property = prop,
                    Value = string.IsNullOrWhiteSpace(value) ? "true" : value,
                    Line = line,
                };
            }
        }

        // generic attr: set "#x" attr aria-expanded true
        if (after.StartsWith("attr ", StringComparison.OrdinalIgnoreCase))
        {
            var restAttr = after[5..].Trim();
            var sp = restAttr.IndexOf(' ');
            if (sp < 0)
                throw new SoloJsException($"line {line}: use `set \"#x\" attr name value`");
            var name = restAttr[..sp].Trim();
            var value = restAttr[(sp + 1)..].Trim();
            return new SoloJsSet { Selector = selector, Property = "attr." + name, Value = value, Line = line };
        }

        throw new SoloJsException($"line {line}: set property must be text, html, value, class, style, dataset, attr, …");
    }

    private static SoloJsClassOp ParseClassOp(string text, int line)
    {
        var op = text.StartsWith("addClass", StringComparison.OrdinalIgnoreCase) ? "add"
            : text.StartsWith("removeClass", StringComparison.OrdinalIgnoreCase) ? "remove"
            : "toggle";
        var rest = text[(text.IndexOf(' ') + 1)..].Trim();
        string selector;
        string className;
        if (rest.StartsWith('"') || rest.StartsWith('\''))
        {
            (selector, var after) = ReadQuoted(rest, line);
            className = Unquote(after.Trim());
        }
        else
        {
            var parts = SplitParts(rest);
            if (parts.Length < 2)
                throw new SoloJsException($"line {line}: use `toggleClass \"#nav\" open`");
            selector = Unquote(parts[0]);
            className = Unquote(parts[1]);
        }
        return new SoloJsClassOp { Op = op, Selector = selector, ClassName = className, Line = line };
    }

    private static SoloJsFocus ParseFocus(string text, int line)
    {
        var rest = Unquote(text[5..].Trim());
        if (string.IsNullOrWhiteSpace(rest))
            throw new SoloJsException($"line {line}: focus needs a selector");
        return new SoloJsFocus { Selector = rest, Line = line };
    }

    private static SoloJsCanvas ParseCanvas(string text, int line)
    {
        // canvas "#c" into ctx
        // canvas "#c"
        var rest = StripColon(text[6..].Trim());
        string selector;
        string? into = null;
        var intoIdx = rest.IndexOf(" into ", StringComparison.OrdinalIgnoreCase);
        if (intoIdx >= 0)
        {
            selector = Unquote(rest[..intoIdx].Trim());
            into = rest[(intoIdx + 6)..].Trim();
        }
        else
        {
            var parts = SplitParts(rest);
            selector = Unquote(parts[0]);
            if (parts.Length >= 3 && parts[1].Equals("into", StringComparison.OrdinalIgnoreCase))
                into = parts[2];
        }
        if (string.IsNullOrWhiteSpace(selector))
            throw new SoloJsException($"line {line}: canvas needs a selector");
        return new SoloJsCanvas { Selector = selector, Into = into, Line = line };
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
        var brace = 0;
        foreach (var ch in text)
        {
            if (inQ != '\0')
            {
                cur.Append(ch);
                if (ch == inQ) inQ = '\0';
                continue;
            }

            if (ch == '{') { brace++; cur.Append(ch); continue; }
            if (ch == '}' && brace > 0) { brace--; cur.Append(ch); continue; }
            if (brace > 0) { cur.Append(ch); continue; }

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
