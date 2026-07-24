using System.Text;
using SoloC.Compiler;
using SoloC.Compiler.Diagnostics;

namespace SoloC.Tests;

public class ImportAndInputTests
{
    [Fact]
    public void Input_reads_line_with_prompt()
    {
        var output = new StringWriter();
        var input = new StringReader("Kaedyn\n");
        var compilation = new Compilation("""
            var name = input("Name: ");
            print(name);
            """);
        var result = compilation.Evaluate(output, ExecutionEngine.Interpreter, input);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Contains("Name: ", output.ToString());
        Assert.Contains("Kaedyn", output.ToString());
    }

    [Fact]
    public void File_import_merges_helpers()
    {
        var root = Path.Combine(Path.GetTempPath(), "soloc-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "lib.sc"), """
                fn twice(int n): int {
                    return n + n;
                }
                """);
            File.WriteAllText(Path.Combine(root, "main.sc"), """
                using "lib.sc";
                print(twice(21));
                """);

            var compilation = Compilation.FromFile(Path.Combine(root, "main.sc"));
            var output = new StringWriter();
            var result = compilation.Evaluate(output, ExecutionEngine.Interpreter);
            Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
            Assert.Equal("42" + Environment.NewLine, output.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
