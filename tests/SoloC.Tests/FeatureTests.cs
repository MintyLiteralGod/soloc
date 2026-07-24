using System.Text;
using SoloC.Compiler;
using SoloC.Compiler.Diagnostics;

namespace SoloC.Tests;

public class FeatureTests
{
    [Fact]
    public void Arrays_index_length_and_update()
    {
        var output = Evaluate("""
            var nums = [1, 2, 3];
            print(nums.Length);
            print(nums[1]);
            nums[1] = 9;
            print(nums[1]);
            """);

        Assert.Equal(
            "3" + Environment.NewLine +
            "2" + Environment.NewLine +
            "9" + Environment.NewLine,
            output);
    }

    [Fact]
    public void Using_Math_imports_helpers()
    {
        var output = Evaluate("""
            using Math;
            print(sqrt(9));
            print(max(2, 8));
            """);

        Assert.Equal(
            "3" + Environment.NewLine +
            "8" + Environment.NewLine,
            output);
    }

    [Fact]
    public void Type_checker_catches_mismatches()
    {
        var result = new Compilation("""
            int x = "nope";
            """).Evaluate(new StringWriter());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("Type mismatch"));
    }

    [Fact]
    public void Diagnostics_include_line_and_column()
    {
        var result = new Compilation("""
            print(missing);
            """).Evaluate(new StringWriter());

        Assert.False(result.Success);
        var error = Assert.Single(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.NotNull(error.Location);
        Assert.True(error.Location!.Value.Line >= 1);
        Assert.True(error.Location.Value.Column >= 1);
    }

    [Fact]
    public void Bytecode_vm_runs_simple_script()
    {
        var writer = new StringWriter();
        var result = new Compilation("""
            var x = 20;
            print(x + 22);
            """).Evaluate(writer, ExecutionEngine.Vm);

        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(ExecutionEngine.Vm, result.Engine);
        Assert.Equal("42" + Environment.NewLine, writer.ToString());
    }

    [Fact]
    public void Friendly_let_error_message()
    {
        var result = new Compilation("""
            let x = 1;
            x = 2;
            """).Evaluate(new StringWriter());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("let"));
    }

    private static string Evaluate(string source)
    {
        var writer = new StringWriter();
        var result = new Compilation(source).Evaluate(writer);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return writer.ToString();
    }
}
