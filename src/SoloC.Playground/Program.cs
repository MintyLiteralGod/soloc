using SoloC.Compiler;
using SoloC.Compiler.Diagnostics;
using SoloC.Playground;
using SoloHtml.Compiler;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5088");
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var demos = DemoCatalog.All;
var htmlDemos = HtmlDemoCatalog.All;

app.MapGet("/api/demos", () => demos.Select(d => new
{
    d.Id,
    d.Title,
    d.Blurb,
    d.Mode,
}));

app.MapGet("/api/demos/{id}", (string id) =>
{
    var demo = demos.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    return demo is null ? Results.NotFound() : Results.Ok(demo);
});

app.MapGet("/api/html/demos", () => htmlDemos.Select(d => new
{
    d.Id,
    d.Title,
    d.Blurb,
}));

app.MapGet("/api/html/demos/{id}", (string id) =>
{
    var demo = htmlDemos.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    return demo is null ? Results.NotFound() : Results.Ok(demo);
});

app.MapPost("/api/run", (RunRequest request) =>
{
    var source = request.Source ?? string.Empty;
    var output = new StringWriter();
    var compilation = new Compilation(source, request.FileName ?? "playground.sc");
    var result = compilation.Evaluate(output, ExecutionEngine.Interpreter);

    return Results.Ok(new RunResponse(
        result.Success,
        output.ToString(),
        result.Engine?.ToString(),
        result.Diagnostics.Select(d => FormatDiagnostic(d, compilation.SourceText.FileName)).ToArray()));
});

app.MapPost("/api/html/compile", (HtmlCompileRequest request) =>
{
    var result = SoloHtmlCompiler.Compile(request.Source ?? string.Empty, request.Title);
    return Results.Ok(new HtmlCompileResponse(result.Ok, result.Html, result.Errors.ToArray()));
});

app.MapPost("/api/arena/battle", (ArenaBattleRequest request) =>
{
    var battle = ArenaEngine.Simulate(request);
    return Results.Ok(battle);
});

app.MapFallbackToFile("index.html");

app.Run();

static string FormatDiagnostic(Diagnostic d, string file)
{
    var loc = d.Location is { } l ? $"{l.Line}:{l.Column}" : "?";
    var tip = string.IsNullOrWhiteSpace(d.Tip) ? string.Empty : $" — tip: {d.Tip}";
    return $"{file}:{loc}: {d.Severity.ToString().ToLowerInvariant()}: {d.Message}{tip}";
}

internal sealed record RunRequest(string? Source, string? FileName);
internal sealed record RunResponse(bool Ok, string Output, string? Engine, string[] Diagnostics);
internal sealed record HtmlCompileRequest(string? Source, string? Title);
internal sealed record HtmlCompileResponse(bool Ok, string Html, string[] Errors);
