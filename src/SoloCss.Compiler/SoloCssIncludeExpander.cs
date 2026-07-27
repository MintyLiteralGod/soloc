using System.Text;

namespace SoloCss.Compiler;

/// <summary>
/// Resolves <c>include path.solocss</c> / <c>import path.solocss</c> lines by splicing source.
/// </summary>
public static class SoloCssIncludeExpander
{
    public static string Expand(string source, string? basePath, HashSet<string>? stack = null)
    {
        basePath ??= Directory.GetCurrentDirectory();
        stack ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();
            if (TryParseInclude(trimmed, out var path))
            {
                var full = Path.GetFullPath(Path.Combine(basePath, path));
                if (!stack.Add(full))
                    throw new SoloCssException($"line {i + 1}: cyclic include: {path}");
                if (!File.Exists(full))
                    throw new SoloCssException($"line {i + 1}: include not found: {path}");

                var nested = File.ReadAllText(full);
                var expanded = Expand(nested, Path.GetDirectoryName(full)!, stack);
                sb.AppendLine(expanded.TrimEnd());
                stack.Remove(full);
                continue;
            }

            sb.AppendLine(raw);
        }

        return sb.ToString();
    }

    private static bool TryParseInclude(string trimmed, out string path)
    {
        path = "";
        foreach (var prefix in new[] { "include ", "import " })
        {
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            var rest = trimmed[prefix.Length..].Trim().TrimEnd(';');
            if (rest.StartsWith('@'))
                continue; // leave @import url(...) alone for CSS passthrough later
            path = Unquote(rest);
            return path.Length > 0;
        }
        return false;
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];
        return value;
    }
}
