using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Text;

/// <summary>
/// Source text with fast line/column lookup for friendly diagnostics.
/// </summary>
public sealed class SourceText
{
    private readonly string _text;
    private readonly int[] _lineStarts;

    public SourceText(string text, string fileName = "<source>")
    {
        _text = text;
        FileName = fileName;
        _lineStarts = ComputeLineStarts(text);
    }

    public string FileName { get; }
    public string Text => _text;
    public int Length => _text.Length;
    public int LineCount => _lineStarts.Length;
    public char this[int index] => _text[index];

    public static SourceText From(string text, string fileName = "<source>") => new(text, fileName);

    public TextLocation GetLocation(int position)
    {
        position = Math.Clamp(position, 0, _text.Length);
        var lineIndex = GetLineIndex(position);
        var lineStart = _lineStarts[lineIndex];
        return new TextLocation(lineIndex + 1, position - lineStart + 1, position);
    }

    public TextLocation GetLocation(TextSpan span) => GetLocation(span.Start);

    public string GetLine(int lineNumber)
    {
        var index = Math.Clamp(lineNumber - 1, 0, _lineStarts.Length - 1);
        var start = _lineStarts[index];
        var end = index + 1 < _lineStarts.Length ? _lineStarts[index + 1] : _text.Length;
        var line = _text[start..end];
        return line.TrimEnd('\r', '\n');
    }

    public int GetLineIndex(int position)
    {
        var low = 0;
        var high = _lineStarts.Length - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var start = _lineStarts[mid];
            if (position < start)
                high = mid - 1;
            else if (mid + 1 < _lineStarts.Length && position >= _lineStarts[mid + 1])
                low = mid + 1;
            else
                return mid;
        }

        return _lineStarts.Length - 1;
    }

    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\n')
            {
                starts.Add(i + 1);
            }
            else if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }
}

public readonly record struct TextLocation(int Line, int Column, int Position)
{
    public override string ToString() => $"{Line}:{Column}";
}
