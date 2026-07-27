using System.Net;
using System.Text;

namespace SoloHtml.Compiler;

public sealed class HtmlEmitter
{
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
    };

    private readonly bool _forceDefaultTheme;
    private readonly bool? _includeDefaultTheme;

    public HtmlEmitter(bool? includeDefaultTheme = null, bool forceDefaultTheme = false)
    {
        _includeDefaultTheme = includeDefaultTheme;
        _forceDefaultTheme = forceDefaultTheme;
    }

    public string Emit(SoloHtmlDocument document, string? pageTitle = null)
    {
        var sb = new StringBuilder();
        var roots = document.Roots;
        var hasPage = roots.Any(r => r.Tag is "page" or "html");

        if (hasPage)
        {
            foreach (var root in roots)
                EmitNode(sb, root, 0, pageTitle);
        }
        else
        {
            // Wrap fragments in a minimal page shell for preview friendliness.
            var useTheme = ShouldIncludeDefaultTheme(roots);
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"utf-8\" />");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine($"  <title>{WebUtility.HtmlEncode(pageTitle ?? "SoloHTML")}</title>");
            if (useTheme)
            {
                sb.AppendLine("  <style>");
                sb.AppendLine(DefaultCss);
                sb.AppendLine("  </style>");
            }
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            foreach (var root in roots)
                EmitNode(sb, root, 1, pageTitle);
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
        }

        return sb.ToString();
    }

    private bool ShouldIncludeDefaultTheme(IReadOnlyList<SoloHtmlNode> roots)
    {
        if (_forceDefaultTheme)
            return true;
        if (_includeDefaultTheme is { } forced)
            return forced;

        foreach (var root in roots)
        {
            if (WantsNoDefaultTheme(root))
                return false;
            if (root.Children.Any(c => c.Tag is "css" or "stylesheet"))
                return false;
        }

        return true;
    }

    private static bool WantsNoDefaultTheme(SoloHtmlNode node) =>
        (node.Attributes.TryGetValue("theme", out var theme) &&
         theme.Equals("none", StringComparison.OrdinalIgnoreCase))
        || node.Attributes.ContainsKey("notheme")
        || node.Attributes.ContainsKey("bare");

    private void EmitNode(StringBuilder sb, SoloHtmlNode node, int depth, string? pageTitle)
    {
        var pad = new string(' ', depth * 2);
        var mapped = MapTag(node);

        if (node.Tag is "page")
        {
            EmitPage(sb, node, pageTitle);
            return;
        }

        if (node.Tag is "slot" or "content" or "head" or "layout")
            return;

        if (node.Tag is "style")
        {
            sb.Append(pad).AppendLine("<style>");
            if (!string.IsNullOrWhiteSpace(node.Text))
                sb.Append(pad).Append("  ").AppendLine(node.Text);
            foreach (var child in node.Children)
            {
                // style children treated as raw text lines via tag+text
                if (!string.IsNullOrWhiteSpace(child.Text))
                    sb.Append(pad).Append("  ").AppendLine($"{child.Tag} {child.Text}".Trim());
                else
                    sb.Append(pad).Append("  ").AppendLine(child.Tag);
            }

            sb.Append(pad).AppendLine("</style>");
            return;
        }

        if (node.Tag is "text" or "t")
        {
            sb.Append(pad).AppendLine(WebUtility.HtmlEncode(node.Text ?? string.Empty));
            return;
        }

        if (VoidTags.Contains(mapped.Tag))
        {
            sb.Append(pad).Append('<').Append(mapped.Tag);
            WriteAttrs(sb, mapped.Attributes);
            sb.AppendLine(" />");
            return;
        }

        sb.Append(pad).Append('<').Append(mapped.Tag);
        WriteAttrs(sb, mapped.Attributes);
        sb.Append('>');

        var hasChildren = node.Children.Count > 0;
        if (!hasChildren && string.IsNullOrEmpty(node.Text))
        {
            sb.Append("</").Append(mapped.Tag).AppendLine(">");
            return;
        }

        if (!hasChildren)
        {
            sb.Append(WebUtility.HtmlEncode(node.Text ?? string.Empty));
            sb.Append("</").Append(mapped.Tag).AppendLine(">");
            return;
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(node.Text))
            sb.Append(pad).Append("  ").AppendLine(WebUtility.HtmlEncode(node.Text));

        foreach (var child in node.Children)
            EmitNode(sb, child, depth + 1, pageTitle);

        sb.Append(pad).Append("</").Append(mapped.Tag).AppendLine(">");
    }

    private static readonly HashSet<string> HeadOnlyTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "meta", "style", "css", "stylesheet", "link", "head",
        "favicon", "icon", "apple-touch-icon", "canonical", "og", "twitter", "jsonld", "ld+json",
    };

    private void EmitPage(StringBuilder sb, SoloHtmlNode page, string? pageTitle)
    {
        var title = page.Text ?? pageTitle ?? "SoloHTML";
        var titleNode = page.Children.FirstOrDefault(c => c.Tag == "title")
            ?? page.Children.Where(c => c.Tag == "head").SelectMany(c => c.Children).FirstOrDefault(c => c.Tag == "title");
        if (titleNode?.Text is { } t)
            title = t;

        var lang = page.Attributes.TryGetValue("lang", out var langAttr) ? langAttr : "en";

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"{WebUtility.HtmlEncode(lang)}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"  <title>{WebUtility.HtmlEncode(title)}</title>");

        foreach (var node in EnumerateHeadNodes(page))
            EmitHeadNode(sb, node, "  ");

        var useTheme = ShouldIncludeDefaultTheme(new[] { page });
        var customStyle = page.Children.FirstOrDefault(c => c.Tag == "style")
            ?? page.Children.Where(c => c.Tag == "head").SelectMany(c => c.Children).FirstOrDefault(c => c.Tag == "style");
        if (useTheme || customStyle is not null)
        {
            sb.AppendLine("  <style>");
            if (useTheme)
                sb.AppendLine(DefaultCss);
            if (customStyle is not null)
            {
                if (!string.IsNullOrWhiteSpace(customStyle.Text))
                    sb.AppendLine(customStyle.Text);
                foreach (var child in customStyle.Children)
                    sb.AppendLine(string.IsNullOrWhiteSpace(child.Text) ? child.Tag : $"{child.Tag} {child.Text}");
            }

            sb.AppendLine("  </style>");
        }

        foreach (var css in page.Children.Where(c => c.Tag is "css" or "stylesheet")
                     .Concat(page.Children.Where(c => c.Tag == "head").SelectMany(c => c.Children)
                         .Where(c => c.Tag is "css" or "stylesheet")))
            EmitStylesheetLink(sb, css, "  ");

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var child in page.Children)
        {
            if (HeadOnlyTags.Contains(child.Tag))
                continue;
            if ((child.Tag is "js" or "script") && HasSrc(child))
                continue;
            EmitNode(sb, child, 1, title);
        }

        foreach (var js in page.Children.Where(c => (c.Tag is "js" or "script") && HasSrc(c)))
            EmitScriptTag(sb, js, "  ");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }

    private static IEnumerable<SoloHtmlNode> EnumerateHeadNodes(SoloHtmlNode page)
    {
        foreach (var child in page.Children)
        {
            if (child.Tag == "head")
            {
                foreach (var nested in child.Children)
                    yield return nested;
                continue;
            }

            if (child.Tag is "meta" or "link" or "favicon" or "icon" or "apple-touch-icon"
                or "canonical" or "og" or "twitter" or "jsonld" or "ld+json")
                yield return child;
        }
    }

    private static void EmitHeadNode(StringBuilder sb, SoloHtmlNode node, string pad)
    {
        switch (node.Tag)
        {
            case "title":
            case "style":
            case "css":
            case "stylesheet":
                return; // handled elsewhere

            case "meta":
            {
                sb.Append(pad).Append("<meta");
                var attrs = new Dictionary<string, string>(node.Attributes, StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(node.Text) && !attrs.ContainsKey("content"))
                    attrs["content"] = node.Text;
                WriteAttrs(sb, attrs);
                sb.AppendLine(" />");
                return;
            }

            case "favicon":
            case "icon":
            {
                var href = AttrOrText(node, "href", "src");
                var type = node.Attributes.TryGetValue("type", out var ty) ? ty : null;
                sb.Append(pad).Append("<link rel=\"icon\"");
                if (!string.IsNullOrWhiteSpace(type))
                    sb.Append(" type=\"").Append(WebUtility.HtmlEncode(type)).Append('"');
                sb.Append(" href=\"").Append(WebUtility.HtmlEncode(href ?? "/favicon.ico")).AppendLine("\" />");
                return;
            }

            case "apple-touch-icon":
            {
                var href = AttrOrText(node, "href", "src") ?? "/apple-touch-icon.png";
                sb.Append(pad).Append("<link rel=\"apple-touch-icon\" href=\"")
                    .Append(WebUtility.HtmlEncode(href)).AppendLine("\" />");
                return;
            }

            case "canonical":
            {
                var href = AttrOrText(node, "href", "src") ?? node.Text;
                if (string.IsNullOrWhiteSpace(href))
                    throw new SoloHtmlException($"line {node.Line}: canonical needs href");
                sb.Append(pad).Append("<link rel=\"canonical\" href=\"")
                    .Append(WebUtility.HtmlEncode(href.Trim())).AppendLine("\" />");
                return;
            }

            case "og":
            {
                // og title=… / og property=og:title content=…
                var attrs = new Dictionary<string, string>(node.Attributes, StringComparer.OrdinalIgnoreCase);
                if (!attrs.ContainsKey("property"))
                {
                    // first non-content key becomes og:* 
                    var propKey = attrs.Keys.FirstOrDefault(k =>
                        !k.Equals("content", StringComparison.OrdinalIgnoreCase));
                    if (propKey is not null)
                    {
                        var val = attrs[propKey];
                        attrs.Remove(propKey);
                        attrs["property"] = propKey.StartsWith("og:", StringComparison.OrdinalIgnoreCase)
                            ? propKey
                            : "og:" + propKey;
                        if (!attrs.ContainsKey("content"))
                            attrs["content"] = val;
                    }
                }
                if (!string.IsNullOrWhiteSpace(node.Text) && !attrs.ContainsKey("content"))
                    attrs["content"] = node.Text;
                sb.Append(pad).Append("<meta");
                WriteAttrs(sb, attrs);
                sb.AppendLine(" />");
                return;
            }

            case "twitter":
            {
                var attrs = new Dictionary<string, string>(node.Attributes, StringComparer.OrdinalIgnoreCase);
                if (!attrs.ContainsKey("name"))
                {
                    var propKey = attrs.Keys.FirstOrDefault(k =>
                        !k.Equals("content", StringComparison.OrdinalIgnoreCase));
                    if (propKey is not null)
                    {
                        var val = attrs[propKey];
                        attrs.Remove(propKey);
                        attrs["name"] = propKey.StartsWith("twitter:", StringComparison.OrdinalIgnoreCase)
                            ? propKey
                            : "twitter:" + propKey;
                        if (!attrs.ContainsKey("content"))
                            attrs["content"] = val;
                    }
                }
                if (!string.IsNullOrWhiteSpace(node.Text) && !attrs.ContainsKey("content"))
                    attrs["content"] = node.Text;
                sb.Append(pad).Append("<meta");
                WriteAttrs(sb, attrs);
                sb.AppendLine(" />");
                return;
            }

            case "jsonld":
            case "ld+json":
            {
                var json = node.Text ?? string.Join("\n", node.Children.Select(c =>
                    string.IsNullOrWhiteSpace(c.Text) ? c.Tag : $"{c.Tag} {c.Text}"));
                sb.Append(pad).AppendLine("<script type=\"application/ld+json\">");
                sb.Append(pad).Append("  ").AppendLine(json.Trim());
                sb.Append(pad).AppendLine("</script>");
                return;
            }

            case "link":
            {
                // Real HTML <link> — never an anchor.
                var attrs = new Dictionary<string, string>(node.Attributes, StringComparer.OrdinalIgnoreCase);
                if (!attrs.ContainsKey("href"))
                {
                    var href = AttrOrText(node, "href", "src");
                    if (!string.IsNullOrWhiteSpace(href))
                        attrs["href"] = href!;
                }
                sb.Append(pad).Append("<link");
                WriteAttrs(sb, attrs);
                sb.AppendLine(" />");
                return;
            }

            default:
                EmitNodeStatic(sb, node, pad);
                return;
        }
    }

    private static void EmitNodeStatic(StringBuilder sb, SoloHtmlNode node, string pad)
    {
        // Minimal fallback for unexpected head children
        sb.Append(pad).Append("<!-- head: ").Append(node.Tag).AppendLine(" -->");
    }

    private static string? AttrOrText(SoloHtmlNode node, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (node.Attributes.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
        }
        return string.IsNullOrWhiteSpace(node.Text) ? null : node.Text.Trim();
    }

    private static bool HasSrc(SoloHtmlNode node) =>
        node.Attributes.ContainsKey("src") || !string.IsNullOrWhiteSpace(node.Text);

    private static void EmitStylesheetLink(StringBuilder sb, SoloHtmlNode node, string pad)
    {
        var href = node.Attributes.TryGetValue("href", out var h) ? h
            : node.Attributes.TryGetValue("src", out var s) ? s
            : node.Text;
        if (string.IsNullOrWhiteSpace(href))
            throw new SoloHtmlException($"line {node.Line}: css needs href — try `css href=styles.css`");
        sb.Append(pad).Append("<link rel=\"stylesheet\" href=\"")
            .Append(WebUtility.HtmlEncode(href.Trim()))
            .AppendLine("\" />");
    }

    private static void EmitScriptTag(StringBuilder sb, SoloHtmlNode node, string pad)
    {
        var src = node.Attributes.TryGetValue("src", out var s) ? s : node.Text;
        if (string.IsNullOrWhiteSpace(src))
            throw new SoloHtmlException($"line {node.Line}: js needs src — try `js src=app.js`");
        sb.Append(pad).Append("<script src=\"")
            .Append(WebUtility.HtmlEncode(src.Trim()))
            .AppendLine("\"></script>");
    }

    private static MappedTag MapTag(SoloHtmlNode node)
    {
        var attrs = new Dictionary<string, string>(node.Attributes, StringComparer.OrdinalIgnoreCase);

        switch (node.Tag)
        {
            case "hero":
                AppendClass(attrs, "hero");
                return new MappedTag("header", attrs);
            case "brand":
                AppendClass(attrs, "brand");
                return new MappedTag("div", attrs);
            case "section":
                return new MappedTag("section", attrs);
            case "card":
                AppendClass(attrs, "card");
                return new MappedTag("article", attrs);
            case "row":
                AppendClass(attrs, "row");
                return new MappedTag("div", attrs);
            case "list":
                return new MappedTag("ul", attrs);
            case "item":
                return new MappedTag("li", attrs);
            case "button":
            {
                // .button theme class is opt-in: primary/secondary/ghost/btn/styled
                if (WantsButtonThemeClass(attrs))
                    AppendClass(attrs, "button");
                if (attrs.ContainsKey("href"))
                    return new MappedTag("a", attrs);
                return new MappedTag("button", attrs);
            }
            case "css":
            case "stylesheet":
            {
                // Handled specially in EmitPage; fallback for fragments:
                if (!attrs.ContainsKey("rel"))
                    attrs["rel"] = "stylesheet";
                if (!attrs.ContainsKey("href"))
                {
                    if (attrs.TryGetValue("src", out var src))
                        attrs["href"] = src;
                    else if (!string.IsNullOrWhiteSpace(node.Text))
                        attrs["href"] = node.Text!;
                }
                return new MappedTag("link", attrs);
            }
            case "js":
            {
                if (!attrs.ContainsKey("src") && !string.IsNullOrWhiteSpace(node.Text))
                    attrs["src"] = node.Text!;
                return new MappedTag("script", attrs);
            }
            case "link":
                // HTML <link> for head assets — not an anchor. EmitPage handles page-level links.
                if (!attrs.ContainsKey("href") && !string.IsNullOrWhiteSpace(node.Text))
                    attrs["href"] = node.Text!;
                return new MappedTag("link", attrs);
            case "a":
            case "anchor":
                return new MappedTag("a", attrs);
            case "image":
            case "img":
                if (!attrs.ContainsKey("alt"))
                    attrs["alt"] = node.Text ?? string.Empty;
                if (!attrs.ContainsKey("src") && !string.IsNullOrWhiteSpace(node.Text))
                    attrs["src"] = node.Text!;
                return new MappedTag("img", attrs);
            case "footer":
                return new MappedTag("footer", attrs);
            case "nav":
                return new MappedTag("nav", attrs);
            case "form":
            case "input":
            case "label":
            case "textarea":
            case "select":
            case "option":
            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "p":
            case "div":
            case "span":
            case "main":
            case "header":
            case "ul":
            case "ol":
            case "li":
            case "strong":
            case "em":
            case "code":
            case "pre":
            case "canvas":
                return new MappedTag(node.Tag, attrs);
            default:
                // Unknown tags pass through as custom elements / plain tags.
                return new MappedTag(node.Tag, attrs);
        }
    }

    private static bool WantsButtonThemeClass(Dictionary<string, string> attrs)
    {
        if (attrs.ContainsKey("btn") || attrs.ContainsKey("styled"))
            return true;
        if (!attrs.TryGetValue("class", out var cls) || string.IsNullOrWhiteSpace(cls))
            return false;
        return cls.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(c => c is "primary" or "secondary" or "ghost" or "btn" or "styled" or "button");
    }

    private static void AppendClass(Dictionary<string, string> attrs, string className)
    {
        if (attrs.TryGetValue("class", out var existing))
            attrs["class"] = existing + " " + className;
        else
            attrs["class"] = className;
    }

    private static void WriteAttrs(StringBuilder sb, IReadOnlyDictionary<string, string> attrs)
    {
        foreach (var (key, value) in attrs)
        {
            sb.Append(' ')
                .Append(WebUtility.HtmlEncode(key))
                .Append("=\"")
                .Append(WebUtility.HtmlEncode(value))
                .Append('"');
        }
    }

    private sealed record MappedTag(string Tag, Dictionary<string, string> Attributes);

    private const string DefaultCss = """
    :root { color-scheme: light; }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", system-ui, sans-serif;
      color: #102018;
      background: #f4fff8;
      line-height: 1.5;
    }
    .brand {
      font-weight: 800;
      font-size: 1.25rem;
      letter-spacing: -0.03em;
    }
    .hero {
      padding: 4rem clamp(1.25rem, 5vw, 4rem);
      background: linear-gradient(145deg, #0f2a22, #1d4d3c 55%, #d8ff3e);
      color: #f4fff8;
    }
    .hero h1 { font-size: clamp(2.4rem, 7vw, 4.5rem); line-height: 1; margin: 0.4rem 0 0.8rem; }
    .hero p { max-width: 34ch; font-size: 1.15rem; opacity: 0.92; }
    section { padding: 2.5rem clamp(1.25rem, 5vw, 4rem); }
    .row { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }
    .card {
      background: white;
      border: 1px solid #d7ebdd;
      border-radius: 16px;
      padding: 1.1rem 1.2rem;
      box-shadow: 0 10px 30px rgba(16, 32, 24, 0.06);
    }
    .button {
      display: inline-flex;
      margin-top: 1rem;
      padding: 0.7rem 1.1rem;
      border-radius: 999px;
      background: #d8ff3e;
      color: #102018;
      text-decoration: none;
      font-weight: 700;
      border: 0;
    }
    .button.primary { background: #d8ff3e; }
    .button.secondary { background: #3dffc2; }
    .button.ghost { background: transparent; border: 1px solid currentColor; color: inherit; }
    ul { padding-left: 1.2rem; }
    footer { padding: 2rem; color: #5d7468; text-align: center; }
    """;
}
