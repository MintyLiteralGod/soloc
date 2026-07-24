using System.Net;
using System.Text;

namespace SoloHtml.Compiler;

public sealed class HtmlEmitter
{
    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
    };

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
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"utf-8\" />");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine($"  <title>{WebUtility.HtmlEncode(pageTitle ?? "SoloHTML")}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine(DefaultCss);
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            foreach (var root in roots)
                EmitNode(sb, root, 1, pageTitle);
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
        }

        return sb.ToString();
    }

    private void EmitNode(StringBuilder sb, SoloHtmlNode node, int depth, string? pageTitle)
    {
        var pad = new string(' ', depth * 2);
        var mapped = MapTag(node);

        if (node.Tag is "page")
        {
            EmitPage(sb, node, pageTitle);
            return;
        }

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

    private void EmitPage(StringBuilder sb, SoloHtmlNode page, string? pageTitle)
    {
        var title = page.Text ?? pageTitle ?? "SoloHTML";
        var titleNode = page.Children.FirstOrDefault(c => c.Tag == "title");
        if (titleNode?.Text is { } t)
            title = t;

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"  <title>{WebUtility.HtmlEncode(title)}</title>");

        foreach (var meta in page.Children.Where(c => c.Tag == "meta"))
        {
            sb.Append("  <meta");
            var attrs = new Dictionary<string, string>(meta.Attributes, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(meta.Text) && !attrs.ContainsKey("content"))
                attrs["content"] = meta.Text;
            if (!attrs.ContainsKey("name") && attrs.Count > 0)
            {
                // allow: meta description=...
            }

            WriteAttrs(sb, attrs);
            sb.AppendLine(" />");
        }

        var customStyle = page.Children.FirstOrDefault(c => c.Tag == "style");
        sb.AppendLine("  <style>");
        sb.AppendLine(DefaultCss);
        if (customStyle is not null)
        {
            if (!string.IsNullOrWhiteSpace(customStyle.Text))
                sb.AppendLine(customStyle.Text);
            foreach (var child in customStyle.Children)
                sb.AppendLine(string.IsNullOrWhiteSpace(child.Text) ? child.Tag : $"{child.Tag} {child.Text}");
        }

        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        foreach (var child in page.Children)
        {
            if (child.Tag is "title" or "meta" or "style")
                continue;
            EmitNode(sb, child, 1, title);
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
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
                AppendClass(attrs, "button");
                if (attrs.ContainsKey("href"))
                    return new MappedTag("a", attrs);
                return new MappedTag("button", attrs);
            }
            case "link":
            case "a":
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
                return new MappedTag(node.Tag, attrs);
            default:
                // Unknown tags pass through as custom elements / plain tags.
                return new MappedTag(node.Tag, attrs);
        }
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
