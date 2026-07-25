# SoloC · Solo5

**The easiest languages to learn — made by SoloGem**

SoloC started as an open-source, C#-inspired language for absolute beginners. It now leads **Solo5** — five friendly languages that compile to real tools beginners already meet on the web and beyond.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Made by SoloGem](https://img.shields.io/badge/Made%20by-SoloGem-0ea5e9.svg)](docs/philosophy.md)
[![Solo5](https://img.shields.io/badge/Solo5-five%20languages-16352c.svg)](docs/solo5/README.md)

## Solo5 at a glance

| Language | Ext | Output | Studio |
|----------|-----|--------|--------|
| **SoloC** | `.sc` | runs in SoloC | [localhost:5088](http://localhost:5088) |
| **SoloHTML** | `.solohtml` | HTML5 | [localhost:5089](http://localhost:5089) |
| **SoloCSS** | `.solocss` | CSS | [localhost:5090](http://localhost:5090) |
| **SoloJS** | `.solojs` | JavaScript | [localhost:5091](http://localhost:5091) |
| **SoloLua** | `.sololua` | Lua 5.4 source | [localhost:5092](http://localhost:5092) |
| **SoloPage** | folder | bundled HTML site | CLI + Hub |
| **Solo5 Hub** | — | Studio directory | [localhost:5080](http://localhost:5080) |

Full map: **[docs/solo5/README.md](docs/solo5/README.md)**

## 60-second start

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Clone this repo and build:

```bash
dotnet build SoloC.sln
```

3. Open the **Solo5 Hub** (or any Studio):

```bash
dotnet run --project src/Solo5.Hub             # Hub      :5080
dotnet run --project src/SoloC.Playground     # SoloC     :5088
dotnet run --project src/SoloHtml.Studio       # SoloHTML  :5089
dotnet run --project src/SoloCss.Studio        # SoloCSS   :5090
dotnet run --project src/SoloJs.Studio         # SoloJS    :5091
dotnet run --project src/SoloLua.Studio        # SoloLua   :5092
```

### Compilers (CLI)

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloHtml.Cli -- compile examples/html/showcase.solohtml
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss
dotnet run --project src/SoloJs.Cli -- compile examples/js/hello.solojs
dotnet run --project src/SoloLua.Cli -- compile examples/lua/showcase.sololua
dotnet run --project src/SoloPage.Cli -- build examples/page
```

## Learn SoloC

Follow the guided path — one small idea per lesson:

**[Start learning →](docs/learn/00-welcome.md)**

| Step | Lesson |
|------|--------|
| 00 | [Welcome](docs/learn/00-welcome.md) |
| 01 | [Your first program](docs/learn/01-your-first-program.md) |
| 02 | [Variables](docs/learn/02-variables.md) |
| 03 | [Decisions](docs/learn/03-decisions.md) |
| 04 | [Loops](docs/learn/04-loops.md) |
| 05 | [Functions](docs/learn/05-functions.md) |
| 06 | [Arrays](docs/learn/06-arrays.md) |
| 07 | [Classes](docs/learn/07-classes.md) |
| 08 | [Next steps](docs/learn/08-next-steps.md) |

More: [Docs hub](docs/README.md) · [Cheat sheet](docs/cheatsheet.md) · [Philosophy](docs/philosophy.md)

## Features

- **Solo5 suite** — SoloC + SoloHTML + SoloCSS + SoloJS + SoloLua
- **Solo5 Hub** — one landing page for every Studio (`:5080`)
- **SoloPage** — bundle HTML+CSS+JS from one folder
- **Studios for every language** — browser GUIs with demos and download
- **Dedicated compilers** — `soloc`, `solohtml`, `solocss`, `solojs`, `sololua`, `solopage`
- **SoloC** — multi-file `using "file.sc"`, `input()`, friendly errors, bytecode VM
- **SoloHTML** — `include` components, `css` / `js` links
- **SoloJS** — `fetch`, `after`, `every`, DOM helpers, React
- **SoloLua** — locals-by-default, dense `list`, `class`, `continue`, `!=`/`&&`/`||` → real Lua
- **Archived:** SoloRUST lives under [`archive/solorust/`](archive/solorust/README.md)

## Examples

```soloc
print("Hi,", "SoloGem");

fn add(int a, int b): int {
    return a + b;
}
print(add(2, 40));
```

```solocss
vars
  accent #d8ff3e
.button
  background $accent
  radius 0.55rem
  bold
```

```solojs
print "Hello, SoloJS"
when ready
  set "#out" text "Ready!"
```

## Open source

**MIT** licensed — free to use, learn from, and improve. See [LICENSE](LICENSE).

Created by **SoloGem**. Contributions welcome: [CONTRIBUTING.md](CONTRIBUTING.md).

## Repository layout

```text
src/SoloC.*             SoloC language, CLI, Studio (:5088)
src/SoloHtml.*          SoloHTML compiler, CLI, Studio (:5089)
src/SoloCss.*           SoloCSS compiler, CLI, Studio (:5090)
src/SoloJs.*            SoloJS compiler, CLI, Studio (:5091)
src/SoloLua.*           SoloLua compiler, CLI, Studio (:5092)
src/SoloPage.*          SoloPage bundler + CLI
src/Solo5.Hub/          Solo5 Hub (:5080)
archive/solorust/       Archived SoloRUST (experimental)
tests/                  Unit tests per language
examples/               Samples for every language + SoloPage
docs/                   Learn path, Solo5 hub, per-language guides
```
