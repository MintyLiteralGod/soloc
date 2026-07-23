# SoloC language overview

SoloC is **SoloGem's** open-source programming language — C#-inspired, beginner-first, MIT licensed. Source files use the **`.sc`** extension.

**Tagline:** the easiest language to learn.

## Start here

- New? Follow the **[learn path](learn/00-welcome.md)** (lessons 00–08)
- Need a lookup? Use the **[reference](reference/README.md)**
- Want the *why*? Read **[philosophy](philosophy.md)**

## Quick taste

```soloc
print("Hello, SoloC!");

fn add(int a, int b): int {
    return a + b;
}

print(add(2, 40));
```

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
```

## What SoloC includes

| Feature | Docs |
|---------|------|
| Top-level scripts | [Learn 01](learn/01-your-first-program.md) |
| Variables (`var` / `let` / types) | [Learn 02](learn/02-variables.md) · [Types](reference/types.md) |
| `if` / `else` | [Learn 03](learn/03-decisions.md) |
| `while` / `for` | [Learn 04](learn/04-loops.md) |
| Functions (`fn`) | [Learn 05](learn/05-functions.md) |
| Arrays (`[]`, `.Length`) | [Learn 06](learn/06-arrays.md) |
| Classes + `Main` | [Learn 07](learn/07-classes.md) |
| Modules (`using`) | [Modules](reference/modules.md) |
| `print` / `Console` / `Math` | [Stdlib](reference/stdlib.md) |
| Friendly errors | [Errors](errors.md) |
| Type checking | [Types](reference/types.md) |
| Bytecode VM | [VM](vm.md) (advanced) |

## Program shapes

1. **Script** — statements at file scope  
2. **Functions** — `fn` declarations you call by name  
3. **Classes** — fields, methods, `this`, and optional `static void Main()`

## Built-ins at a glance

```soloc
print("a", 1);
Console.WriteLine("b", 2);

using Math;
print(max(1, 2));
```

## Operators

Arithmetic `+ - * / %` · Compare `== != < <= > >=` · Logic `&& || !`

## Learning tools

- [Cheat sheet](cheatsheet.md)
- [Examples](../examples/)
- [Contributing](../CONTRIBUTING.md)

SoloC grows with its learners. Keep programs tiny, read errors kindly, and ship the next `.sc` file.
