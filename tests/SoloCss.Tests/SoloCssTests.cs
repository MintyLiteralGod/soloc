using SoloCss.Compiler;

public class SoloCssTests
{
    [Fact]
    public void Compiles_vars_and_nested_rules()
    {
        var result = SoloCssCompiler.Compile(
            """
            vars
              brand #0f2a22
              accent #d8ff3e

            .hero
              background $brand
              color $accent

              h1
                size 3rem
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains(":root", result.Css);
        Assert.Contains("--brand:", result.Css);
        Assert.Contains(".hero {", result.Css);
        Assert.Contains("var(--brand)", result.Css);
        Assert.Contains(".hero h1", result.Css);
        Assert.Contains("font-size: 3rem", result.Css);
    }

    [Fact]
    public void Compiles_media_and_shortcuts()
    {
        var result = SoloCssCompiler.Compile(
            """
            .button
              no-underline
              bold
              radius 8px

            media max-width 640px
              .button
                pad 0.5rem
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("text-decoration: none", result.Css);
        Assert.Contains("font-weight: 700", result.Css);
        Assert.Contains("border-radius: 8px", result.Css);
        Assert.Contains("@media (max-width: 640px)", result.Css);
        Assert.Contains("padding: 0.5rem", result.Css);
    }

    [Fact]
    public void Supports_ampersand_parent_selector()
    {
        var result = SoloCssCompiler.Compile(
            """
            .card
              pad 1rem
              &:hover
                background #fff
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains(".card:hover", result.Css);
    }
}
