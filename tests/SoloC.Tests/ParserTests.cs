using SoloC.Compiler;
using SoloC.Compiler.Syntax;

namespace SoloC.Tests;

public class ParserTests
{
    [Fact]
    public void Parses_function_declaration()
    {
        var result = new Compilation("""
            fn add(int a, int b): int {
                return a + b;
            }
            """).Parse();

        Assert.True(result.Success);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(result.Tree.Members));
        Assert.Equal("add", function.Identifier.Text);
        Assert.Equal(2, function.Parameters.Parameters.Count);
    }

    [Fact]
    public void Parses_class_with_method()
    {
        var result = new Compilation("""
            class Greeter {
                string name;
                void Say() {
                    print(this.name);
                }
            }
            """).Parse();

        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        var classDecl = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(result.Tree.Members));
        Assert.Equal("Greeter", classDecl.Identifier.Text);
        Assert.Equal(2, classDecl.Members.Count);
    }

    [Fact]
    public void Parses_top_level_statements()
    {
        var result = new Compilation("""
            var x = 1;
            print(x);
            """).Parse();

        Assert.True(result.Success);
        Assert.Equal(2, result.Tree.Members.Count);
    }
}
