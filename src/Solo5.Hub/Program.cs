var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5080");
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var languages = new[]
{
    new { id = "soloc", name = "SoloC", ext = ".sc", port = 5088, blurb = "Learn to code — easiest first language.", studio = "src/SoloC.Playground" },
    new { id = "solohtml", name = "SoloHTML", ext = ".solohtml", port = 5089, blurb = "Pages with indentation.", studio = "src/SoloHtml.Studio" },
    new { id = "solocss", name = "SoloCSS", ext = ".solocss", port = 5090, blurb = "Styles with vars and nesting.", studio = "src/SoloCss.Studio" },
    new { id = "solojs", name = "SoloJS", ext = ".solojs", port = 5091, blurb = "Browser scripts made friendly.", studio = "src/SoloJs.Studio" },
    new { id = "sololua", name = "SoloLua", ext = ".sololua", port = 5092, blurb = "Lua without the usual footguns.", studio = "src/SoloLua.Studio" },
    new { id = "solopage", name = "SoloPage", ext = "folder", port = 5080, blurb = "Bundle HTML+CSS+JS into one site.", studio = "src/SoloPage.Cli" },
};

app.MapGet("/api/languages", () => languages);
app.MapGet("/api/health", () => Results.Ok(new { ok = true, suite = "Solo5", by = "SoloGem" }));

app.MapFallbackToFile("index.html");
app.Run();
