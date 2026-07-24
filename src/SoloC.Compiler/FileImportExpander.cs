using System.Text;
using System.Text.RegularExpressions;

namespace SoloC.Compiler;

/// <summary>
/// Expands <c>using "file.sc";</c> / <c>import "file.sc";</c> into the source before parsing.
/// Named modules like <c>using Math;</c> are left alone.
/// </summary>
public static class FileImportExpander
{
    private static readonly Regex ImportLine = new(
        @"^\s*(?:using|import)\s+""([^""]+)""\s*;\s*(?://.*)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Expand(string source, string? fileName, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = string.IsNullOrWhiteSpace(fileName) || fileName == "<source>"
            ? null
            : Path.GetFullPath(fileName);

        if (root is not null)
            visited.Add(root);

        var expanded = ExpandCore(source, root, visited, errorList, depth: 0);
        errors = errorList;
        return expanded;
    }

    private static string ExpandCore(
        string source,
        string? currentFile,
        HashSet<string> visited,
        List<string> errors,
        int depth)
    {
        if (depth > 32)
        {
            errors.Add("Too many nested imports (max 32). Check for cycles.");
            return source;
        }

        var baseDir = currentFile is null
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(currentFile)!;

        var sb = new StringBuilder();
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var line in lines)
        {
            var match = ImportLine.Match(line);
            if (!match.Success)
            {
                sb.AppendLine(line);
                continue;
            }

            var relative = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(relative))
            {
                errors.Add("Empty import path.");
                continue;
            }

            var path = Path.GetFullPath(Path.Combine(baseDir, relative));
            if (!visited.Add(path))
            {
                sb.AppendLine($"// skipped cyclic import: {relative}");
                continue;
            }

            if (!File.Exists(path))
            {
                errors.Add($"Import not found: {relative} (looked in {baseDir})");
                continue;
            }

            sb.AppendLine($"// === begin import {relative} ===");
            var imported = File.ReadAllText(path);
            sb.Append(ExpandCore(imported, path, visited, errors, depth + 1));
            if (!sb.ToString().EndsWith('\n'))
                sb.AppendLine();
            sb.AppendLine($"// === end import {relative} ===");
        }

        return sb.ToString();
    }
}
