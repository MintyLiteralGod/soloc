using SoloHtml.Compiler;

namespace SoloHtml.Tests;

public class SoloHtmlTests
{
    [Fact]
    public void Compiles_simple_page()
    {
        var result = SoloHtmlCompiler.Compile("""
            page SoloHTML
              title Hello SoloHTML
              hero
                brand SoloGem
                h1 Easiest markup on the web
                p Indentation instead of angle brackets.
                button primary href=#go Get started
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("<!DOCTYPE html>", result.Html);
        Assert.Contains("<title>Hello SoloHTML</title>", result.Html);
        Assert.Contains("class=\"hero\"", result.Html);
        Assert.Contains("SoloGem", result.Html);
        Assert.Contains("Get started", result.Html);
        Assert.Contains("href=\"#go\"", result.Html);
    }

    [Fact]
    public void Compiles_cards_and_lists()
    {
        var result = SoloHtmlCompiler.Compile("""
            section Features
              row
                card
                  h3 Simple
                  p Learn in minutes.
                card
                  h3 Real HTML
                  p Compiles cleanly.
              list
                item Made by SoloGem
                item Open source
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("<article", result.Html);
        Assert.Contains("class=\"card\"", result.Html);
        Assert.Contains("<ul>", result.Html);
        Assert.Contains("<li>Made by SoloGem</li>", result.Html);
    }

    [Fact]
    public void Supports_classes_and_ids()
    {
        var result = SoloHtmlCompiler.Compile("""
            div #hero.banner center Hello
            """);

        Assert.True(result.Ok, string.Join("; ", result.Errors));
        Assert.Contains("id=\"hero\"", result.Html);
        Assert.Contains("banner", result.Html);
        Assert.Contains("center", result.Html);
        Assert.Contains("Hello", result.Html);
    }
}
