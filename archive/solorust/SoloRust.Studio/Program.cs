using SoloRust.Compiler;
using SoloRust.Studio;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5099");
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var demos = SoloRustDemoCatalog.All;

app.MapGet("/api/demos", () => demos.Select(d => new { d.Id, d.Title, d.Blurb }));
app.MapGet("/api/demos/{id}", (string id) =>
{
    var demo = demos.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    return demo is null ? Results.NotFound() : Results.Ok(demo);
});

app.MapPost("/api/compile", (CompileRequest request) =>
{
    var result = SoloRustCompiler.Compile(request.Source ?? string.Empty, request.Title);
    return Results.Ok(new CompileResponse(result.Ok, result.Rust, result.Errors.ToArray(), result.Notes.ToArray()));
});

app.MapFallbackToFile("index.html");
app.Run();

internal sealed record CompileRequest(string? Source, string? Title);
internal sealed record CompileResponse(bool Ok, string Rust, string[] Errors, string[] Notes);
