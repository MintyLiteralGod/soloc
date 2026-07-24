# Solo5

**SoloGem’s five easiest languages — one family, one mission.**

Solo5 is the name for the open-source language suite that helps beginners go from first `print` to real pages, styles, scripts, and systems thinking — without drowning in ceremony.

| # | Language | Extension | Compiles to | Studio |
|---|----------|-----------|-------------|--------|
| 1 | **SoloC** | `.sc` | runs via SoloC VM / interpreter | `:5088` |
| 2 | **SoloHTML** | `.solohtml` | HTML5 | `:5089` |
| 3 | **SoloCSS** | `.solocss` | CSS | `:5090` |
| 4 | **SoloJS** | `.solojs` | JavaScript | `:5091` |
| 5 | **SoloRUST** | `.solorust` | Rust source *(experimental)* | `:5092` |

## Why Solo5?

Each language removes a different kind of friction:

- **SoloC** — learn programming ideas (variables, loops, functions, classes)
- **SoloHTML** — build pages with indentation, not angle-bracket mazes
- **SoloCSS** — style with theme vars, nesting, and friendly shortcuts
- **SoloJS** — script the browser with `print`, `when ready`, and `on click`
- **SoloRUST** — peek at systems programming with a gentler on-ramp to Rust

Together they form a path: **think → page → style → interact → systems**.

## Quick start (all Studios)

```bash
dotnet run --project src/SoloC.Playground     # SoloC Studio   :5088
dotnet run --project src/SoloHtml.Studio       # SoloHTML       :5089
dotnet run --project src/SoloCss.Studio        # SoloCSS        :5090
dotnet run --project src/SoloJs.Studio         # SoloJS         :5091
dotnet run --project src/SoloRust.Studio       # SoloRUST       :5092
```

## Compilers

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloHtml.Cli -- compile examples/html/hello.solohtml
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss
dotnet run --project src/SoloJs.Cli -- compile examples/js/hello.solojs
dotnet run --project src/SoloRust.Cli -- compile examples/rust/hello.solorust
```

## Docs map

- [SoloC learn path](../learn/00-welcome.md)
- [SoloHTML](../solohtml/README.md)
- [SoloCSS](../solocss/README.md)
- [SoloJS](../solojs/README.md)
- [SoloRUST](../solorust/README.md) · [research notes](../solorust/research.md)

## Design rules (all five)

1. **Readable first** — a beginner should understand a sample in under a minute
2. **Friendly errors** — say what went wrong and how to fix it
3. **Real output** — compile to formats people already use (HTML/CSS/JS/Rust/runnable SoloC)
4. **Studio + CLI** — GUI for learning, CLI for workflows
5. **MIT + open** — free to learn from and improve
