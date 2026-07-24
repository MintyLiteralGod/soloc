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
| **SoloRUST** *(experimental)* | `.solorust` | Rust source | [localhost:5092](http://localhost:5092) |

Full map: **[docs/solo5/README.md](docs/solo5/README.md)**

## 60-second start

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Clone this repo and build:

```bash
dotnet build SoloC.sln
```

3. Open a Studio:

```bash
dotnet run --project src/SoloC.Playground     # SoloC     :5088
dotnet run --project src/SoloHtml.Studio       # SoloHTML  :5089
dotnet run --project src/SoloCss.Studio        # SoloCSS   :5090
dotnet run --project src/SoloJs.Studio         # SoloJS    :5091
dotnet run --project src/SoloRust.Studio       # SoloRUST  :5092
```

### Compilers (CLI)

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloHtml.Cli -- compile examples/html/showcase.solohtml
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss
dotnet run --project src/SoloJs.Cli -- compile examples/js/hello.solojs
dotnet run --project src/SoloRust.Cli -- compile examples/rust/hello.solorust
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

- **Solo5 suite** — SoloC + SoloHTML + SoloCSS + SoloJS + SoloRUST
- **Studios for every language** — browser GUIs with demos and download
- **Dedicated compilers** — `soloc`, `solohtml`, `solocss`, `solojs`, `solorust`
- **SoloCSS** — vars, nesting, media, friendly property shortcuts
- **SoloJS** — `print`, `fn`, loops, `when ready`, DOM helpers
- **SoloRUST** — experimental path into real Rust source ([research](docs/solorust/research.md))
- **SoloC scripts** — top-level statements; friendly errors; bytecode VM

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
src/SoloRust.*          SoloRUST experimental compiler, CLI, Studio (:5092)
tests/                  Unit tests per language
examples/               .sc .solohtml .solocss .solojs .solorust samples
docs/                   Learn path, Solo5 hub, per-language guides
```
