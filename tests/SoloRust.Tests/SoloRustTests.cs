using SoloRust.Compiler;

public class SoloRustTests
{
    [Fact]
    public void Compiles_hello_main()
    {
        var result = SoloRustCompiler.Compile(
            """
            fn main()
              let name = "SoloRUST"
              println "Hello, {name}"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("fn main()", result.Rust);
        Assert.Contains("let name = \"SoloRUST\";", result.Rust);
        Assert.Contains("println!(\"Hello, {}\", name);", result.Rust);
        Assert.NotEmpty(result.Notes);
    }

    [Fact]
    public void Compiles_functions_loops_and_if()
    {
        var result = SoloRustCompiler.Compile(
            """
            fn add(a: i32, b: i32) -> i32
              return a + b

            fn main()
              let mut total = 0
              for i in 1..4
                total = total + i
              if total == 6
                println "ok"
              else
                println "no"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("fn add(a: i32, b: i32) -> i32", result.Rust);
        Assert.Contains("let mut total = 0;", result.Rust);
        Assert.Contains("for i in 1..4", result.Rust);
        Assert.Contains("if total == 6", result.Rust);
        Assert.Contains("else {", result.Rust);
    }

    [Fact]
    public void Rejects_top_level_statements()
    {
        var result = SoloRustCompiler.Compile("println \"nope\"");
        Assert.False(result.Ok);
        Assert.NotEmpty(result.Errors);
    }
}
