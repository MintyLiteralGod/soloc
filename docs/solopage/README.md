# SoloPage

**Assemble SoloHTML + SoloCSS + SoloJS into a site** — SoloGem Solo5.

Not a full web framework. A folder is the unit: one page, or many routes with a shared shell.

## Single page

```text
mysite/
  page.solohtml
  styles.solocss
  app.solojs
  components/
```

```bash
dotnet run --project src/SoloPage.Cli -- new mysite
dotnet run --project src/SoloPage.Cli -- build mysite
# → mysite/index.html (CSS/JS inlined)
```

## Site mode (multi-page)

```text
mysite/
  pages/
    index.solohtml       → dist/index.html           (/)
    deskcore.solohtml    → dist/deskcore/index.html  (/deskcore/)
  layouts/shell.solohtml
  components/
  tokens/brand.solocss
  styles.solocss
  app.solojs
```

```bash
dotnet run --project src/SoloPage.Cli -- new mysite --site
dotnet run --project src/SoloPage.Cli -- build mysite
# → dist/ + assets/site.css + assets/site.js (shared, cacheable)
```

Sample: `examples/site`

## Layouts + includes

```solohtml
layout layouts/shell.solohtml
  title Home
  hero
    h1 Hello
```

Shell owns nav/footer/head once; pages fill `slot`:

```solohtml
page theme=none
  head
    favicon href=/favicon.svg
    og title=My Site
  include components/nav.solohtml
  main
    slot
  include components/footer.solohtml
```

## Head control

| Tag | Emits |
|-----|--------|
| `favicon href=…` | `<link rel="icon">` |
| `apple-touch-icon` | apple touch icon |
| `canonical href=…` | canonical link |
| `og title=…` | `<meta property="og:title">` |
| `link rel=… href=…` | real HTML `<link>` (not an anchor) |
| `meta name=… content=…` | meta |
| `a href=…` | anchors |

## Theme

When a `.solocss` file is present, SoloHTML’s default theme stays **off**.  
`.button` theme class is **opt-in** (`primary` / `secondary` / `ghost` / `btn` / `styled`).

## SoloJS site helpers

- `toggleClass` / `addClass` / `removeClass`
- `set "#x" style.display flex`, `attr aria-expanded true`, `dataset…`
- `preventDefault` / `stopPropagation` inside `on`
- `frame` → `requestAnimationFrame`
- `canvas "#c" into gfx`
- `solo.route.markActive("nav a")` / `solo.route.go("/path")`

## Forms & external flows

Use real form tags; wire checkout/mailto outside SoloPage:

```solohtml
form action=mailto:hello@example.com method=post
  label Email
  input type=email name=email required=true
  button.btn.primary type=submit Send
```

Polar/Stripe/etc.: point `action` or a SoloJS `on click` at your checkout URL — SoloPage does not own payments.

## Related

- [Solo5 overview](../solo5/README.md)
- [SoloHTML](../solohtml/README.md) · [SoloCSS](../solocss/README.md) · [SoloJS](../solojs/README.md)
