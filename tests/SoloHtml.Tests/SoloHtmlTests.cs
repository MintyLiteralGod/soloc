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

    [Fact]
    public void Default_theme_included_unless_opted_out()
    {
        var withTheme = SoloHtmlCompiler.Compile("""
            page Demo
              title Demo
              h1 Hi
            """);
        Assert.True(withTheme.Ok, string.Join("; ", withTheme.Errors));
        Assert.Contains("color-scheme: light", withTheme.Html);

        var themeNone = SoloHtmlCompiler.Compile("""
            page Demo theme=none
              title Demo
              h1 Hi
            """);
        Assert.True(themeNone.Ok, string.Join("; ", themeNone.Errors));
        Assert.DoesNotContain("color-scheme: light", themeNone.Html);

        var notheme = SoloHtmlCompiler.Compile("""
            page Demo notheme
              title Demo
              h1 Hi
            """);
        Assert.True(notheme.Ok, string.Join("; ", notheme.Errors));
        Assert.DoesNotContain("color-scheme: light", notheme.Html);

        var withCss = SoloHtmlCompiler.Compile("""
            page Demo
              title Demo
              css href=app.css
              h1 Hi
            """);
        Assert.True(withCss.Ok, string.Join("; ", withCss.Errors));
        Assert.DoesNotContain("color-scheme: light", withCss.Html);

        var forcedOff = SoloHtmlCompiler.Compile("""
            page Demo
              title Demo
              h1 Hi
            """, emitOptions: new SoloHtmlEmitOptions { IncludeDefaultTheme = false });
        Assert.True(forcedOff.Ok, string.Join("; ", forcedOff.Errors));
        Assert.DoesNotContain("color-scheme: light", forcedOff.Html);
    }

    [Fact]
    public void Include_and_asset_links()
    {
        var root = Path.Combine(Path.GetTempPath(), "solohtml-inc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "components"));
        try
        {
            File.WriteAllText(Path.Combine(root, "components", "nav.solohtml"), """
                nav
                  brand SoloGem
                """);
            File.WriteAllText(Path.Combine(root, "page.solohtml"), """
                page Demo
                  title Demo
                  css href=app.css
                  include components/nav.solohtml
                  h1 Hello
                  js src=app.js
                """);

            var result = SoloHtmlCompiler.CompileFile(Path.Combine(root, "page.solohtml"));
            Assert.True(result.Ok, string.Join("; ", result.Errors));
            Assert.Contains("rel=\"stylesheet\"", result.Html);
            Assert.Contains("href=\"app.css\"", result.Html);
            Assert.Contains("<nav", result.Html);
            Assert.Contains("SoloGem", result.Html);
            Assert.Contains("<script src=\"app.js\"", result.Html);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
