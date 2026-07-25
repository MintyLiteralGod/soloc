using SoloPage.Compiler;

public class SoloPageTests
{
    [Fact]
    public void Builds_html_css_js_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "solopage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "page.solohtml"), """
                page T
                  title T
                  h1 Hi
                """);
            File.WriteAllText(Path.Combine(root, "styles.solocss"), """
                body
                  margin 0
                """);
            File.WriteAllText(Path.Combine(root, "app.solojs"), """
                print "ok"
                """);

            var result = SoloPageCompiler.Build(root);
            Assert.True(result.Ok, string.Join("; ", result.Errors));
            Assert.Contains("<h1", result.Html);
            Assert.Contains("margin: 0", result.Html);
            Assert.Contains("console.log(\"ok\")", result.Html);
            Assert.DoesNotContain("color-scheme: light", result.Html);
            Assert.False(result.IsSite);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Builds_multi_page_site_with_shared_assets()
    {
        var root = Path.Combine(Path.GetTempPath(), "solosite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "pages"));
        Directory.CreateDirectory(Path.Combine(root, "layouts"));
        try
        {
            File.WriteAllText(Path.Combine(root, "layouts", "shell.solohtml"), """
                page theme=none
                  title Site
                  main
                    slot
                """);
            File.WriteAllText(Path.Combine(root, "pages", "index.solohtml"), """
                layout ../layouts/shell.solohtml
                  title Home
                  h1 Home
                """);
            File.WriteAllText(Path.Combine(root, "pages", "deskcore.solohtml"), """
                layout ../layouts/shell.solohtml
                  title DeskCore
                  h1 DeskCore
                """);
            File.WriteAllText(Path.Combine(root, "styles.solocss"), """
                include tokens.solocss
                body
                  margin 0
                """);
            File.WriteAllText(Path.Combine(root, "tokens.solocss"), """
                vars
                  brand #0f2a22
                """);
            File.WriteAllText(Path.Combine(root, "app.solojs"), """
                when ready
                  toggleClass "body" ready
                """);

            var result = SoloPageCompiler.Build(root);
            Assert.True(result.Ok, string.Join("; ", result.Errors));
            Assert.True(result.IsSite);
            Assert.NotNull(result.Files);
            Assert.Contains(result.Files!, f => f.RelativePath == "index.html");
            Assert.Contains(result.Files!, f => f.RelativePath == "deskcore/index.html");
            Assert.Contains(result.Files!, f => f.RelativePath == "assets/site.css");
            Assert.Contains(result.Files!, f => f.RelativePath == "assets/site.js");

            var home = result.Files!.First(f => f.RelativePath == "index.html").Content;
            var desk = result.Files!.First(f => f.RelativePath == "deskcore/index.html").Content;
            Assert.Contains("<h1>Home</h1>", home);
            Assert.Contains("<h1>DeskCore</h1>", desk);
            Assert.Contains("assets/site.css", home);
            Assert.Contains("../assets/site.css", desk);
            Assert.Contains("--brand:", result.Files!.First(f => f.RelativePath == "assets/site.css").Content);
            Assert.Contains("toggleClass", result.Files!.First(f => f.RelativePath == "assets/site.js").Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
