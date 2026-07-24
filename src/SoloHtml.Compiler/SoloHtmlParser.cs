using System.Text;

namespace SoloHtml.Compiler;

/// <summary>
/// SoloHTML — SoloGem's easiest markup language. Indentation-based, compiles to HTML5.
/// </summary>
public sealed class SoloHtmlParser
{
    public SoloHtmlDocument Parse(string source)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var items = new List<LineItem>();

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmedStart = raw.TrimStart();
            if (trimmedStart.StartsWith("//", StringComparison.Ordinal) || trimmedStart.StartsWith("#", StringComparison.Ordinal))
                continue;

            var indent = CountIndent(raw);
            var parsed = ParseLine(trimmedStart.TrimEnd(), i + 1);
            items.Add(new LineItem(indent, parsed, i + 1));
        }

        return new SoloHtmlDocument(BuildForest(items));
    }

    private static int CountIndent(string line)
    {
        var spaces = 0;
        foreach (var ch in line)
        {
            if (ch == ' ')
                spaces++;
            else if (ch == '\t')
                spaces += 2;
            else
                break;
        }

        return spaces / 2;
    }

    private static ParsedLine ParseLine(string line, int number)
    {
        // tag [attrs...] [text...]
        // attrs: key=value | key="value" | .class | #id | bare flag
        var parts = Tokenize(line);
        if (parts.Count == 0)
            throw new SoloHtmlException($"Line {number}: empty tag.");

        var tag = parts[0].ToLowerInvariant();
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var textParts = new List<string>();
        var inText = false;

        for (var i = 1; i < parts.Count; i++)
        {
            var part = parts[i];
            if (!inText)
            {
                if (LooksLikeSelector(part))
                {
                    ApplySelectors(attrs, part);
                    continue;
                }

                var eq = part.IndexOf('=');
                if (eq > 0)
                {
                    var key = part[..eq];
                    var value = Unquote(part[(eq + 1)..]);
                    attrs[key] = value;
                    continue;
                }

                // bare flag like "primary"
                if (IsFlag(part))
                {
                    AppendClass(attrs, part);
                    continue;
                }

                inText = true;
            }

            textParts.Add(Unquote(part));
        }

        var text = textParts.Count == 0 ? null : string.Join(' ', textParts);
        return new ParsedLine(tag, text, attrs);
    }

    private static bool IsFlag(string part) =>
        part is "primary" or "secondary" or "ghost" or "wide" or "center" or "muted" or "hero";

    private static bool LooksLikeSelector(string part) =>
        (part.StartsWith('.') || part.StartsWith('#'))
        && !part.Contains('=');

    private static void ApplySelectors(Dictionary<string, string> attrs, string token)
    {
        // Supports #id.class.other or .class#id
        var i = 0;
        while (i < token.Length)
        {
            var marker = token[i];
            if (marker is not ('.' or '#'))
                break;

            i++;
            var start = i;
            while (i < token.Length && token[i] is not ('.' or '#'))
                i++;

            if (i == start)
                continue;

            var value = token[start..i];
            if (marker == '#')
                attrs["id"] = value;
            else
                AppendClass(attrs, value);
        }
    }

    private static void AppendClass(Dictionary<string, string> attrs, string className)
    {
        if (attrs.TryGetValue("class", out var existing))
            attrs["class"] = existing + " " + className;
        else
            attrs["class"] = className;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];
        return value;
    }

    private static List<string> Tokenize(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        char quote = '\0';

        void Flush()
        {
            if (sb.Length == 0)
                return;
            result.Add(sb.ToString());
            sb.Clear();
        }

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == quote)
                {
                    inQuotes = false;
                    sb.Append(ch);
                    continue;
                }

                sb.Append(ch);
                continue;
            }

            if (ch is '"' or '\'')
            {
                inQuotes = true;
                quote = ch;
                sb.Append(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                Flush();
                continue;
            }

            sb.Append(ch);
        }

        Flush();
        return result;
    }

    private static List<SoloHtmlNode> BuildForest(List<LineItem> items)
    {
        var roots = new List<SoloHtmlNode>();
        var stack = new Stack<(int Indent, List<SoloHtmlNode> Children)>();
        stack.Push((-1, roots));

        foreach (var item in items)
        {
            while (stack.Count > 1 && item.Indent <= stack.Peek().Indent)
                stack.Pop();

            var nodeChildren = new List<SoloHtmlNode>();
            var node = new SoloHtmlNode(item.Parsed.Tag, item.Parsed.Text, item.Parsed.Attrs, nodeChildren, item.Line);
            stack.Peek().Children.Add(node);
            stack.Push((item.Indent, nodeChildren));
        }

        return roots;
    }

    private readonly record struct LineItem(int Indent, ParsedLine Parsed, int Line);
    private sealed record ParsedLine(string Tag, string? Text, Dictionary<string, string> Attrs);
}

public sealed class SoloHtmlException : Exception
{
    public SoloHtmlException(string message) : base(message)
    {
    }
}
