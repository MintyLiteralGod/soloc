namespace SoloHtml.Compiler;

/// <summary>
/// Applies <c>layout file=…</c> shells: shared nav/footer/head with a <c>slot</c> for page body.
/// </summary>
public static class SoloHtmlLayoutExpander
{
    private static readonly HashSet<string> HeadishTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "meta", "head", "style", "css", "stylesheet", "link",
        "favicon", "icon", "apple-touch-icon", "canonical", "og",
    };

    public static SoloHtmlDocument Expand(SoloHtmlDocument document, string? basePath)
    {
        basePath ??= Directory.GetCurrentDirectory();
        var roots = new List<SoloHtmlNode>();

        foreach (var root in document.Roots)
        {
            if (root.Tag is "layout")
            {
                var path = ResolveLayoutPath(root);
                roots.Add(ApplyLayout(path, root.Children, root.Line, basePath));
                continue;
            }

            if (root.Tag is "page" && root.Attributes.TryGetValue("layout", out var layoutAttr) &&
                !string.IsNullOrWhiteSpace(layoutAttr))
            {
                var attrs = new Dictionary<string, string>(root.Attributes, StringComparer.OrdinalIgnoreCase);
                attrs.Remove("layout");
                var pageSansLayout = new SoloHtmlNode(root.Tag, root.Text, attrs, root.Children, root.Line);
                roots.Add(ApplyLayout(layoutAttr, new[] { pageSansLayout }, root.Line, basePath, wrapPageChildren: true));
                continue;
            }

            roots.Add(root);
        }

        return new SoloHtmlDocument(roots);
    }

    private static SoloHtmlNode ApplyLayout(
        string layoutPath,
        IReadOnlyList<SoloHtmlNode> contentRoots,
        int line,
        string basePath,
        bool wrapPageChildren = false)
    {
        var full = Path.GetFullPath(Path.Combine(basePath, layoutPath));
        if (!File.Exists(full))
            throw new SoloHtmlException($"line {line}: layout not found: {layoutPath}");

        var layoutDoc = new SoloHtmlParser().Parse(File.ReadAllText(full));
        var layoutDir = Path.GetDirectoryName(full)!;
        layoutDoc = SoloHtmlIncludeExpander.Expand(layoutDoc, layoutDir);

        var layoutPage = layoutDoc.Roots.FirstOrDefault(r => r.Tag is "page" or "html")
            ?? throw new SoloHtmlException($"line {line}: layout {layoutPath} must contain a `page` root");

        IReadOnlyList<SoloHtmlNode> bodyNodes;
        string? titleOverride = null;
        var headExtras = new List<SoloHtmlNode>();

        if (wrapPageChildren && contentRoots is [SoloHtmlNode { Tag: "page" } page])
        {
            titleOverride = page.Children.FirstOrDefault(c => c.Tag == "title")?.Text ?? page.Text;
            foreach (var child in page.Children)
            {
                if (child.Tag == "title") continue;
                if (child.Tag == "head")
                {
                    headExtras.AddRange(child.Children);
                    continue;
                }
                if (HeadishTags.Contains(child.Tag))
                {
                    headExtras.Add(child);
                    continue;
                }
            }
            bodyNodes = page.Children
                .Where(c => c.Tag is not "title" and not "head" && !HeadishTags.Contains(c.Tag))
                .ToList();
        }
        else
        {
            titleOverride = contentRoots.FirstOrDefault(c => c.Tag == "title")?.Text;
            foreach (var child in contentRoots)
            {
                if (child.Tag == "title") continue;
                if (child.Tag == "head")
                {
                    headExtras.AddRange(child.Children);
                    continue;
                }
                if (HeadishTags.Contains(child.Tag))
                {
                    headExtras.Add(child);
                    continue;
                }
            }
            bodyNodes = contentRoots
                .Where(c => c.Tag is not "title" and not "head" && !HeadishTags.Contains(c.Tag))
                .ToList();
        }

        var mergedChildren = MergeLayoutChildren(layoutPage.Children, bodyNodes, headExtras, titleOverride);
        var attrs = new Dictionary<string, string>(layoutPage.Attributes, StringComparer.OrdinalIgnoreCase);
        // Theme / bare flags from content page can win
        if (wrapPageChildren && contentRoots is [SoloHtmlNode { Tag: "page" } contentPage])
        {
            foreach (var key in new[] { "theme", "notheme", "bare" })
            {
                if (contentPage.Attributes.TryGetValue(key, out var val))
                    attrs[key] = val;
            }
        }

        return new SoloHtmlNode(layoutPage.Tag, titleOverride ?? layoutPage.Text, attrs, mergedChildren, layoutPage.Line);
    }

    private static List<SoloHtmlNode> MergeLayoutChildren(
        IReadOnlyList<SoloHtmlNode> layoutChildren,
        IReadOnlyList<SoloHtmlNode> bodyNodes,
        IReadOnlyList<SoloHtmlNode> headExtras,
        string? titleOverride)
    {
        var result = new List<SoloHtmlNode>();
        var slotFilled = false;

        foreach (var child in layoutChildren)
        {
            if (child.Tag == "title" && titleOverride is not null)
            {
                result.Add(new SoloHtmlNode("title", titleOverride, child.Attributes, child.Children, child.Line));
                continue;
            }

            if (child.Tag == "head")
            {
                var headKids = child.Children.Concat(headExtras).ToList();
                result.Add(new SoloHtmlNode("head", child.Text, child.Attributes, headKids, child.Line));
                headExtras = Array.Empty<SoloHtmlNode>();
                continue;
            }

            if (child.Tag is "slot" or "content")
            {
                result.AddRange(bodyNodes);
                slotFilled = true;
                continue;
            }

            var replaced = ReplaceSlots(child, bodyNodes, ref slotFilled);
            result.Add(replaced);
        }

        if (headExtras.Count > 0)
            result.InsertRange(0, headExtras);

        if (!slotFilled)
            result.AddRange(bodyNodes);

        return result;
    }

    private static SoloHtmlNode ReplaceSlots(SoloHtmlNode node, IReadOnlyList<SoloHtmlNode> bodyNodes, ref bool slotFilled)
    {
        var kids = new List<SoloHtmlNode>();
        foreach (var child in node.Children)
        {
            if (child.Tag is "slot" or "content")
            {
                kids.AddRange(bodyNodes);
                slotFilled = true;
                continue;
            }
            kids.Add(ReplaceSlots(child, bodyNodes, ref slotFilled));
        }
        return new SoloHtmlNode(node.Tag, node.Text, node.Attributes, kids, node.Line);
    }

    private static string ResolveLayoutPath(SoloHtmlNode node)
    {
        if (node.Attributes.TryGetValue("file", out var file) && !string.IsNullOrWhiteSpace(file))
            return file;
        if (node.Attributes.TryGetValue("src", out var src) && !string.IsNullOrWhiteSpace(src))
            return src;
        if (!string.IsNullOrWhiteSpace(node.Text))
            return node.Text.Trim();
        throw new SoloHtmlException($"line {node.Line}: layout needs a file — try `layout layouts/shell.solohtml`");
    }
}
