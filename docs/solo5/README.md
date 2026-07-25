# Solo5

**SoloGem’s five languages — one family, less ceremony.**

Solo5 is the open-source suite for people tired of toolchain folklore: sharp defaults, indent-friendly sources, and compile-to-real targets.

| # | Language | Extension | Compiles to | Studio |
|---|----------|-----------|-------------|--------|
| 1 | **SoloC** | `.sc` | SoloC VM / interpreter | `:5088` |
| 2 | **SoloHTML** | `.solohtml` | HTML5 | `:5089` |
| 3 | **SoloCSS** | `.solocss` | CSS | `:5090` |
| 4 | **SoloJS** | `.solojs` | JavaScript (+ React) | `:5091` |
| 5 | **SoloLua** | `.sololua` | Lua 5.4 source | `:5092` |

**Plus:** [SoloPage](../solopage/README.md) bundles HTML+CSS+JS · **Solo5 Hub** at `:5080`

**Archived:** [SoloRUST](../../archive/solorust/README.md) — experimental Rust path, kept under `archive/solorust/` (Studio `:5099` if you revive it).

## Why Solo5?

Each language removes a different kind of friction:

- **SoloC** — programming ideas without the C#/JVM ritual pile
- **SoloHTML** — pages with indentation, `include`, CSS/JS links
- **SoloCSS** — vars, nesting, shortcuts → real CSS (`#` is color/id, not a comment)
- **SoloJS** — browser scripts, fetch/timers, optional React
- **SoloLua** — Lua’s runtime without globals-by-default, nil holes, or metatable class pain
- **SoloPage** — one folder → one page or a multi-route site (`pages/` + layouts)

## Quick start

```bash
# Hub (links to every Studio)
dotnet run --project src/Solo5.Hub
# → http://localhost:5080

dotnet run --project src/SoloC.Playground     # SoloC     :5088
dotnet run --project src/SoloHtml.Studio       # SoloHTML  :5089
dotnet run --project src/SoloCss.Studio        # SoloCSS   :5090
dotnet run --project src/SoloJs.Studio         # SoloJS    :5091
dotnet run --project src/SoloLua.Studio        # SoloLua   :5092
```

## Compilers

```bash
dotnet run --project src/SoloC.Cli -- run examples/soloc/greeter.sc
dotnet run --project src/SoloHtml.Cli -- compile examples/html/with-include.solohtml
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss
dotnet run --project src/SoloJs.Cli -- compile examples/js/fetch-timer.solojs
dotnet run --project src/SoloLua.Cli -- compile examples/lua/showcase.sololua
dotnet run --project src/SoloPage.Cli -- build examples/page
dotnet run --project src/SoloPage.Cli -- build examples/site
```

## Docs map

- [SoloC learn path](../learn/00-welcome.md)
- [SoloHTML](../solohtml/README.md)
- [SoloCSS](../solocss/README.md)
- [SoloJS](../solojs/README.md)
- [SoloLua](../sololua/README.md) · [studio](../sololua/studio.md)
- [SoloPage](../solopage/README.md)
- [Archived SoloRUST](../../archive/solorust/README.md)

## Design rules (all five)

1. **Readable first** — a sharp sample should land in under a minute
2. **Friendly errors** — say what went wrong and how to fix it
3. **Real output** — HTML/CSS/JS/Lua/runnable SoloC, not a walled garden
4. **Studio + CLI** — GUI for exploring, CLI for workflows
5. **MIT + open** — free to learn from and improve
