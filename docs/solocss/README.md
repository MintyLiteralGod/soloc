# SoloCSS

**SoloGem’s easiest stylesheet language** — theme variables, nested rules, and shortcuts that compile to real CSS.

File extension: `.solocss`

## Tools

| Tool | What it is |
|------|------------|
| **Compiler CLI** | `src/SoloCss.Cli` → `solocss` |
| **Studio app** | `src/SoloCss.Studio` → **http://localhost:5090** |
| **Compiler library** | `src/SoloCss.Compiler` |

## SoloCSS Studio

```bash
dotnet run --project src/SoloCss.Studio
```

Open **http://localhost:5090** — live preview, demos, download `.css`.

## Compiler CLI

```bash
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss
dotnet run --project src/SoloCss.Cli -- watch examples/css/showcase.solocss
dotnet run --project src/SoloCss.Cli -- compile examples/css/hello.solocss --stdout
```

## Quick example

```solocss
vars
  brand #0f2a22
  accent #d8ff3e
  ink #102018

body
  margin 0
  color $ink
  font "Segoe UI", system-ui, sans-serif

.hero
  padding 4rem
  background linear-gradient(145deg, $brand, $accent)

  h1
    size clamp(2.4rem, 7vw, 4rem)

.button
  pad 0.75rem 1.25rem
  background $accent
  radius 0.55rem
  no-underline
  bold

media max-width 640px
  .hero
    padding 2rem
```

## Features

- **`vars` / `theme`** — becomes `:root { --name: value }` and `$name` → `var(--name)`
- **Nesting** — indented rules become descendants (`.hero h1`)
- **`&` parent** — `.card` + `&:hover` → `.card:hover`
- **Media queries** — `media max-width 640px` or `@media (max-width: 640px)`
- **Shortcuts** — see cheat sheet below
- **Comments** — `//` or `#`

## Property cheat sheet

| You write | CSS you get |
|-----------|-------------|
| `size 1.2rem` | `font-size: 1.2rem` |
| `font "Inter", sans-serif` | `font-family: …` |
| `pad 1rem` | `padding: 1rem` |
| `bg #fff` | `background: #fff` |
| `radius 8px` | `border-radius: 8px` |
| `weight 700` / `bold` | `font-weight` |
| `flex` / `grid` | `display: flex/grid` |
| `columns …` | `grid-template-columns` |
| `no-underline` | `text-decoration: none` |
| `center` | `text-align: center` |
| `shadow …` | `box-shadow` |
| `letter -0.03em` | `letter-spacing` |
| `line 1.5` | `line-height` |

Any normal CSS property name also works: `margin`, `color`, `display`, …

## Learn path

1. [Studio walkthrough](studio.md)
2. Copy `examples/css/hello.solocss`
3. Change `$accent` and recompile
4. Add a nested rule under `.hero`
5. Add a `media` block for phones

## Part of Solo5

SoloCSS is language **#3** in [Solo5](../solo5/README.md).
