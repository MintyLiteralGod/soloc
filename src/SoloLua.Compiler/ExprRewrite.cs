using System.Text;
using System.Text.RegularExpressions;

namespace SoloLua.Compiler;

/// <summary>
/// Rewrites SoloLua expression sugar into Lua 5.4 expressions.
/// </summary>
public static class ExprRewrite
{
    public static string Rewrite(string? expr)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return expr ?? "";

        var s = expr.Trim();
        s = RewriteInterpolation(s);
        s = RewriteNullish(s);
        s = RewriteOptional(s);
        s = RewriteMethodCalls(s);
        s = RewriteOperators(s);
        return s;
    }

    private static readonly HashSet<string> LuaLibs = new(StringComparer.Ordinal)
    {
        "math", "string", "table", "io", "os", "debug", "package", "coroutine", "utf8", "solo", "bit32",
    };

    /// <summary>
    /// <c>obj.method(</c> → <c>obj:method(</c> so list/class methods get <c>self</c>.
    /// Keeps dots for <c>.new(</c> and stdlib modules (<c>math.floor</c>).
    /// </summary>
    private static string RewriteMethodCalls(string s) =>
        Regex.Replace(s, @"\b([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\(", m =>
        {
            var recv = m.Groups[1].Value;
            var method = m.Groups[2].Value;
            if (method.Equals("new", StringComparison.Ordinal) || LuaLibs.Contains(recv))
                return m.Value;
            return $"{recv}:{method}(";
        });

    public static string RewriteInterpolation(string s)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] is '"' or '\'')
            {
                var q = s[i];
                var start = i;
                i++;
                var content = new StringBuilder();
                while (i < s.Length && s[i] != q)
                {
                    if (s[i] == '\\' && i + 1 < s.Length)
                    {
                        content.Append(s[i]).Append(s[i + 1]);
                        i += 2;
                        continue;
                    }
                    content.Append(s[i]);
                    i++;
                }
                if (i < s.Length) i++; // closing quote

                var inner = content.ToString();
                if (q == '"' && inner.Contains('{'))
                    sb.Append(ExpandInterpolated(inner));
                else
                    sb.Append(q).Append(inner).Append(q);
                continue;
            }

            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }

    private static string ExpandInterpolated(string inner)
    {
        // "hi {name}" → ("hi " .. tostring(name))
        var parts = new List<string>();
        var buf = new StringBuilder();
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '{' && i + 1 < inner.Length && inner[i + 1] != '{')
            {
                if (buf.Length > 0)
                {
                    parts.Add(LuaString(buf.ToString()));
                    buf.Clear();
                }
                i++;
                var expr = new StringBuilder();
                while (i < inner.Length && inner[i] != '}')
                {
                    expr.Append(inner[i]);
                    i++;
                }
                parts.Add($"tostring({RewriteOperators(expr.ToString().Trim())})");
                continue;
            }
            if (inner[i] == '{' && i + 1 < inner.Length && inner[i + 1] == '{')
            {
                buf.Append('{');
                i++;
                continue;
            }
            buf.Append(inner[i]);
        }
        if (buf.Length > 0)
            parts.Add(LuaString(buf.ToString()));

        return parts.Count == 0 ? "\"\"" : "(" + string.Join(" .. ", parts) + ")";
    }

    private static string LuaString(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string RewriteNullish(string s)
    {
        // a ?? b → solo.nullish(a, b)  (simple split on top-level ??)
        while (true)
        {
            var idx = IndexOfTopLevel(s, "??");
            if (idx < 0) return s;
            var left = s[..idx].Trim();
            var right = s[(idx + 2)..].Trim();
            s = $"solo.nullish({left}, {right})";
        }
    }

    private static string RewriteOptional(string s)
    {
        // x?.y → solo.opt(x, "y")
        // x?.[expr] not supported yet
        return Regex.Replace(s, @"([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\?\.([A-Za-z_][A-Za-z0-9_]*)",
            m => $"solo.opt({m.Groups[1].Value}, \"{m.Groups[2].Value}\")");
    }

    private static string RewriteOperators(string s)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] is '"' or '\'')
            {
                var q = s[i];
                sb.Append(q);
                i++;
                while (i < s.Length && s[i] != q)
                {
                    if (s[i] == '\\' && i + 1 < s.Length)
                    {
                        sb.Append(s[i]).Append(s[i + 1]);
                        i += 2;
                        continue;
                    }
                    sb.Append(s[i]);
                    i++;
                }
                if (i < s.Length)
                {
                    sb.Append(s[i]);
                    i++;
                }
                continue;
            }

            // != → ~=
            if (i + 1 < s.Length && s[i] == '!' && s[i + 1] == '=')
            {
                sb.Append("~=");
                i += 2;
                continue;
            }

            // && → and
            if (i + 1 < s.Length && s[i] == '&' && s[i + 1] == '&')
            {
                sb.Append(" and ");
                i += 2;
                continue;
            }

            // || → or
            if (i + 1 < s.Length && s[i] == '|' && s[i + 1] == '|')
            {
                sb.Append(" or ");
                i += 2;
                continue;
            }

            // !x → not x (when not part of !=)
            if (s[i] == '!' && (i + 1 >= s.Length || s[i + 1] != '='))
            {
                sb.Append(" not ");
                i++;
                continue;
            }

            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }

    private static int IndexOfTopLevel(string s, string token)
    {
        var depth = 0;
        for (var i = 0; i <= s.Length - token.Length; i++)
        {
            var ch = s[i];
            if (ch is '"' or '\'')
            {
                var q = ch;
                i++;
                while (i < s.Length && s[i] != q)
                {
                    if (s[i] == '\\' && i + 1 < s.Length) i++;
                    i++;
                }
                continue;
            }
            if (ch is '(' or '[' or '{') depth++;
            else if (ch is ')' or ']' or '}') depth = Math.Max(0, depth - 1);
            else if (depth == 0 && s.AsSpan(i, token.Length).SequenceEqual(token))
                return i;
        }
        return -1;
    }
}
