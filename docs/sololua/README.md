# SoloLua

**Lua without the usual footguns** — SoloGem Solo5 language #5.

File extension: `.sololua` → compiles to **Lua 5.4 / LuaJIT-friendly** `.lua`.

SoloLua is for people who like Lua’s small runtime and hate the ceremony and traps around it. It is not a kid language.

## Tools

| Tool | What |
|------|------|
| **Compiler CLI** | `src/SoloLua.Cli` → `sololua` |
| **Studio** | `src/SoloLua.Studio` → **http://localhost:5092** |
| **Library** | `src/SoloLua.Compiler` |

```bash
dotnet run --project src/SoloLua.Studio
dotnet run --project src/SoloLua.Cli -- compile examples/lua/showcase.sololua
dotnet run --project src/SoloLua.Cli -- notes
```

## What SoloLua fixes

| Lua pain | SoloLua |
|----------|---------|
| Globals by default | **Locals by default**; `global x = …` for `_G` |
| `~=` `and` `or` only | `!=` `&&` `||` `!` also work |
| No `continue` | `continue` (goto labels) |
| Array holes from `nil` | `list` refuses nil pushes/writes |
| `ipairs` vs `pairs` folklore | `for x in list` / `for k, v in map` |
| Metatable class ceremony | `class` / `fn` → `solo.class` |
| Awkward concat / no `+=` | `"hi {name}"`, `+=` `-=` `*=` `/=` `..=` |
| `pcall` noise | `try` / `catch` |
| `require` path mush | `import "mod" as name` |
| Optional chaining / nullish | `x?.y`, `a ?? b` |

## Quick example

```sololua
name = "SoloLua"
print "Hello, {name}"

nums = list 1, 2, 3
nums.push(4)

class Counter
  fn new(start)
    self.n = start ?? 0
    return self
  fn inc()
    self.n += 1
    return self.n

c = Counter.new(10)
print c.inc()
```

## Design notes

- **Real Lua out** — run with `lua`, LuaJIT, or embed as usual.
- **1-based tables stay 1-based** for interop; use `at0` / `set0` when you want 0-based indexing on lists.
- **Method calls** — `obj.method(` rewrites to `obj:method(` (except `.new` and stdlib modules).

## Replaces SoloRUST in Solo5

SoloRUST is **archived** under [`archive/solorust/`](../../archive/solorust/README.md). SoloLua takes language slot **#5** and Studio port **:5092**.

## Related

- [Solo5 overview](../solo5/README.md)
- [Studio](studio.md)
