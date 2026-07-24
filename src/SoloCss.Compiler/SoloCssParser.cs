namespace SoloCss.Compiler;

public sealed class SoloCssParser
{
    public SoloCssDocument Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var doc = new SoloCssDocument();
        var stack = new Stack<(int Indent, object Frame)>();
        stack.Push((-1, doc));

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var indent = CountIndent(raw);
            var text = raw.Trim();
            if (text.StartsWith("//") || text.StartsWith('#'))
                continue;

            while (stack.Count > 1 && indent <= stack.Peek().Indent)
                stack.Pop();

            var parent = stack.Peek().Frame;

            if (IsVarsHeader(text))
            {
                var frame = new VarsFrame();
                AttachVars(parent, frame, lineNo);
                stack.Push((indent, frame));
                continue;
            }

            if (IsMediaHeader(text, out var query))
            {
                var media = new SoloCssMedia { Query = query, Line = lineNo };
                AttachMedia(parent, media, lineNo);
                stack.Push((indent, media));
                continue;
            }

            if (parent is VarsFrame)
            {
                ParseVarLine(doc, text, lineNo);
                continue;
            }

            if (LooksLikeDeclaration(text, parent))
            {
                var (prop, value) = SplitDeclaration(text, lineNo);
                AddDeclaration(parent, prop, value, lineNo);
                continue;
            }

            // Flag-style shortcuts on their own line: bold / center / no-underline / flex
            if (parent is SoloCssRule && PropertyMap.IsKnown(text))
            {
                AddDeclaration(parent, text, "", lineNo);
                continue;
            }

            var rule = new SoloCssRule { Selector = text, Line = lineNo };
            AttachRule(parent, rule, lineNo);
            stack.Push((indent, rule));
        }

        return doc;
    }

    private static bool IsVarsHeader(string text) =>
        text is "vars" or "theme" or "variables" ||
        text.Equals("vars:", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("theme:", StringComparison.OrdinalIgnoreCase);

    private static bool IsMediaHeader(string text, out string query)
    {
        query = "";
        if (text.StartsWith("@media", StringComparison.OrdinalIgnoreCase))
        {
            query = text["@media".Length..].Trim().TrimEnd('{').Trim();
            if (string.IsNullOrWhiteSpace(query))
                query = "all";
            return true;
        }

        if (text.StartsWith("media ", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("media", StringComparison.OrdinalIgnoreCase))
        {
            query = text.Length > 5 ? text[5..].Trim() : "all";
            // Allow: media max-width 640px  →  (max-width: 640px)
            if (!query.Contains('(') && query.Contains(' '))
            {
                var parts = query.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                    query = $"({parts[0]}: {parts[1]})";
            }
            return true;
        }

        return false;
    }

    private static bool LooksLikeDeclaration(string text, object parent)
    {
        if (parent is not SoloCssRule)
            return false;
        // Selectors / parent refs are never declarations
        if (text.StartsWith('&') || text.StartsWith('.') || text.StartsWith('#') ||
            text.StartsWith('*') || text.StartsWith(':') || text.StartsWith('['))
            return false;
        // property value  OR  property: value
        if (text.Contains(':'))
        {
            var idx = text.IndexOf(':');
            var head = text[..idx].Trim();
            // reject selector-looking heads
            if (head.Length == 0 || head.StartsWith('&'))
                return false;
            return true;
        }
        var sp = text.IndexOf(' ');
        if (sp <= 0)
            return false;
        var name = text[..sp];
        if (name.Contains('>') || name.Contains('+') || name.Contains('~'))
            return false;
        return PropertyMap.IsKnown(name) || char.IsLetter(name[0]);
    }

    private static (string Prop, string Value) SplitDeclaration(string text, int line)
    {
        if (text.Contains(':'))
        {
            var idx = text.IndexOf(':');
            var prop = text[..idx].Trim();
            var value = text[(idx + 1)..].Trim().TrimEnd(';');
            if (string.IsNullOrWhiteSpace(prop) || string.IsNullOrWhiteSpace(value))
                throw new SoloCssException($"line {line}: expected `property: value`");
            return (prop, value);
        }

        var sp = text.IndexOf(' ');
        if (sp < 0)
            throw new SoloCssException($"line {line}: expected `property value`");
        return (text[..sp].Trim(), text[(sp + 1)..].Trim().TrimEnd(';'));
    }

    private static void ParseVarLine(SoloCssDocument doc, string text, int line)
    {
        string name;
        string value;
        if (text.Contains('='))
        {
            var parts = text.Split('=', 2);
            name = parts[0].Trim().TrimStart('$');
            value = parts[1].Trim();
        }
        else if (text.Contains(':'))
        {
            var idx = text.IndexOf(':');
            name = text[..idx].Trim().TrimStart('$');
            value = text[(idx + 1)..].Trim();
        }
        else
        {
            var sp = text.IndexOf(' ');
            if (sp < 0)
                throw new SoloCssException($"line {line}: var needs a value — try `brand #0f2a22`");
            name = text[..sp].Trim().TrimStart('$');
            value = text[(sp + 1)..].Trim();
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            throw new SoloCssException($"line {line}: invalid variable");
        doc.Vars[name] = value;
    }

    private static void AttachVars(object parent, VarsFrame frame, int line)
    {
        if (parent is not SoloCssDocument)
            throw new SoloCssException($"line {line}: `vars` / `theme` must be at the top level");
        // frame is only a stack marker; vars go on the document
        _ = frame;
    }

    private static void AttachMedia(object parent, SoloCssMedia media, int line)
    {
        if (parent is SoloCssDocument doc)
            doc.Blocks.Add(media);
        else
            throw new SoloCssException($"line {line}: media queries must be top-level");
    }

    private static void AttachRule(object parent, SoloCssRule rule, int line)
    {
        switch (parent)
        {
            case SoloCssDocument doc:
                doc.Blocks.Add(rule);
                break;
            case SoloCssMedia media:
                media.Rules.Add(rule);
                break;
            case SoloCssRule parentRule:
                parentRule.Children.Add(rule);
                break;
            default:
                throw new SoloCssException($"line {line}: cannot nest rule here");
        }
    }

    private static void AddDeclaration(object parent, string prop, string value, int line)
    {
        if (parent is SoloCssRule rule)
        {
            rule.Declarations.Add((prop, value));
            return;
        }

        throw new SoloCssException($"line {line}: properties must be inside a rule");
    }

    private static int CountIndent(string line)
    {
        var n = 0;
        foreach (var ch in line)
        {
            if (ch == ' ')
                n++;
            else if (ch == '\t')
                n += 2;
            else
                break;
        }
        return n;
    }

    private sealed class VarsFrame;
}

internal static class PropertyMap
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["size"] = "font-size",
        ["font"] = "font-family",
        ["weight"] = "font-weight",
        ["pad"] = "padding",
        ["padding"] = "padding",
        ["margin"] = "margin",
        ["gap"] = "gap",
        ["bg"] = "background",
        ["background"] = "background",
        ["color"] = "color",
        ["radius"] = "border-radius",
        ["border"] = "border",
        ["width"] = "width",
        ["height"] = "height",
        ["max-width"] = "max-width",
        ["min-height"] = "min-height",
        ["display"] = "display",
        ["flex"] = "display",
        ["grid"] = "display",
        ["align"] = "align-items",
        ["justify"] = "justify-content",
        ["direction"] = "flex-direction",
        ["wrap"] = "flex-wrap",
        ["shadow"] = "box-shadow",
        ["opacity"] = "opacity",
        ["cursor"] = "cursor",
        ["letter"] = "letter-spacing",
        ["line"] = "line-height",
        ["text"] = "text-align",
        ["decoration"] = "text-decoration",
        ["transform"] = "transform",
        ["transition"] = "transition",
        ["position"] = "position",
        ["top"] = "top",
        ["left"] = "left",
        ["right"] = "right",
        ["bottom"] = "bottom",
        ["z"] = "z-index",
        ["overflow"] = "overflow",
        ["grid-template"] = "grid-template-columns",
        ["columns"] = "grid-template-columns",
        ["no-underline"] = "text-decoration",
        ["bold"] = "font-weight",
        ["center"] = "text-align",
        ["muted"] = "opacity",
    };

    private static readonly HashSet<string> Known = new(Map.Keys, StringComparer.OrdinalIgnoreCase);

    public static bool IsKnown(string name) => Known.Contains(name);

    public static (string CssName, string? ForcedValue) Expand(string name, string value)
    {
        var key = name.Trim();
        if (key.Equals("flex", StringComparison.OrdinalIgnoreCase) && value is "1" or "row" or "column" or "true" or "")
            return ("display", "flex");
        if (key.Equals("grid", StringComparison.OrdinalIgnoreCase))
            return ("display", "grid");
        if (key.Equals("no-underline", StringComparison.OrdinalIgnoreCase))
            return ("text-decoration", "none");
        if (key.Equals("bold", StringComparison.OrdinalIgnoreCase))
            return ("font-weight", value is "true" or "1" or "" ? "700" : value);
        if (key.Equals("center", StringComparison.OrdinalIgnoreCase))
            return ("text-align", "center");
        if (key.Equals("muted", StringComparison.OrdinalIgnoreCase))
            return ("opacity", value is "true" or "1" or "" ? "0.72" : value);
        if (key.Equals("flex", StringComparison.OrdinalIgnoreCase))
            return ("display", "flex");

        if (Map.TryGetValue(key, out var css))
            return (css, null);
        return (key, null);
    }
}
