namespace SoloHtml.Compiler;

/// <summary>
/// Expands <c>include</c> / <c>component</c> tags by splicing in other .solohtml files.
/// </summary>
public static class SoloHtmlIncludeExpander
{
    public static SoloHtmlDocument Expand(SoloHtmlDocument document, string? basePath, HashSet<string>? stack = null)
    {
        stack ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = ExpandNodes(document.Roots, basePath ?? Directory.GetCurrentDirectory(), stack);
        return new SoloHtmlDocument(roots);
    }

    private static List<SoloHtmlNode> ExpandNodes(
        IReadOnlyList<SoloHtmlNode> nodes,
        string basePath,
        HashSet<string> stack)
    {
        var result = new List<SoloHtmlNode>();
        foreach (var node in nodes)
        {
            if (node.Tag is "include" or "component")
            {
                var path = ResolvePath(node, basePath);
                if (path is null)
                    throw new SoloHtmlException($"line {node.Line}: include needs a file — try `include nav.solohtml` or `include file=nav.solohtml`");

                var full = Path.GetFullPath(Path.Combine(basePath, path));
                if (!stack.Add(full))
                    throw new SoloHtmlException($"line {node.Line}: cyclic include: {path}");

                if (!File.Exists(full))
                    throw new SoloHtmlException($"line {node.Line}: include not found: {path}");

                var source = File.ReadAllText(full);
                var imported = new SoloHtmlParser().Parse(source);
                var dir = Path.GetDirectoryName(full)!;
                var expanded = Expand(imported, dir, stack);
                result.AddRange(expanded.Roots);
                stack.Remove(full);
                continue;
            }

            var children = ExpandNodes(node.Children, basePath, stack);
            result.Add(new SoloHtmlNode(node.Tag, node.Text, node.Attributes, children, node.Line));
        }

        return result;
    }

    private static string? ResolvePath(SoloHtmlNode node, string basePath)
    {
        if (node.Attributes.TryGetValue("file", out var file) && !string.IsNullOrWhiteSpace(file))
            return file;
        if (node.Attributes.TryGetValue("src", out var src) && !string.IsNullOrWhiteSpace(src))
            return src;
        if (!string.IsNullOrWhiteSpace(node.Text))
            return node.Text.Trim();
        return null;
    }
}
