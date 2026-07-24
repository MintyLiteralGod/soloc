using SoloJs.Compiler;

public class SoloJsTests
{
    [Fact]
    public void Compiles_print_fn_and_loop()
    {
        var result = SoloJsCompiler.Compile(
            """
            print "hi"

            fn add(a, b)
              return a + b

            print add(2, 40)

            for i in 0..3
              print i
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("console.log(\"hi\")", result.JavaScript);
        Assert.Contains("function add(a, b)", result.JavaScript);
        Assert.Contains("console.log(add(2, 40))", result.JavaScript);
        Assert.Contains("for (let i = 0; i < 3; i++)", result.JavaScript);
    }

    [Fact]
    public void Compiles_dom_helpers_and_reassign()
    {
        var result = SoloJsCompiler.Compile(
            """
            score = 0

            when ready
              set "#out" text "Ready"
              on click "#btn"
                score = score + 1
                set "#score" text score
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("let score = 0", result.JavaScript);
        Assert.Contains("score = score + 1", result.JavaScript);
        Assert.DoesNotContain("let score = score + 1", result.JavaScript);
        Assert.Contains("DOMContentLoaded", result.JavaScript);
        Assert.Contains("solo.on(", result.JavaScript);
        Assert.Contains("solo.set(", result.JavaScript);
    }

    [Fact]
    public void Compiles_if_elif_else()
    {
        var result = SoloJsCompiler.Compile(
            """
            if score >= 90
              print "A"
            elif score >= 80
              print "B"
            else
              print "Keep going"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("if (score >= 90)", result.JavaScript);
        Assert.Contains("else if (score >= 80)", result.JavaScript);
        Assert.Contains("else {", result.JavaScript);
    }

    [Fact]
    public void Compiles_fetch_and_timers()
    {
        var result = SoloJsCompiler.Compile(
            """
            after 100
              print "hi"
            every 1000
              print "tick"
            fetch "https://example.com" into data
              print data
            catch
              print "nope"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("solo.after(100", result.JavaScript);
        Assert.Contains("solo.every(1000", result.JavaScript);
        Assert.Contains("solo.fetch(", result.JavaScript);
        Assert.Contains(".catch(", result.JavaScript);
    }

    [Fact]
    public void Compiles_react_component()
    {
        var result = SoloJsCompiler.Compile(
            """
            react

            component Counter
              state count = 0

              fn bump()
                count = count + 1

              render
                div.card
                  h1 {count}
                  p "Clicks so far"
                  button onClick=bump "+1"

            mount Counter into "#root"
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.True(result.UsesReact);
        Assert.Contains("function Counter(props)", result.JavaScript);
        Assert.Contains("React.useState(0)", result.JavaScript);
        Assert.Contains("setCount(", result.JavaScript);
        Assert.Contains("React.createElement", result.JavaScript);
        Assert.Contains("solo.react.mount(Counter", result.JavaScript);
        Assert.Contains("onClick: bump", result.JavaScript);
    }
}
