# SoloJS

**SoloGem’s easiest scripting language** — indent-friendly code that compiles to real JavaScript.

File extension: `.solojs`

## Tools

| Tool | What it is |
|------|------------|
| **Compiler CLI** | `src/SoloJs.Cli` → `solojs` |
| **Studio app** | `src/SoloJs.Studio` → **http://localhost:5091** |
| **Compiler library** | `src/SoloJs.Compiler` |

## SoloJS Studio

```bash
dotnet run --project src/SoloJs.Studio
```

Open **http://localhost:5091** — see compiled JS + live console/DOM preview.

## Compiler CLI

```bash
dotnet run --project src/SoloJs.Cli -- compile examples/js/hello.solojs
dotnet run --project src/SoloJs.Cli -- watch examples/js/counter.solojs
```

## Quick example

```solojs
print "Hello, SoloJS"

fn add(a, b)
  return a + b

print add(2, 40)

score = 0

when ready
  set "#out" text "Ready!"
  on click "#btn"
    score = score + 1
    set "#score" text score
```

## Features

| Feature | Example |
|---------|---------|
| Print | `print "hi"` → `console.log` |
| Functions | `fn add(a, b)` |
| Assign | `score = 0` (auto `let` on first use) |
| `var` / `let` / `const` | optional keywords |
| Ranges | `for i in 0..3` |
| For-each | `for x in items` |
| While | `while n > 0` |
| If / elif / else | indent blocks |
| DOM ready | `when ready` |
| Events | `on click "#btn"` |
| Set DOM | `set "#out" text "Hi"` / `style.*` / `dataset.*` / `attr …` |
| ClassList | `addClass` / `removeClass` / `toggleClass` |
| Events | `on click "#btn"` handler gets `e`; `preventDefault` / `stopPropagation` |
| Focus | `focus "#field"` |
| Frame / canvas | `frame` → rAF; `canvas "#c" into gfx` |
| Routing helpers | `solo.route.go` / `markActive` (hash or History) |
| Fetch | `fetch "url" into data` (+ optional `catch`) |
| Timers | `after 500` / `every 1000` |
| **React** | `component` / `state` / `render` / `mount` → React 18 |

## DOM helpers

```solojs
when ready
  set "#title" text "Solo5"
  set "#nav" style.display flex
  set "#btn" attr aria-expanded true
  toggleClass "#nav" open
  canvas "#c" into gfx
  on click "#menu"
    preventDefault
    toggleClass "#nav" open
  frame
    gfx.clear()
  solo.route.markActive("nav a")
```

## Fetch + timers

```solojs
when ready
  after 300
    print "delayed hello"

  every 5000
    print "heartbeat"

  fetch "https://example.com" into data
    set "#out" text data
  catch
    set "#out" text "offline"
```

## React (components)

SoloJS compiles to real React 18 (`createElement` + `useState` + `createRoot`).

```solojs
react

component Counter
  state count = 0

  fn bump()
    count = count + 1

  render
    div.card
      h1 {count}
      button onClick=bump "+1"

mount Counter into "#root"
```

Load React UMD scripts (SoloPage does this automatically when it detects React):

```html
<script crossorigin src="https://unpkg.com/react@18/umd/react.development.js"></script>
<script crossorigin src="https://unpkg.com/react-dom@18/umd/react-dom.development.js"></script>
```

Try: `examples/js/react-counter.solojs` or `solopage build examples/page-react`

## Learn path

1. [Studio walkthrough](studio.md)
2. Run `examples/js/hello.solojs`
3. Build a click counter (`examples/js/counter.solojs`)
4. Add `elif` branches
5. Wire SoloJS into a SoloHTML page

## Part of Solo5

SoloJS is language **#4** in [Solo5](../solo5/README.md).
