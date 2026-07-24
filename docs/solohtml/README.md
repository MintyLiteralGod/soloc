# SoloHTML

**SoloGem’s easiest markup language** — write pages with indentation, get real HTML5.

File extension: `.solohtml`

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

## Compile

```bash
dotnet run --project src/SoloC.Cli -- html examples/html/hello.solohtml
```

Writes `examples/html/hello.html`.

Or open **SoloC Studio → SoloHTML** for a live preview:

```bash
dotnet run --project src/SoloC.Playground
```

Visit http://localhost:5088 and click **SoloHTML**.

## Ideas

| You write | You get |
|-----------|---------|
| `page` | Full HTML5 document |
| `hero` | Styled header band |
| `section` | Page section |
| `row` + `card` | Responsive card grid |
| `button primary` | Pretty call-to-action |
| `list` / `item` | Bulleted list |
| `#id` / `.class` | IDs and classes |
| `// comment` | Ignored |

## Why SoloHTML?

Same mission as SoloC: remove ceremony. No angle-bracket nesting maze — just indent and write.
