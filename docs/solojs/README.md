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
| Set DOM | `set "#out" text "Hi"` |
| Runtime helpers | `solo.$`, `solo.on`, `solo.set` bundled in output |

## DOM helpers

```solojs
when ready
  set "#title" text "Solo5"
  set "#box" html "<strong>Hi</strong>"
  set "#field" value "typed"
  set "#pane" class "open"
  on click "#save"
    print "saved"
```

## Learn path

1. [Studio walkthrough](studio.md)
2. Run `examples/js/hello.solojs`
3. Build a click counter (`examples/js/counter.solojs`)
4. Add `elif` branches
5. Wire SoloJS into a SoloHTML page

## Part of Solo5

SoloJS is language **#4** in [Solo5](../solo5/README.md).
