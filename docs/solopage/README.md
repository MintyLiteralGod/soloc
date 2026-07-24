# SoloPage

**Bundle SoloHTML + SoloCSS + SoloJS into one site** — SoloGem Solo5.

A SoloPage project is just a folder:

```text
mysite/
  page.solohtml
  styles.solocss
  app.solojs
  components/     (optional includes)
```

## Tools

| Tool | What |
|------|------|
| CLI | `src/SoloPage.Cli` → `solopage` |
| Library | `src/SoloPage.Compiler` |
| Hub | `src/Solo5.Hub` → **http://localhost:5080** |

## Quick start

```bash
dotnet run --project src/SoloPage.Cli -- new mysite
dotnet run --project src/SoloPage.Cli -- build mysite
# → mysite/index.html
```

Or build the repo sample:

```bash
dotnet run --project src/SoloPage.Cli -- build examples/page
```

## Solo5 Hub

One landing page for every Studio:

```bash
dotnet run --project src/Solo5.Hub
```

Open **http://localhost:5080**

## How bundling works

1. Compile `page.solohtml` (supports `include`, `css`, `js`)
2. Compile `styles.solocss`
3. Compile `app.solojs`
4. Inline CSS + JS into a single `index.html` (default)
5. If SoloJS uses React (`component` / `mount`), inject React 18 UMD scripts

## React example

```bash
dotnet run --project src/SoloPage.Cli -- build examples/page-react
```

Open `examples/page-react/index.html` in a browser.

## Related

- [Solo5 overview](../solo5/README.md)
- [SoloHTML includes](../solohtml/README.md)
- [SoloCSS](../solocss/README.md)
- [SoloJS](../solojs/README.md)
