# SoloPage

**Assemble SoloHTML + SoloCSS + SoloJS into a site** — SoloGem Solo5.

A folder is the unit: one page, or a multi-route marketing site with layouts, shared CSS, data→pages, and Netlify-ready output.

## Single page

```bash
dotnet run --project src/SoloPage.Cli -- new mysite
dotnet run --project src/SoloPage.Cli -- build mysite
```

## Site mode

```text
mysite/
  pages/           → routes (/, /deskcore/, /tips/, …)
  layouts/shell.solohtml
  components/
  templates/       → JSON collection templates
  data/*.json
  public/          → copied into dist/ (favicons, og.jpg, fonts)
  tokens/ + styles.solocss
  app.solojs
  site.json
```

```bash
dotnet run --project src/SoloPage.Cli -- new mysite --site
dotnet run --project src/SoloPage.Cli -- build mysite --base-url https://sologem.xyz
# → dist/ + assets/ + _redirects + sitemap.xml + robots.txt + public/*
```

Sample: **`examples/site`**

## Layouts + SEO head

```solohtml
layout layouts/shell.solohtml
  title Home
  head
    meta name=description content=Hello
    og title=Home
    twitter card=summary
    twitter title=Home
    canonical href=https://example.com/
    favicon href=/favicon.svg
    jsonld
      {"@type":"WebSite","name":"SoloGem"}
  hero
    h1 Hello
```

`link` emits HTML `<link>` (not an anchor). Use `a` for links. `.button` theme class is opt-in.

## Data → pages

`site.json`:

```json
{
  "baseUrl": "https://example.com",
  "collections": [
    {
      "data": "data/tips.json",
      "template": "templates/tip.solohtml",
      "out": "tips/{{slug}}/index.html",
      "route": "/tips/{{slug}}"
    }
  ]
}
```

Each JSON item with a `slug` becomes its own SEO URL.

## SoloJS site APIs

| Need | SoloJS |
|------|--------|
| Active nav | `solo.route.markActive("nav a")` (uses `meta solo:route`) |
| Burger / flyout | `toggleClass "#nav" open` |
| Scroll cue | `on scroll window` |
| Reveal | `when visible ".card"` |
| Clipboard | `copy "text"` / `clipboard …` |
| Forms | `on submit "#f"` + `formData` + `fetch … method=POST body="form #f" mode=no-cors` |
| Timers | `after` / `every` / `frame` |
| Canvas | `canvas "#c" into gfx` |

### Contact form (Google Form / Netlify)

```solojs
on submit "#contact"
  preventDefault
  formData "#contact" into payload
  fetch "YOUR_FORM_RESPONSE_URL" method=POST body="form #contact" mode=no-cors
    set "#status" text "Sent — thanks!"
  catch
    set "#status" text "Could not send."
```

For Netlify Forms, add `netlify=true` on the form and a success page — or POST to your function URL without `no-cors`.

## Shared SoloCSS

One `styles.solocss` (with `include tokens/brand.solocss`) compiles to `assets/site.css` linked from every page.

## Netlify

Build command:

```bash
dotnet run --project src/SoloPage.Cli -- build . --base-url https://yoursite.com
```

Publish directory: `dist`  
`_redirects` maps `/deskcore` → `/deskcore/` for clean URLs.

## Related

- [Solo5](../solo5/README.md) · [SoloHTML](../solohtml/README.md) · [SoloJS](../solojs/README.md) · [SoloCSS](../solocss/README.md)
