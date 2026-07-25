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
            // SoloCSS owns the look — SoloHTML default theme stays out.
            Assert.DoesNotContain("color-scheme: light", result.Html);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
