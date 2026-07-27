# SoloHTML

**SoloGem’s easiest markup language** — write pages with indentation, get real HTML5.

File extension: `.solohtml`

## Tools

| Tool | What it is |
|------|------------|
| **Compiler CLI** | `src/SoloHtml.Cli` → `solohtml` |
| **Studio app** | `src/SoloHtml.Studio` → browser GUI at `:5089` |
| **Compiler library** | `src/SoloHtml.Compiler` |

## SoloHTML Studio (GUI)

```bash
dotnet run --project src/SoloHtml.Studio
```

Open **http://localhost:5089**

- Live preview as you type
- Demo gallery
- Download compiled `.html`

## Compiler CLI

```bash
# Compile once
dotnet run --project src/SoloHtml.Cli -- compile examples/html/hello.solohtml

# Watch + rebuild on save
dotnet run --project src/SoloHtml.Cli -- watch examples/html/showcase.solohtml

# Print HTML to terminal
dotnet run --project src/SoloHtml.Cli -- compile examples/html/hello.solohtml --stdout
```

## Quick example

```solohtml
page Hello
  title Hello SoloHTML
  hero
    brand SoloGem
    h1 Hello, SoloHTML
    p The easiest way to make a web page.
    button primary href=#go Get started
```

## Tag cheat sheet

| You write | You get |
|-----------|---------|
| `page` | Full HTML5 document |
| `hero` | Styled header band |
| `section` | Page section |
| `row` + `card` | Responsive card grid |
| `button primary` / `button btn` | CTA; `.button` class is **opt-in** |
| `a href=…` | Anchor |
| `link rel=… href=…` | Real HTML `<link>` (head asset — not an anchor) |
| `favicon` / `og` / `canonical` | Head SEO / icons |
| `layout shell.solohtml` + `slot` | Shared site shell |
| `include nav.solohtml` | Splice in another SoloHTML file |
| `css href=app.css` | `<link rel="stylesheet">` |
| `js src=app.js` | `<script src>` at end of body |
| `page theme=none` / `notheme` / `bare` | Skip SoloHTML’s default theme CSS |
| `// comment` | Ignored |

## Layouts + includes

```solohtml
layout layouts/shell.solohtml
  title Home
  hero
    h1 Hello
```

```solohtml
// layouts/shell.solohtml
page theme=none
  head
    favicon href=/favicon.svg
    og title=My Site
  include components/nav.solohtml
  main
    slot
  include components/footer.solohtml
```

Paths are relative to the current file. Cycles are rejected.

## Default theme

SoloHTML injects a small starter stylesheet for Studio demos. Opt out when you bring your own CSS:

- `page MySite theme=none` (or `notheme` / `bare`)
- any `css` / `stylesheet` link on the page
- SoloPage with a `.solocss` file (theme off automatically)

`.button` styling is opt-in (`primary` / `secondary` / `ghost` / `btn` / `styled`) so custom SoloCSS is not fighting mystery classes.

## Why SoloHTML?

Same mission as SoloC: remove ceremony. No angle-bracket nesting maze — indent and write.
