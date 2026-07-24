using System.Text;
using SoloC.Compiler;

namespace SoloC.Tests;

public class InterpreterTests
{
    [Fact]
    public void Runs_hello_print()
    {
        var output = Evaluate("""
            print("Hello, SoloC!");
            """);

        Assert.Equal("Hello, SoloC!" + Environment.NewLine, output);
    }

    [Fact]
    public void Runs_arithmetic_and_variables()
    {
        var output = Evaluate("""
            var a = 10;
            let b = 32;
            print(a + b);
            """);

        Assert.Equal("42" + Environment.NewLine, output);
    }

    [Fact]
    public void Runs_functions()
    {
        var output = Evaluate("""
            fn add(int a, int b): int {
                return a + b;
            }

            print(add(20, 22));
            """);

        Assert.Equal("42" + Environment.NewLine, output);
    }

    [Fact]
    public void Runs_if_while_for()
    {
        var output = Evaluate("""
            var sum = 0;
            for (var i = 1; i <= 5; i = i + 1) {
                sum = sum + i;
            }
            print(sum);

            var n = 3;
            while (n > 0) {
                n = n - 1;
            }
            print(n);

            if (sum == 15) {
                print("ok");
            } else {
                print("no");
            }
            """);

        Assert.Equal(
            "15" + Environment.NewLine +
            "0" + Environment.NewLine +
            "ok" + Environment.NewLine,
            output);
    }

    [Fact]
    public void Runs_class_instance_methods()
    {
        var output = Evaluate("""
            class Counter {
                int value = 0;

                void Inc() {
                    this.value = this.value + 1;
                }

                int Get() {
                    return this.value;
                }
            }

            var c = new Counter();
            c.Inc();
            c.Inc();
            print(c.Get());
            """);

        Assert.Equal("2" + Environment.NewLine, output);
    }

    [Fact]
    public void Runs_csharp_style_main()
    {
        var output = Evaluate("""
            class Program {
                static void Main() {
                    Console.WriteLine("from Main");
                }
            }
            """);

        Assert.Equal("from Main" + Environment.NewLine, output);
    }

    [Fact]
    public void Let_is_immutable()
    {
        var compilation = new Compilation("""
            let x = 1;
            x = 2;
            """);
        var result = compilation.Evaluate(new StringWriter());
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("let", StringComparison.OrdinalIgnoreCase));
    }

    private static string Evaluate(string source)
    {
        var writer = new StringWriter();
        var result = new Compilation(source).Evaluate(writer);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return writer.ToString();
    }
}
